# HPDevices - GPIB/VISA Device Library

A C# class library for automating HP/Agilent test equipment via GPIB (General Purpose Interface Bus) using the National Instruments VISA interface.

## Overview

This library provides a high-level interface to control and measure with HP/Agilent test equipment through GPIB communication. Each device class encapsulates the specific command set and functionality for its instrument, making it easy to integrate test equipment into automated test systems.

## Supported Devices

### HP 53131A - Universal Counter
- **Frequency measurements** up to 225 MHz (Channel 1) or 3 GHz (Channel 3)
- Configurable input impedance (50Ω or 1MΩ)
- High-resolution measurements with automatic ranging
- Service Request (SRQ) support for measurement complete signaling

### HP 8350B - Sweep Oscillator
- **CW frequency control** (continuous wave operation)
- **Power level control** in dBm
- Frequency range dependent on installed plug-in modules
- Simple command interface for signal generation

### HP 8673B - Synthesized Signal Generator
- **Frequency range:** 2-18 GHz
- **CW frequency control** with high stability
- **Power level control** in dBm
- **RF output enable/disable** control
- Advanced SRQ features for sweep and source settling detection

### HP 8902A - Measuring Receiver
- **AM/FM/Phase modulation measurements**
- **Frequency measurements** with high accuracy
- **Power sensor calibration** support with JSON-based calibration factor storage
- Comprehensive measurement capabilities for communications testing
- SRQ support for data ready and error conditions

### HP E4418B - Power Meter
- **Power measurements** with calibration support
- **Automatic sensor zeroing and calibration** routines
- Frequency-dependent power measurements
- High accuracy for RF power testing

## Requirements

### Hardware
- HP/Agilent test equipment (one or more of the supported devices)
- GPIB interface hardware (e.g., National Instruments GPIB-USB-HS adapter)
- Properly configured GPIB bus with correct addressing

### Software
- .NET Framework 4.7.2 or later
- Visual Studio 2015 or later (for building)
- NI-VISA runtime (National Instruments VISA drivers)

### NuGet Dependencies
- `NationalInstruments.Visa` - GPIB/VISA communication library
- `System.Text.Json` (8.0.5) - JSON serialization for calibration data
- `Microsoft.Bcl.AsyncInterfaces` (8.0.0) - Async support

## Building the Library

### Using Visual Studio
1. Open `HPDevices.sln` in Visual Studio
2. Restore NuGet packages (right-click solution → Restore NuGet Packages)
3. Build the solution (F6 or Build → Build Solution)

### Using MSBuild
```bash
msbuild HPDevices.sln /p:Configuration=Release
```

## Usage

### Basic Device Instantiation

Devices are instantiated using their GPIB address in the format `GPIB0::XX::INSTR` where `XX` is the device's GPIB address (typically 0-30).

```csharp
using HPDevices.HP8350B;
using HPDevices.HP53131A;
using HPDevices.E4418B;

// Create device instances with their GPIB addresses
var signalGenerator = new HPDevices.HP8350B.Device("GPIB0::19::INSTR");
var frequencyCounter = new HPDevices.HP53131A.Device("GPIB0::23::INSTR");
var powerMeter = new HPDevices.E4418B.Device("GPIB0::13::INSTR");

// Set signal generator frequency to 100 MHz
signalGenerator.SetCWFrequency(100e6); // Frequency in Hz

// Set output power to 0 dBm
signalGenerator.SetPowerLevel(0);

// Measure frequency on channel 1
double measuredFreq = frequencyCounter.MeasureFrequency(1);

// Measure power at 100 MHz
double measuredPower = powerMeter.MeasurePower(100); // Frequency in MHz
```

### GPIB Address Configuration

Each device on the GPIB bus must have a unique address (0-30). Configure addresses on the instruments:
- Typically found in the instrument's **System** or **GPIB** settings menu
- Common addresses: 10-30 (0-9 often reserved for controllers and special functions)
- Verify addresses using NI MAX (National Instruments Measurement & Automation Explorer)

## Device Features

### HP 53131A Universal Counter
- `MeasureFrequency(int channel)` - Measure frequency on specified channel (1, 2, or 3)
- `Set50OhmImpedance(bool enable)` - Configure input impedance
- Automatic SRQ-based measurement completion
- Configurable timeout for low-frequency measurements (default: 20 seconds)

### HP 8350B Sweep Oscillator
- `SetCWFrequency(double frequency)` - Set CW frequency in Hz
- `SetPowerLevel(double power)` - Set output power in dBm
- Simple, reliable command interface
- Instrument preset on initialization

### HP 8673B Synthesized Signal Generator
- `SetCWFrequency(double frequency)` - Set frequency in Hz (2-18 GHz range)
- `SetPowerLevel(double power)` - Set power level in dBm
- `SetRFOutputOn(bool enable)` - Enable/disable RF output
- SRQ mask control for advanced sweep operations
- Source settling detection

### HP 8902A Measuring Receiver
- `MeasureMWFrequency()` - Measure RF carrier frequency
- `MeasureAMModulation()` - Measure AM modulation depth
- `MeasureFMModulation()` - Measure FM deviation
- `MeasurePhaseModulation()` - Measure phase deviation
- `LoadCalibrationFactors(string filename)` - Load sensor calibration from JSON
- `SaveCalibrationFactors(string filename)` - Save sensor calibration to JSON
- Comprehensive error detection via SRQ

### HP E4418B Power Meter
- `MeasurePower(int frequency)` - Measure power at specified frequency (MHz)
- `ZeroAndCalibrateSensor()` - Perform automatic zero and calibration sequence
- Waits for calibration completion using SRQ
- Accurate measurements across wide frequency range

## Communication Details

All device classes follow a consistent pattern:
- **Initialization:** Open GPIB session, set timeout (20 seconds default), enable termination characters
- **Clearing:** Send device clear command on startup
- **SRQ Handling:** Use SemaphoreSlim for asynchronous Service Request waiting
- **Command Methods:** Simple, descriptive method names for all device operations
- **Error Handling:** Automatic timeout and communication error detection

### Typical Command Flow
1. Instantiate device with GPIB address
2. Device automatically initializes and clears
3. Call methods to configure and control device
4. Methods with measurements use SRQ to wait for completion
5. Results returned directly from measurement methods

## Thread Safety

Device classes use SemaphoreSlim for SRQ event handling, providing:
- Asynchronous wait for measurement completion
- Thread-safe operation
- Proper event synchronization

## Known Limitations

- **Single connection per device:** Each device instance maintains one GPIB session
- **Blocking operations:** Measurement methods block until completion or timeout
- **No automatic retry:** Communication errors require re-initialization
- **Fixed timeouts:** 20-second timeout suitable for most operations but may need adjustment for very low frequency measurements

## Troubleshooting

### GPIB Connection Issues
- **"Resource not found"**: Verify GPIB address matches instrument configuration
- **Timeout errors**: Check GPIB cable connections and termination
- **Communication failures**: Ensure NI-VISA drivers are properly installed
- **Device not responding**: Verify instrument is powered on and not in local mode

### Measurement Issues
- **Zero readings**: Check for timeout - may need longer timeout for low-frequency measurements
- **Inconsistent results**: Ensure proper settling time between measurements
- **SRQ not firing**: Verify instrument supports and is configured for SRQ operation

### Build Issues
- **Missing NI-VISA references**: Install NI-VISA runtime from National Instruments
- **Missing NuGet packages**: Restore packages in Visual Studio or use `nuget restore`

## Additional Resources

- [National Instruments VISA Documentation](https://www.ni.com/en-us/support/downloads/drivers/download.ni-visa.html)
- [GPIB Tutorial and Basics](https://www.ni.com/en-us/support/documentation/gpib-tutorial.html)
- HP/Agilent instrument programming manuals (available from Keysight Technologies)

## Test Applications

See the `HPTestApps` folder for example console applications demonstrating usage of each device class.

## License

This library is part of the GPIBUtils repository. Refer to the repository's main license for usage terms.
