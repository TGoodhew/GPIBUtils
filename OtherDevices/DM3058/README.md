# DM3058 Digital Multimeter Application

A WPF application for monitoring and controlling the Rigol DM3058 Digital Multimeter via GPIB/LXI interface. Provides real-time measurement display with engineering notation formatting.

## Overview

This application provides a graphical interface to remotely monitor measurements from a Rigol DM3058 digital multimeter. It communicates with the instrument using SCPI (Standard Commands for Programmable Instruments) commands over GPIB or LXI (LAN eXtensions for Instrumentation).

### Features

- **Real-time measurement display** with automatic updates
- **Multiple measurement modes**: DC/AC Voltage, DC/AC Current, Resistance, Continuity, Diode, Capacitance, Frequency
- **Engineering notation formatting**: Automatic unit scaling (e.g., 1.23 kΩ, 5.67 mV)
- **High precision**: Displays measurements with full instrument precision
- **Flexible connectivity**: Supports GPIB and LXI/VXI-11 connections
- **Simple interface**: Clean, easy-to-read display with minimal controls

## Requirements

### Hardware
- Rigol DM3058 Digital Multimeter (or compatible DM30xx series model)
- GPIB interface card or USB-GPIB adapter (for GPIB connection), or
- Network connection (for LXI connection)

### Software
- .NET Framework 4.8.1 or later
- Visual Studio 2015 or later (for building)
- NI-VISA runtime (National Instruments VISA drivers)
  - Download from: [NI-VISA Downloads](https://www.ni.com/en-us/support/downloads/drivers/download.ni-visa.html)

### NuGet Dependencies
- `NationalInstruments.Visa` - GPIB/LXI communication library
- `NationalInstruments.Common` - NI common libraries

## Configuration

### GPIB Setup

1. **Configure the multimeter's GPIB address:**
   - On the DM3058, press **Utility** → **Interface** → **GPIB**
   - Set the GPIB address (typically 1-30)
   - Note the address for application configuration

2. **Configure the application:**
   - Edit the GPIB address in the source code or configuration file
   - Default format: `GPIB0::address::INSTR` (e.g., `GPIB0::12::INSTR`)

3. **Verify connectivity:**
   - Use NI MAX (National Instruments Measurement & Automation Explorer) to test connection
   - Send `*IDN?` query to verify instrument responds correctly

### LXI (Network) Setup

1. **Configure the multimeter's network settings:**
   - On the DM3058, press **Utility** → **Interface** → **LAN**
   - Set IP address (static or DHCP)
   - Note the IP address displayed

2. **Configure the application:**
   - Use TCPIP resource string format: `TCPIP0::ip_address::INSTR`
   - Example: `TCPIP0::192.168.1.100::INSTR`

3. **Verify connectivity:**
   - Test with ping: `ping 192.168.1.100`
   - Use NI MAX to test LXI connection

## Building the Application

### Using Visual Studio
1. Open `DM3058.sln` in Visual Studio
2. Restore NuGet packages (right-click solution → Restore NuGet Packages)
3. Build the solution (F6 or Build → Build Solution)

### Using MSBuild
```bash
msbuild DM3058.sln /p:Configuration=Release
```

The compiled application will be output to `DM3058/bin/Release/DM3058.exe`

## Usage

1. **Connect the multimeter**
   - Power on the DM3058
   - Connect via GPIB cable or network

2. **Start the application**
   - Launch DM3058.exe
   - Application will attempt to connect using configured address

3. **View measurements**
   - Application automatically polls the multimeter for current measurement
   - Display updates in real-time with engineering notation
   - Measurement mode is detected automatically from instrument

4. **Control the multimeter**
   - Use the multimeter's front panel to change measurement modes
   - Use the multimeter's front panel to change ranges and settings
   - Application reflects current measurement mode and value

## Architecture

### Core Components

#### `MainWindow.xaml`
Main application window providing:
- Large, easy-to-read measurement display
- Status indicators
- Connection information
- Minimal UI for distraction-free monitoring

#### `MainWindow.xaml.cs`
Application logic:
- **VISA session management**: Initializes and maintains GPIB/LXI connection
- **Background polling**: Continuously queries multimeter for measurements
- **SCPI communication**: Sends and receives SCPI commands
- **Data formatting**: Converts raw measurements to engineering notation
- **Error handling**: Manages connection errors and instrument faults

#### `ToEngineeringFormat.cs`
Utility class for engineering notation formatting:
- Converts numeric values with appropriate metric prefixes
- Supports full range from yocto (10^-24) to yotta (10^24)
- Examples: 0.001234 V → "1.234 mV", 12345 Ω → "12.345 kΩ"

## SCPI Commands Reference

The application uses standard SCPI commands compatible with the Rigol DM3058:

### Basic Queries
```
*IDN?                          # Query instrument identification
SYSTem:ERRor?                  # Query error queue
```

### Measurement Queries
```
MEASure[:VOLTage][:DC]?        # Measure DC voltage
MEASure:VOLTage:AC?            # Measure AC voltage
MEASure[:CURRent][:DC]?        # Measure DC current
MEASure:CURRent:AC?            # Measure AC current
MEASure:RESistance?            # Measure resistance
MEASure:FRESistance?           # Measure 4-wire resistance
MEASure:CONTinuity?            # Continuity test
MEASure:DIODe?                 # Diode test
MEASure:CAPacitance?           # Measure capacitance
MEASure:FREQuency?             # Measure frequency
```

### Configuration Commands
```
CONFigure[:VOLTage][:DC]       # Configure DC voltage mode
CONFigure:VOLTage:AC           # Configure AC voltage mode
CONFigure[:CURRent][:DC]       # Configure DC current mode
CONFigure:CURRent:AC           # Configure AC current mode
SENSe:VOLTage:RANGe <value>    # Set voltage range
```

## Measurement Modes

The DM3058 supports the following measurement modes, all of which are displayed by the application:

- **DC Voltage**: Up to 1000V, resolution down to 0.1 µV
- **AC Voltage**: Up to 750V, 20 Hz to 100 kHz
- **DC Current**: Up to 10A, resolution down to 0.1 nA
- **AC Current**: Up to 10A, 20 Hz to 5 kHz
- **2-Wire Resistance**: Up to 100 MΩ
- **4-Wire Resistance**: Up to 100 MΩ (eliminates lead resistance)
- **Continuity**: Audio and visual indication
- **Diode**: Forward voltage measurement
- **Capacitance**: Up to 10 mF
- **Frequency**: 20 Hz to 1 MHz

## Performance

- **Update Rate**: Configurable polling interval (typically 100-500ms)
- **Precision**: Full instrument precision preserved in display
- **Latency**: Minimal delay between instrument measurement and display update
- **CPU Usage**: Low CPU usage during normal operation

## Known Limitations

- **Single instrument**: Only supports one multimeter connection at a time
- **No configuration**: Measurement mode and range must be set on instrument front panel
- **No logging**: Does not include data logging features (measurements are displayed but not recorded)
- **No graphs**: Displays current value only, no trend plotting
- **Manual reconnection**: Connection errors require application restart

## Troubleshooting

### Connection Issues
- **"Unable to connect"**:
  - Verify instrument address (GPIB or TCPIP)
  - Check that multimeter is powered on
  - For GPIB: Ensure GPIB interface card is installed, cables connected
  - For LXI: Verify network connectivity with ping, check firewall settings
  - Test connection independently using NI MAX

- **Timeout errors**:
  - Check instrument is not in remote lockout mode
  - Verify NI-VISA drivers are properly installed
  - Try increasing timeout values in code
  - Ensure instrument is not busy with long measurement

### Measurement Issues
- **No display or zero values**:
  - Check instrument measurement mode
  - Verify test leads are properly connected
  - Ensure instrument is not showing error on front panel
  - Check instrument range settings

- **Incorrect values**:
  - Verify instrument calibration
  - Check test lead connections and quality
  - Ensure appropriate range is selected
  - Review instrument accuracy specifications

### Build Issues
- **Missing NI-VISA references**: 
  - Install NI-VISA runtime from [National Instruments website](https://www.ni.com/en-us/support/downloads/drivers/download.ni-visa.html)
  - Rebuild solution after installing NI-VISA

- **NuGet package errors**: 
  - Restore NuGet packages in Visual Studio
  - Clear NuGet cache if packages fail to restore

## Device Information

### Rigol DM3058 Specifications

- **Display**: 5½ digit (20,000 count)
- **Dual Display**: Main and auxiliary measurements simultaneously
- **Measurement Speed**: Up to 50 readings/second
- **Interfaces**: USB, RS-232, GPIB (optional), LXI (LAN)
- **SCPI Compatible**: Standard SCPI command set
- **Accuracy**: Up to 0.015% for DC voltage

### Why "OtherDevices"?

This application is located in the `OtherDevices` folder because the DM3058 is manufactured by Rigol, not HP/Agilent. The `HPDevices` library and test apps are specifically for HP/Agilent equipment, while `OtherDevices` contains drivers and applications for test equipment from other manufacturers.

## Additional Resources

- [Rigol DM3058 Product Page](https://www.rigolna.com/products/digital-multimeters/dm3000/)
- [Rigol DM3058 Programming Guide](https://www.rigolna.com/products/digital-multimeters/dm3000/) (check Downloads section)
- [NI-VISA Documentation](https://www.ni.com/en-us/support/downloads/drivers/download.ni-visa.html)
- [SCPI Standard](https://www.ivifoundation.org/specifications/default.aspx)
- [LXI Consortium](https://www.lxistandard.org/)

## Engineering Notation Credits

Engineering notation formatting by Steve Hageman: http://analoghome.blogspot.com/2012/01/how-to-format-numbers-in-engineering.html

## Related Projects

- **DS1054Z**: Oscilloscope viewer application for Rigol DS1054Z (see `../DS1054Z/`)
- **HPDevices**: Class library for HP/Agilent test equipment (see `../../HPDevices/`)
- **HPTestApps**: Test applications for HP devices (see `../../HPTestApps/`)

## License

This application is part of the GPIBUtils repository. Refer to the repository's main license for usage terms.
