# HPDevices Class Library

A .NET Framework class library containing device driver classes for various HP/Agilent test equipment. This library enables GPIB automation and control of HP test instruments using the NationalInstruments.Visa interface.

## Overview

HPDevices provides high-level .NET interfaces for controlling HP/Agilent test equipment via GPIB (General Purpose Interface Bus). Each device class encapsulates the low-level GPIB commands and provides intuitive methods for instrument control and measurement.

### Supported Devices

The library currently supports the following HP/Agilent test equipment:

- **HP53131A** - Universal Counter
  - High-resolution frequency measurements
  - Dual-channel input
  - SRQ (Service Request) support for asynchronous measurements

- **HP8350B** - Sweep Oscillator Mainframe
  - CW (Continuous Wave) frequency generation
  - Power level control
  - Frequency sweep capabilities

- **HP8673B** - Synthesized Signal Generator
  - Wide frequency range signal generation
  - Precision power control
  - Modulation capabilities

- **HP8902A** - Measuring Receiver
  - AM/FM/Phase modulation measurements
  - Frequency measurements
  - RF power measurements
  - Calibration factor support for accurate power readings
  - SRQ support for async measurement operations

- **HPE4418B** - Power Meter
  - RF power measurements
  - Multiple sensor support
  - Calibration factor management

- **HP5351A** - Frequency Counter
  - Precision frequency measurements
  - Compatible with HP53131A interface

## Requirements

### Hardware
- HP/Agilent test equipment (one or more of the supported devices listed above)
- GPIB interface card or USB-GPIB adapter
- GPIB cables for device connection

### Software
- .NET Framework 4.7.2 or later
- Visual Studio 2015 or later (for building)
- NI-VISA runtime (National Instruments VISA drivers)
  - Download from: [NI-VISA Downloads](https://www.ni.com/en-us/support/downloads/drivers/download.ni-visa.html)

### NuGet Dependencies
The library uses the following NuGet packages:
- `NationalInstruments.Visa` - GPIB/VISA communication library
- `System.Text.Json` (8.0.5) - JSON serialization for calibration data
- `Microsoft.Bcl.AsyncInterfaces` (8.0.0) - Async/await support
- Various `System.*` packages for .NET Framework compatibility

## Building the Library

### Using Visual Studio
1. Open `HPDevices.sln` in Visual Studio
2. Restore NuGet packages (right-click solution → Restore NuGet Packages)
3. Build the solution (F6 or Build → Build Solution)

### Using MSBuild
```bash
msbuild HPDevices.sln /p:Configuration=Release
```

The compiled library will be output to `HPDevices/bin/Release/HPDevices.dll`

## Usage

### Referencing the Library

Add a reference to the `HPDevices.dll` assembly in your project, or reference the HPDevices project directly in your solution.

### Basic Example - HP8350B Sweep Oscillator

```csharp
using HPDevices.HP8350B;

// Create device instance with GPIB address
Device sweepOsc = new Device("GPIB0::19::INSTR");

// Set CW frequency to 1 GHz (frequency in Hz)
sweepOsc.SetCWFrequency(1e9);

// Set power level to -10 dBm
sweepOsc.SetPowerLevel(-10.0);
```

### Basic Example - HP8902A Measuring Receiver

```csharp
using HPDevices.HP8902A;

// Create device instance with GPIB address
Device receiver = new Device("GPIB0::8::INSTR");

// Measure frequency
double frequency = receiver.MeasureFrequency();
Console.WriteLine($"Measured Frequency: {frequency} Hz");

// Measure AM modulation depth
double amDepth = receiver.MeasureAMModulationPercent();
Console.WriteLine($"AM Modulation Depth: {amDepth}%");
```

### Basic Example - HP53131A Universal Counter

```csharp
using HPDevices.HP53131A;

// Create device instance with GPIB address
Device counter = new Device("GPIB0::10::INSTR");

// Measure frequency on channel 1
double freq = counter.MeasureFrequency(1);
Console.WriteLine($"Frequency (CH1): {freq} Hz");
```

### GPIB Address Format

All device classes accept GPIB addresses in the NI-VISA format:
```
GPIB[board]::primary_address::INSTR
```

Examples:
- `GPIB0::8::INSTR` - Device at primary address 8 on board 0
- `GPIB0::19::INSTR` - Device at primary address 19 on board 0

Use NI MAX (National Instruments Measurement & Automation Explorer) to discover connected GPIB devices and their addresses.

## Architecture

### Common Patterns

All device classes follow a consistent pattern:

1. **Constructor**: Accepts GPIB address, initializes VISA resources, configures timeouts and termination
2. **Public Methods**: High-level device functionality (measurements, configuration)
3. **Private Methods**: Low-level GPIB communication helpers
4. **Resource Management**: Proper cleanup of VISA resources
5. **SRQ Handling**: Asynchronous Service Request support using `SemaphoreSlim` for non-blocking measurements

### Threading and Async Operations

The library uses async/await patterns with SRQ (Service Request) handling for operations that may take time to complete:
- Measurements are initiated with appropriate setup commands
- SRQ is configured to signal measurement completion
- SemaphoreSlim provides efficient async waiting for SRQ events
- Prevents blocking the calling thread during long measurements

## Test Applications

For usage examples and testing, see the **HPTestApps** solution which contains console test applications for each device class.

## Known Limitations

- **Hardware required**: All device classes require physical GPIB hardware and instruments to function
- **Single connection**: Each device instance maintains one GPIB connection; create separate instances for multiple devices
- **Thread safety**: Device classes are not thread-safe; use appropriate synchronization if accessing from multiple threads
- **VISA dependency**: Requires NI-VISA runtime to be installed on the system

## Troubleshooting

### GPIB Connection Issues
- **"Resource not found"**: 
  - Verify GPIB address using NI MAX
  - Ensure instrument is powered on and connected
  - Check GPIB interface card is properly installed
- **Timeout errors**: 
  - Increase timeout values for slow operations (some are pre-configured for 20+ seconds)
  - Check GPIB cable connections
  - Verify instrument is responding (test with NI MAX)

### Build Issues
- **Missing NI-VISA references**: Install NI-VISA runtime from [National Instruments website](https://www.ni.com/en-us/support/downloads/drivers/download.ni-visa.html)
- **NuGet package restore failures**: Ensure you have internet connectivity and NuGet package sources are configured correctly

## Additional Resources

- [NI-VISA Documentation](https://www.ni.com/en-us/support/downloads/drivers/download.ni-visa.html)
- [GPIB Tutorial](https://www.ni.com/en-us/support/documentation/supplemental/06/gpib-instrument-control-tutorial.html)
- HP/Agilent Instrument Programming Manuals (device-specific, available from Keysight/Agilent)

## Related Projects

- **HPTestApps**: Console test applications for each device class (see `../HPTestApps/`)
- **OtherDevices**: Similar device libraries for non-HP equipment (see `../OtherDevices/`)

## License

This library is part of the GPIBUtils repository. Refer to the repository's main license for usage terms.
