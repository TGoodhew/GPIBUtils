# HPTestApps - Device Library Test Applications

Console applications for testing and demonstrating the functionality of the HPDevices library. These applications provide interactive examples of device control and automated measurement routines.

## Overview

This solution contains test applications for each supported HP/Agilent device, plus common utilities. Each test application demonstrates typical usage patterns and measurement workflows for its corresponding device class.

## Test Applications

### HP5351ATestApp - HP 5351A Frequency Counter Tests
Interactive console application for testing the HP 5351A microwave frequency counter.

**Features:**
- Frequency measurement demonstrations
- Channel selection testing
- Input impedance configuration
- Measurement accuracy verification

**Usage:** Run the application and follow the interactive prompts.

### HP8350BTestApp - Comprehensive Frequency and Power Test
Automated test suite using multiple instruments for signal generation and measurement validation.

**Instruments Used:**
- HP 8350B - Signal generator (GPIB address 19)
- HP 53131A - Frequency counter (GPIB address 23)
- HP E4418B - Power meter (GPIB address 13)

**Test Sequence:**
1. Initializes all three instruments
2. Prompts user to connect power sensor to reference output for calibration
3. Performs automatic power sensor zeroing and calibration
4. Prompts user to connect test setup (power sensor to DUT, counter to Channel 1)
5. Sweeps 10-220 MHz in 5 MHz steps on Channel 1
6. Prompts user to reconnect to Channel 3
7. Sweeps 225-2400 MHz in 5 MHz steps on Channel 3
8. Records set frequency, measured frequency, and measured power for each point
9. Saves results to `Results.csv` for analysis

**Output Format:**
The test generates a CSV file with three columns:
- Set Frequency (Hz)
- Measured Frequency (Hz)
- Measured Power (dBm)

**Example Output:**
```
Set Frequency is 10.0000 MHz      Actual frequency is 10.0001 MHz      Power is -0.234 dBm
Set Frequency is 15.0000 MHz      Actual frequency is 15.0002 MHz      Power is -0.187 dBm
```

### HP8673BTestApp - HP 8673B Signal Generator Tests
Tests for the HP 8673B synthesized signal generator (2-18 GHz).

**Features:**
- Frequency setting and verification
- Power level control testing
- RF output enable/disable control
- CW mode operation demonstrations

**Usage:** Run the application and follow the interactive prompts to test various signal generator functions.

### HP8902ATestApp - HP 8902A Measuring Receiver Tests
Interactive tests for the HP 8902A measuring receiver.

**Features:**
- Frequency measurement testing
- AM/FM/Phase modulation measurements
- Power sensor calibration factor management (JSON-based)
- Comprehensive receiver functionality demonstrations

**Usage:** Run the application and follow the prompts to exercise different measurement modes.

### TestAppCommon - Shared Utilities
Common functionality used across test applications.

**Contents:**
- `ToEngineeringFormat` class - Converts numeric values to engineering notation with SI prefixes
  - Example: `1500000` → `"1.50 MHz"`
  - Example: `0.000234` → `"234 µV"`
  - Supports prefixes from yocto (10⁻²⁴) to tera (10¹²)

**Usage:** Reference this project from test applications to use shared formatting utilities.

## Requirements

### Hardware
- HP/Agilent test equipment as required by each test application
- GPIB interface hardware (e.g., National Instruments GPIB-USB-HS adapter)
- Proper GPIB cables and connections
- For HP8350BTestApp: Signal routing hardware (cables, adapters, splitters as needed)

### Software
- .NET Framework 4.7.2 or later
- Visual Studio 2015 or later (for building)
- NI-VISA runtime (National Instruments VISA drivers)
- HPDevices library (automatically referenced in solution)

### GPIB Configuration
Each test application expects specific GPIB addresses. Verify your instruments are configured with the correct addresses:
- HP 8350B: Address 19 (in HP8350BTestApp)
- HP 53131A: Address 23 (in HP8350BTestApp)
- HP E4418B: Address 13 (in HP8350BTestApp)

**Note:** You can modify addresses in the source code if your instruments use different GPIB addresses.

## Building the Applications

### Using Visual Studio
1. Open `HPTestApps.sln` in Visual Studio
2. Restore NuGet packages (right-click solution → Restore NuGet Packages)
3. Build the solution (F6 or Build → Build Solution)
4. Navigate to each application's `bin\Debug` or `bin\Release` folder to run executables

### Using MSBuild
```bash
msbuild HPTestApps.sln /p:Configuration=Release
```

## Usage

### Running Test Applications

All test applications are **interactive console applications** that:
- Provide step-by-step instructions
- Wait for user input at key points (e.g., cable connections)
- Display real-time measurement results
- Use color-coded output (green for prompts, white for results, red for errors)
- Generate audible beeps to alert user when action is required

### General Workflow

1. **Build the application** using Visual Studio or MSBuild
2. **Connect test equipment** to GPIB interface
3. **Power on instruments** and verify GPIB communication
4. **Run the executable** from the command line or Visual Studio
5. **Follow on-screen prompts** for cable connections and user actions
6. **Review results** in console output or generated CSV files

### HP8350BTestApp Detailed Example

This is the most comprehensive test application, demonstrating a complete automated measurement routine.

**Setup:**
1. Connect all three instruments (HP 8350B, HP 53131A, HP E4418B) to GPIB interface
2. Verify GPIB addresses match those in the code
3. Have test cables ready for connections
4. Run the application

**Execution Steps:**
1. Application initializes all instruments
2. **First Prompt:** "Connect the power sensor to the reference output. Press any key to continue"
   - Connect the power sensor to the HP 8350B's reference output
   - Press any key to start calibration
   - Calibration runs automatically (zeroing and calibration)
3. **Second Prompt:** "Connect the power sensor to the DUT and the counter to Channel 1. Press any key to continue"
   - Connect power sensor to device under test
   - Connect frequency counter probe to Channel 1 input
   - Press any key to begin measurements
4. **Measurement Phase 1:** 10-220 MHz sweep runs automatically
   - Real-time results displayed in console
   - Data saved to Results.csv
5. **Third Prompt:** "Connect the signal generator to channel 3. Press any key to continue"
   - Reconnect to frequency counter Channel 3
   - Press any key to continue
6. **Measurement Phase 2:** 225-2400 MHz sweep runs automatically
7. **Completion:** "Test completed. Press any key to exit."

**Output Files:**
- `Results.csv` - Contains all measurement data in CSV format for import into Excel or analysis tools

### Tips for Running Tests

- **Warm-up time:** Allow instruments to warm up for accurate measurements (typically 30+ minutes)
- **Check connections:** Verify all cables are properly connected before proceeding at each prompt
- **Monitor results:** Watch for red-colored frequency readings indicating timeout errors
- **Cable quality:** Use high-quality, properly terminated cables for best results
- **Ambient conditions:** Perform measurements in stable temperature environment

## Example Code Patterns

### Basic Device Instantiation and Control
```csharp
// Create device instance
var signalGen = new HPDevices.HP8350B.Device("GPIB0::19::INSTR");

// Set frequency and power
signalGen.SetCWFrequency(100e6); // 100 MHz in Hz
signalGen.SetPowerLevel(0);      // 0 dBm

// Perform measurement
var counter = new HPDevices.HP53131A.Device("GPIB0::23::INSTR");
double freq = counter.MeasureFrequency(1);

// Format and display results
Console.WriteLine("Frequency: {0}", 
    ToEngineeringFormat.Convert(freq, 3, "Hz", true));
```

### Engineering Format Conversion
```csharp
using TestAppCommon;

// Format various values with engineering notation
string voltage = ToEngineeringFormat.Convert(0.00234, 3, "V", true);   // "2.34 mV"
string power = ToEngineeringFormat.Convert(0.001, 3, "W", true);       // "1.00 mW"
string freq = ToEngineeringFormat.Convert(1500000, 4, "Hz", true);     // "1.5000 MHz"
```

### Saving Measurements to CSV
```csharp
// Append measurement data to CSV file
using (StreamWriter sw = File.AppendText("results.csv"))
{
    sw.WriteLine("{0},{1},{2}", setFrequency, measuredFreq, measuredPower);
}
```

## Output Data Analysis

### Results.csv Format
The HP8350BTestApp generates a CSV file with comma-separated values:
```
10000000,10000123.45,-0.234
15000000,15000234.56,-0.187
...
```

**Columns:**
1. Set Frequency (Hz)
2. Measured Frequency (Hz)
3. Measured Power (dBm)

**Analysis Suggestions:**
- Import into Excel or MATLAB for plotting and analysis
- Calculate frequency error: `(Measured - Set) / Set * 100%`
- Plot power vs. frequency to verify flatness
- Identify problematic frequency bands

## Troubleshooting

### Application Won't Start
- **GPIB device not found:** Verify instruments are powered on and GPIB addresses are correct
- **NI-VISA not installed:** Download and install NI-VISA runtime
- **Missing HPDevices.dll:** Build the HPDevices project first

### Measurement Errors
- **Timeout on frequency measurements (red text):** 
  - Signal level too low - increase source power
  - Wrong channel selected - verify cable connections
  - Counter settings incorrect - verify impedance settings
- **Inconsistent power readings:**
  - Power sensor needs calibration - re-run calibration sequence
  - Connections loose - verify all RF connections
  - Ambient temperature change - allow re-stabilization

### Communication Issues
- **GPIB errors:** Check GPIB cables and termination
- **Slow response:** Instruments may need warm-up time
- **Intermittent failures:** Verify GPIB bus is not overloaded

## Extending Test Applications

To create a new test application:
1. Add new Console Application project to the solution
2. Reference the HPDevices project
3. Reference TestAppCommon for engineering format utilities
4. Instantiate device objects with appropriate GPIB addresses
5. Implement test sequence with user prompts and measurements
6. Build and test with actual hardware

## Additional Resources

- [HPDevices Library Documentation](../HPDevices/README.md)
- [National Instruments VISA Documentation](https://www.ni.com/en-us/support/downloads/drivers/download.ni-visa.html)
- [GPIB Basics and Configuration](https://www.ni.com/en-us/support/documentation/gpib-tutorial.html)

## License

These test applications are part of the GPIBUtils repository. Refer to the repository's main license for usage terms.
