# DM3058 - Rigol Digital Multimeter Interface

A WPF application for real-time monitoring and control of the Rigol DM3058 digital multimeter via SCPI commands over TCP/IP or GPIB.

## Overview

This application provides a graphical interface to remotely control and monitor measurements from a Rigol DM3058 digital multimeter. It communicates with the instrument using SCPI (Standard Commands for Programmable Instruments) commands, supporting various measurement modes including DC/AC voltage, DC/AC current, and resistance.

### Features

- **Multiple measurement modes**: DCV, ACV, DCI, ACI, and Ohms (Ω)
- **Real-time readings**: Continuous measurement updates with configurable refresh rate
- **Mode selection buttons**: Quick switching between measurement functions
- **Engineering notation formatting**: Automatic unit scaling (e.g., mV, µA, kΩ)
- **Simple interface**: Clean, intuitive UI for instrument control
- **TCP/IP or GPIB connectivity**: Flexible connection options

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

## Usage

### Starting the Application

1. **Launch the application** - The application will automatically attempt to connect to the multimeter
2. **Connection verification** - If connection fails, verify the IP address and network connectivity
3. **Default mode** - Application starts in DC Voltage (DCV) measurement mode

### Measurement Modes

Click the appropriate button to switch measurement modes:

- **DCV** - DC Voltage measurement
- **ACV** - AC Voltage measurement  
- **DCI** - DC Current measurement
- **ACI** - AC Current measurement
- **OHM** - Resistance (Ohms) measurement

### Reading Measurements

- Measurements update automatically at 1-second intervals (configurable in code)
- Values are displayed with automatic unit scaling using engineering notation
- Examples: `1.234 V`, `123.4 mV`, `12.34 µA`, `1.234 kΩ`

### Making Connections

Before taking measurements:
1. **DC/AC Voltage:** Connect test leads to the multimeter's V/Ω and COM inputs
2. **DC/AC Current:** Connect test leads to the appropriate current input (A or mA) and COM
3. **Resistance:** Connect test leads to the V/Ω and COM inputs
4. Select the appropriate measurement mode in the application
5. Connect the test leads to the circuit or component being measured

## Architecture

### Core Components

#### `MainWindow.xaml`
WPF window defining the user interface:
- Mode selection buttons (DCV, ACV, DCI, ACI, OHM)
- Measurement display area
- Command bindings for mode switching

#### `MainWindow.xaml.cs`
Main application logic:
- **VISA session management**: Initializes TCP/IP or GPIB connection
- **Measurement timer**: Polls the multimeter at regular intervals
- **Mode control**: Sends appropriate SCPI commands for each measurement mode
- **Data formatting**: Converts readings to engineering notation

#### `ToEngineeringFormat.cs`
Utility for formatting numeric values with engineering notation and SI prefixes:
- Converts values like `0.00123` to `"1.23 mV"`
- Supports common metric prefixes from nano to tera
- Shared utility also used in HPDevices test applications

## SCPI Commands

The application uses standard SCPI commands to communicate with the DM3058:

### Measurement Configuration
```
MEAS:VOLT:DC?       # Measure DC voltage
MEAS:VOLT:AC?       # Measure AC voltage
MEAS:CURR:DC?       # Measure DC current
MEAS:CURR:AC?       # Measure AC current
MEAS:RES?           # Measure resistance
```

**Note:** The leading colon (`:`) is optional in SCPI commands. The application uses commands without the leading colon as shown above.

### Command Structure
All measurement commands follow the SCPI query format with a question mark (`?`) to request a reading. The multimeter responds with a numeric value that the application parses and formats for display.

## Timer Configuration

The measurement update rate is controlled by a DispatcherTimer:
```csharp
_readTimer.Interval = TimeSpan.FromSeconds(1); // 1 second updates
```

To change the update rate:
1. Edit the interval in `InitializeTimer()` method
2. Rebuild the application
3. Shorter intervals provide faster updates but increase bus traffic

## Engineering Notation Format

The ToEngineeringFormat utility automatically scales values with SI prefixes:

| Range | Prefix | Example |
|-------|--------|---------|
| ≥1e12 | T (tera) | 1.5e12 → "1.50 T" |
| ≥1e9 | G (giga) | 2.4e9 → "2.40 G" |
| ≥1e6 | M (mega) | 3.3e6 → "3.30 M" |
| ≥1e3 | k (kilo) | 4.7e3 → "4.70 k" |
| ≥1 | (base) | 5.0 → "5.00" |
| ≥1e-3 | m (milli) | 6e-3 → "6.00 m" |
| ≥1e-6 | µ (micro) | 7e-6 → "7.00 µ" |
| <1e-6 | n (nano) and smaller | 8e-9 → "8.00 n" |

## Known Limitations

- **Single multimeter support**: Application connects to one multimeter at a time
- **No data logging**: Measurements are not automatically saved (display only)
- **Limited error handling**: Communication errors may require application restart
- **Manual address configuration**: IP address changes require code modification and rebuild
- **Fixed update rate**: Timer interval requires code change to adjust

## Troubleshooting

### Connection Issues
- **"Unable to connect"**: 
  - Verify multimeter IP address in code matches actual device address
  - Check network connectivity with `ping` command
  - Ensure multimeter is powered on and network interface is active
  - Verify NI-VISA is properly installed
- **Timeout errors**: 
  - Check firewall settings - VISA may be blocked
  - Ensure no other applications are connected to the multimeter
  - Verify VISA address format is correct

### Measurement Issues
- **No readings or zero values**: 
  - Verify correct measurement mode is selected
  - Check test lead connections to multimeter
  - Ensure signal is within multimeter's measurement range
- **Erratic readings**:
  - Check for loose connections
  - Verify proper test lead connection to COM terminal
  - Allow multimeter to warm up (typically 30 minutes)

### Build Issues
- **Missing NI-VISA references**: 
  - Install NI-VISA runtime from National Instruments website
  - Or install NI Measurement & Automation Explorer (NI MAX)
- **Missing WPF dependencies**: 
  - Ensure .NET Framework 4.8.1 SDK is installed
  - Verify Visual Studio has WPF workload installed
- **NuGet restore fails**:
  - Check internet connection
  - Clear NuGet cache: `nuget locals all -clear`
  - Restore manually from Package Manager Console

## Additional Resources

- [Rigol DM3058/DM3058E Programming Guide](https://www.rigolna.com/products/digital-multimeters/dm3000/)
- [NI-VISA Documentation](https://www.ni.com/en-us/support/downloads/drivers/download.ni-visa.html)
- [SCPI Standard Commands](https://www.ivifoundation.org/specifications/default.aspx)
- [National Instruments VISA Tutorial](https://www.ni.com/en-us/support/documentation/supplemental/visa-tutorial.html)

## Related Projects

This application is part of the GPIBUtils repository, which also includes:
- **HPDevices** - Library for HP/Agilent test equipment
- **HPTestApps** - Test applications for HP devices
- **DS1054Z** - Oscilloscope viewer application

## License

This application is part of the GPIBUtils repository. Refer to the repository's main license for usage terms.

## Credits

Engineering notation formatting by Steve Hageman: http://analoghome.blogspot.com/2012/01/how-to-format-numbers-in-engineering.html
