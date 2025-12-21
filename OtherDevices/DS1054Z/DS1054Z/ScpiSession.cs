using DS1054Z;
using NationalInstruments.Visa;
using System;
using System.Diagnostics;
using System.Text;

namespace DS1054Z
{

    public sealed class ScpiSession : IDisposable
    {
        private readonly object _lock = new object();
        private readonly TcpipSession _session;
        private bool _disposed;

        public ScpiSession(TcpipSession session)
        {
            _session = session ?? throw new ArgumentNullException(nameof(session));
        }

        // --------------- Core low-level operations ---------------

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

        public double QueryVpp(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > 4)
                throw new ArgumentOutOfRangeException(nameof(channelNumber));

            string cmd = $":MEASure:ITEM? VPP,CHANnel{channelNumber}";
            return QueryDouble(cmd);
        }

        public double QueryChannelScale(int channelNumber)
        {
            if (channelNumber < 1 || channelNumber > 4)
                throw new ArgumentOutOfRangeException(nameof(channelNumber));

            string cmd = $":CHANnel{channelNumber}:SCALe?";
            return QueryDouble(cmd);
        }

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

        private void EnsureNotDisposed()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(ScpiSession));
        }

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