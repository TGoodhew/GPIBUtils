using DS1054Z;
using NationalInstruments.Visa;
using System;
using System.Diagnostics;
using System.Text;

namespace DS1054Z
{
    /// <summary>
    /// Provides a thread-safe SCPI (Standard Commands for Programmable Instruments) communication session
    /// wrapper for the Rigol DS1054Z oscilloscope over TCP/IP.
    /// </summary>
    /// <remarks>
    /// This class wraps a NationalInstruments.Visa TcpipSession to provide simplified SCPI command
    /// execution with thread-safe locking and proper resource management. It supports both text-based
    /// queries and binary block transfers (e.g., for waveform data).
    /// </remarks>
    public sealed class ScpiSession : IDisposable
    {
        private readonly object _lock = new object();
        private readonly TcpipSession _session;
        private bool _disposed;

        /// <summary>
        /// Initializes a new instance of the <see cref="ScpiSession"/> class.
        /// </summary>
        /// <param name="session">The TCP/IP session to wrap. Must not be null.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="session"/> is null.</exception>
        public ScpiSession(TcpipSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        // --------------- Core low-level operations ---------------

        /// <summary>
        /// Sends a SCPI command to the instrument without expecting a response.
        /// </summary>
        /// <param name="command">The SCPI command string to send. Must not be null or empty.</param>
        /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is null or empty.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when this session has been disposed.</exception>
        public void Write(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command must not be null or empty.", nameof(command));

            lock (_lock)
            {
                EnsureNotDisposed();
                _session.FormattedIO.WriteLine(command);
            }
        }

        /// <summary>
        /// Sends a SCPI query command and reads the response as a string.
        /// </summary>
        /// <param name="command">The SCPI query command to send. Must not be null or empty.</param>
        /// <returns>The string response from the instrument.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is null or empty.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when this session has been disposed.</exception>
        public string QueryString(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command must not be null or empty.", nameof(command));

            lock (_lock)
            {
                EnsureNotDisposed();
                _session.FormattedIO.WriteLine(command);
                return _session.FormattedIO.ReadString();
            }
        }

        /// <summary>
        /// Sends a SCPI query command and reads the response as a double-precision floating-point number.
        /// </summary>
        /// <param name="command">The SCPI query command to send. Must not be null or empty.</param>
        /// <returns>The numeric response from the instrument as a double.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is null or empty.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when this session has been disposed.</exception>
        /// <exception cref="FormatException">Thrown when the response cannot be parsed as a double.</exception>
        public double QueryDouble(string command)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command must not be null or empty.", nameof(command));

            lock (_lock)
            {
                EnsureNotDisposed();
                _session.FormattedIO.WriteLine(command);
                return _session.FormattedIO.ReadDouble();
            }
        }

        // --------------- Binary block read (e.g., waveform data) ---------------

        /// <summary>
        /// Sends a SCPI query command and reads the response as a binary block in IEEE 488.2 format.
        /// </summary>
        /// <remarks>
        /// This method handles the IEEE 488.2 definite-length arbitrary block format (#NXXXXXXXX...),
        /// where N is a single digit indicating how many bytes follow to specify the data length,
        /// and XXXXXXXX is the decimal byte count of the payload data.
        /// </remarks>
        /// <param name="command">The SCPI query command to send. Must not be null or empty.</param>
        /// <param name="swallowTerminator">If true, attempts to read and discard a trailing terminator character after the binary data.</param>
        /// <returns>The binary payload data as a byte array.</returns>
        /// <exception cref="ArgumentException">Thrown when <paramref name="command"/> is null or empty.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when this session has been disposed.</exception>
        /// <exception cref="InvalidOperationException">Thrown when the binary block format is invalid or incomplete.</exception>
        public byte[] QueryBinaryBlock(string command, bool swallowTerminator = true)
        {
            if (string.IsNullOrWhiteSpace(command))
                throw new ArgumentException("Command must not be null or empty.", nameof(command));

            lock (_lock)
            {
                EnsureNotDisposed();

                _session.FormattedIO.WriteLine(command);

                var raw = _session.RawIO;

                // 1. Read '#'+digit count
                byte[] header = raw.Read(2);
                if (header.Length != 2 || header[0] != (byte)'#')
                    throw new InvalidOperationException("Invalid SCPI binary block: missing '#' prefix.");

                int numLenDigits = header[1] - '0';
                if (numLenDigits < 1 || numLenDigits > 9)
                    throw new InvalidOperationException($"Invalid SCPI binary block: length digit count {numLenDigits} out of range.");

                // 2. Read length digits
                byte[] lenBytes = raw.Read(numLenDigits);
                if (lenBytes.Length != numLenDigits)
                    throw new InvalidOperationException("Incomplete SCPI binary block length header.");

                int dataLength = int.Parse(Encoding.ASCII.GetString(lenBytes));

                // 3. Read payload
                byte[] data = raw.Read(dataLength);
                if (data.Length != dataLength)
                    throw new InvalidOperationException("Incomplete SCPI binary block payload.");

                // 4. Optionally consume trailing terminator to keep stream aligned
                if (swallowTerminator)
                {
                    // Use a short timeout to avoid blocking if no terminator is present
                    int originalTimeout = _session.TimeoutMilliseconds;
                    try
                    {
                        _session.TimeoutMilliseconds = 100; // Short timeout for optional terminator
                        byte[] terminator = raw.Read(1);
                        // Successfully read terminator (typically '\n'), discard it
                    }
                    catch (Ivi.Visa.IOTimeoutException ex)
                    {
                        // No terminator present, which is fine for many instruments
                        Debug.WriteLine($"No terminator found after binary block read: {ex.Message}");
                    }
                    finally
                    {
                        _session.TimeoutMilliseconds = originalTimeout;
                    }
                }

                return data;
            }
        }

        // --------------- Higher-level helpers (Rigol-specific) ---------------

        /// <summary>
        /// Queries the waveform preamble for a specific channel on the Rigol DS1054Z oscilloscope.
        /// </summary>
        /// <remarks>
        /// The preamble contains metadata about the waveform such as format, number of points,
        /// and scaling information needed to convert raw data to voltage/time values.
        /// </remarks>
        /// <param name="channelNumber">The channel number (1-4) to query.</param>
        /// <returns>A <see cref="WaveformPreamble"/> structure containing the parsed preamble data.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="channelNumber"/> is not between 1 and 4.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when this session has been disposed.</exception>
        /// <exception cref="FormatException">Thrown when the preamble response cannot be parsed.</exception>
        public WaveformPreamble QueryWaveformPreamble(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > 4)
                throw new ArgumentOutOfRangeException(nameof(channelNumber), "Channel must be between 1 and 4.");

            lock (_lock)
            {
                EnsureNotDisposed();

                // Set waveform source for the correct channel
                _session.FormattedIO.WriteLine($":WAVeform:SOURce CHANnel{channelNumber}");

                // Query preamble as a comma-separated string
                _session.FormattedIO.WriteLine(":WAVeform:PREamble?");
                string preambleText = _session.FormattedIO.ReadString();

                return WaveformPreamble.Parse(preambleText);
            }
        }

        /// <summary>
        /// Queries the peak-to-peak voltage (Vpp) measurement for a specific channel.
        /// </summary>
        /// <param name="channelNumber">The channel number (1-4) to measure.</param>
        /// <returns>The peak-to-peak voltage in volts.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="channelNumber"/> is not between 1 and 4.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when this session has been disposed.</exception>
        public double QueryVpp(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > 4)
                throw new ArgumentOutOfRangeException(nameof(channelNumber));

            string cmd = $":MEASure:ITEM? VPP,CHANnel{channelNumber}";
            return QueryDouble(cmd);
        }

        /// <summary>
        /// Queries the vertical scale setting for a specific channel.
        /// </summary>
        /// <param name="channelNumber">The channel number (1-4) to query.</param>
        /// <returns>The vertical scale in volts per division.</returns>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="channelNumber"/> is not between 1 and 4.</exception>
        /// <exception cref="ObjectDisposedException">Thrown when this session has been disposed.</exception>
        public double QueryChannelScale(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > 4)
                throw new ArgumentOutOfRangeException(nameof(channelNumber));

            string cmd = $":CHANnel{channelNumber}:SCALe?";
            return QueryDouble(cmd);
        }

        /// <summary>
        /// Queries the horizontal timebase scale setting.
        /// </summary>
        /// <returns>The horizontal timebase scale in seconds per division.</returns>
        /// <exception cref="ObjectDisposedException">Thrown when this session has been disposed.</exception>
        public double QueryTimebaseScale()
        {
            return QueryDouble(":TIMebase:MAIN:SCALe?");
        }

        /// <summary>
        /// Queries the current waveform points setting from the oscilloscope.
        /// The number of points varies with the timebase setting.
        /// </summary>
        /// <returns>The number of waveform points currently available.</returns>
        /// <exception cref="FormatException">Thrown when the device returns an invalid or non-positive value.</exception>
        public int QueryWaveformPoints()
        {
            string result = QueryString(":WAVeform:POINts?");
            if (!int.TryParse(result.Trim(), out int points) || points <= 0)
                throw new FormatException($"Device returned invalid waveform points value: '{result}'. Expected a positive integer.");
            return points;
        }

        // --------------- Lifetime ---------------

        /// <summary>
        /// Ensures the session has not been disposed. Throws if it has.
        /// </summary>
        /// <exception cref="ObjectDisposedException">Thrown when this session has been disposed.</exception>
        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ScpiSession));
        }

        /// <summary>
        /// Releases all resources used by the <see cref="ScpiSession"/> instance.
        /// </summary>
        public void Dispose()
        {
            if (_disposed) return;

            lock (_lock)
            {
                if (_disposed) return;
                _disposed = true;
                _session?.Dispose(); // if you own it; otherwise remove this
            }
        }
    }
}