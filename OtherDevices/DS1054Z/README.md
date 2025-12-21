# DS1054Z Oscilloscope Viewer

A WPF application for real-time waveform display and control of the Rigol DS1054Z oscilloscope via TCP/IP (LXI/VXI-11).

## Overview

This application provides a graphical interface to remotely control and view waveforms from a Rigol DS1054Z oscilloscope. It communicates with the instrument using SCPI (Standard Commands for Programmable Instruments) commands over a TCP/IP connection.

### Features

- **Real-time waveform display** for all 4 channels
- **Channel control**: Enable/disable individual channels
- **Acquisition control**: Run, Stop, Single, and Auto-scale modes
- **Live measurements**: Peak-to-peak voltage (Vpp), channel scale, and timebase
- **Color-coded channels**: Yellow (CH1), Cyan (CH2), Violet (CH3), Blue (CH4)
- **Engineering notation formatting** for measurements (e.g., 1.23 mV, 5.00 µs)

## Requirements

### Hardware
- Rigol DS1054Z oscilloscope (or compatible model)
- Network connection between PC and oscilloscope (Ethernet/LAN)

### Software
- .NET Framework 4.7.2 or later
- Visual Studio 2015 or later (for building)
- NI-VISA runtime (National Instruments VISA drivers)
- Syncfusion WPF controls for charting

### NuGet Dependencies
- `NationalInstruments.Visa` - GPIB/LXI communication library
- `Syncfusion.UI.Xaml.Charts` - High-performance charting controls

## Configuration

### Network Setup

1. **Configure the oscilloscope's IP address:**
   - On the DS1054Z, press **Utility** → **IO Setting** → **LAN Config**
   - Set a static IP address or enable DHCP
   - Note the IP address displayed

2. **Update the application's TCP/IP address:**
   - Open `MainWindow.xaml.cs`
   - Locate the line: `private string TCPIPAddress = @"TCPIP0::192.168.1.145::inst0::INSTR";`
   - Replace `192.168.1.145` with your oscilloscope's IP address

3. **Verify connectivity:**
   - Test the connection using NI MAX (National Instruments Measurement & Automation Explorer)
   - Or use ping to verify network connectivity: `ping 192.168.1.145`

## Building the Application

### Using Visual Studio
1. Open `DS1054Z.sln` in Visual Studio
2. Restore NuGet packages (right-click solution → Restore NuGet Packages)
3. Build the solution (F6 or Build → Build Solution)

### Using MSBuild
```bash
msbuild DS1054Z.sln /p:Configuration=Release
```

## Usage

1. **Start the application** - The application will automatically connect to the oscilloscope at startup
   - If connection fails, it will retry indefinitely until successful

2. **Enable channels** - Check the channel checkboxes (CH1-CH4) to enable display and acquisition
   - Unchecked channels are not displayed or queried

3. **Control acquisition:**
   - **Run/Stop** toggle: Start/stop continuous acquisition
   - **Auto** button: Trigger auto-scale on the oscilloscope
   - **Single** button: Capture a single acquisition

4. **View measurements** - Labels show real-time measurements for each enabled channel:
   - **Vpp**: Peak-to-peak voltage
   - **Scale**: Vertical scale (volts/division)
   - **Timebase**: Horizontal timebase (seconds/division)

## Architecture

### Core Components

#### `ScpiSession.cs`
Thread-safe SCPI communication wrapper providing:
- **Low-level operations**: `Write()`, `QueryString()`, `QueryDouble()`
- **Binary block transfer**: `QueryBinaryBlock()` for waveform data (IEEE 488.2 format)
- **Rigol-specific helpers**: `QueryWaveformPreamble()`, `QueryVpp()`, `QueryChannelScale()`, `QueryTimebaseScale()`

#### `MainWindow.xaml.cs`
Main application window and logic:
- **Communication initialization**: Sets up TCP/IP session with proper timeouts and termination settings
- **Background thread**: Continuously polls enabled channels for waveform data
- **Event handlers**: UI controls for channel enable/disable and acquisition control
- **Data structures**: `WaveformPreamble` for metadata, `LabelItem` for channel info display

#### `ChartViewModel.cs`
Data model for waveform display:
- Converts raw byte arrays from the oscilloscope into `DataPoint` collections
- Each data point has an X (sample index) and Y (sample value) coordinate

#### `ToEngineeringFormat.cs`
Utility for formatting numeric values with engineering notation and metric prefixes:
- Converts values like 0.00123 V to "1.23 mV"
- Supports yocto (10^-24) through tera (10^12) prefixes

## SCPI Commands Reference

The application uses the following SCPI commands to communicate with the DS1054Z:

### Waveform Configuration
```
:WAVeform:FORMat BYTE          # Set waveform data format to byte
:WAVeform:MODE NORMal          # Set waveform mode to normal
:WAVeform:STARt 1              # Set waveform start point
:WAVeform:STOP <points>        # Set waveform stop point (auto-detected)
:WAVeform:SOURce CHANnel<n>    # Select waveform source channel (1-4)
```

### Waveform Queries
```
:WAVeform:PREamble?            # Query waveform preamble (metadata)
:WAVeform:DATA?                # Query waveform data (returns IEEE 488.2 binary block)
:WAVeform:POINts?              # Query available waveform points
```

### Channel Control
```
:CHANnel<n>:DISPlay ON/OFF     # Enable/disable channel display
:CHANnel<n>:SCALe?             # Query channel vertical scale
```

### Acquisition Control
```
:RUN                           # Start continuous acquisition
:STOP                          # Stop acquisition
:SINGle                        # Single acquisition
:AUToscale                     # Auto-scale all channels
```

### Measurements
```
:MEASure:ITEM? VPP,CHANnel<n>  # Query peak-to-peak voltage
:TIMebase:MAIN:SCALe?          # Query timebase scale
```

## Waveform Data Format

The oscilloscope returns waveform data in **IEEE 488.2 definite-length arbitrary block format**:

```
#<N><XXXX><data bytes>
```

Where:
- `#` - Block prefix character
- `N` - Single digit indicating the number of digits in the length field
- `XXXX` - Decimal byte count of the payload (N digits)
- `<data bytes>` - Raw waveform data

**Example:** `#9000001200<1200 bytes of data>`
- `#9` indicates 9 digits follow
- `000001200` means 1200 bytes of data
- Followed by 1200 bytes of waveform samples

### Converting Raw Data to Voltage

Using the preamble values:
```
Voltage = ((byte_value - yReference) * yIncrement) + yOrigin
Time = ((point_index - xReference) * xIncrement) + xOrigin
```

## Threading Model

The application uses a background thread (`UpdateDisplayThread`) to continuously poll the oscilloscope:
- Main thread handles UI interactions
- Background thread queries waveform data every 10ms for enabled channels
- Data is marshaled to UI thread via `Dispatcher.Invoke()`
- Thread is gracefully shut down on window closing with a 2-second timeout

## Known Limitations

- **Fixed IP address**: Must be changed in source code and recompiled
- **No error recovery**: Communication errors are logged but may require application restart
- **Single oscilloscope**: Only supports one oscilloscope connection at a time
- **Limited to 4 channels**: Designed specifically for the DS1054Z's 4 analog channels

## Troubleshooting

### Connection Issues
- **"Unable to connect"**: Verify oscilloscope IP address and network connectivity
- **Timeout errors**: Check firewall settings, ensure NI-VISA is properly installed
- **No waveforms displayed**: Verify channels are enabled on both oscilloscope and application

### Performance Issues
- **Slow update rate**: Reduce timebase to decrease data points
- **UI freezing**: Ensure background thread is running properly (check debug output)

### Build Issues
- **Missing NI-VISA references**: Install NI-VISA runtime from National Instruments website
- **Missing Syncfusion controls**: Restore NuGet packages or install Syncfusion WPF controls

## Additional Resources

- [Rigol DS1054Z Programming Guide](https://www.rigolna.com/products/digital-oscilloscopes/1000z/)
- [NI-VISA Documentation](https://www.ni.com/en-us/support/downloads/drivers/download.ni-visa.html)
- [SCPI Standard](https://www.ivifoundation.org/specifications/default.aspx)

## License

This application is part of the GPIBUtils repository. Refer to the repository's main license for usage terms.

## Credits

- Engineering notation formatting by Steve Hageman: http://analoghome.blogspot.com/2012/01/how-to-format-numbers-in-engineering.html
