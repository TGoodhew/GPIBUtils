# HPTestApps - Test Applications

Console test applications for testing and demonstrating the HPDevices library classes. Each test application corresponds to a specific HP/Agilent test instrument and provides examples of how to use the device driver classes.

## Overview

HPTestApps is a collection of .NET console applications that serve multiple purposes:
- **Testing**: Validate functionality of HPDevices library classes
- **Examples**: Demonstrate proper usage patterns for each device class
- **Diagnostics**: Help troubleshoot GPIB communication and instrument issues
- **Development**: Aid in developing and debugging new device features

## Projects in Solution

### HP8350BTestApp
Test application for the HP8350B Sweep Oscillator Mainframe.

**Features:**
- Set CW (Continuous Wave) frequencies
- Control output power levels
- Test sweep functionality
- Verify GPIB communication

### HP8673BTestApp
Test application for the HP8673B Synthesized Signal Generator.

**Features:**
- Frequency generation tests
- Power level control verification
- Modulation testing
- Command response validation

### HP8902ATestApp
Test application for the HP8902A Measuring Receiver.

**Features:**
- Frequency measurement tests
- AM modulation depth measurements
- FM deviation measurements
- Phase modulation measurements
- Power measurement validation
- Calibration factor testing
- SRQ (Service Request) functionality verification

### HP5351ATestApp
Test application for the HP5351A/HP53131A Frequency Counter.

**Features:**
- Frequency measurement on both channels
- Resolution and accuracy testing
- SRQ measurement mode verification
- Timeout handling validation

**Note:** This test app is compatible with both HP5351A and HP53131A counters, which share similar command sets.

### TestAppCommon
Shared utilities and common functionality used across multiple test applications.

**Provides:**
- Common helper functions
- Shared GPIB utilities
- Configuration management
- Error handling utilities

## Requirements

### Hardware
- One or more HP/Agilent test instruments (devices being tested)
- GPIB interface card or USB-GPIB adapter
- GPIB cables connecting PC to test equipment
- **Physical test equipment is required** - these applications cannot run without connected hardware

### Software
- .NET Framework 4.7.2 or later
- Visual Studio 2015 or later (for building)
- NI-VISA runtime (National Instruments VISA drivers)
  - Download from: [NI-VISA Downloads](https://www.ni.com/en-us/support/downloads/drivers/download.ni-visa.html)
- HPDevices library (local project reference - automatically built with solution)

### Dependencies
- HPDevices class library (project reference)
- NationalInstruments.Visa (NuGet package)
- TestAppCommon (for shared utilities)

## Building the Solution

### Using Visual Studio
1. Open `HPTestApps.sln` in Visual Studio
2. Restore NuGet packages (right-click solution → Restore NuGet Packages)
3. Build the solution (F6 or Build → Build Solution)
4. Test applications will be output to each project's `bin\Debug\` or `bin\Release\` folder

### Using MSBuild
```bash
msbuild HPTestApps.sln /p:Configuration=Release
```

### Build Output
Each test application compiles to a standalone console executable:
- `HP8350BTestApp.exe`
- `HP8673BTestApp.exe`
- `HP8902ATestApp.exe`
- `HP5351ATestApp.exe`

## Usage

### Running Test Applications

1. **Connect your GPIB hardware**
   - Ensure test equipment is powered on
   - Connect GPIB cables from PC interface to instruments
   - Note the GPIB address of each device (use NI MAX to discover addresses)

2. **Configure GPIB addresses**
   - Most test apps prompt for GPIB address at startup, or
   - Edit the GPIB address in the source code before building, or
   - Pass as command-line argument (if supported by specific test app)

3. **Run the test application**
   ```bash
   # Example: Run from command line
   cd HPTestApps\HP8902ATestApp\bin\Release
   HP8902ATestApp.exe
   ```

4. **Follow on-screen prompts**
   - Test apps typically provide menu-driven interfaces
   - Enter GPIB address when prompted (e.g., `GPIB0::8::INSTR`)
   - Select test operations from the menu
   - View measurement results and status messages

### GPIB Address Format

When prompted for a GPIB address, use the NI-VISA format:
```
GPIB[board]::primary_address::INSTR
```

Examples:
- `GPIB0::8::INSTR` - Device at address 8 on board 0
- `GPIB0::19::INSTR` - Device at address 19 on board 0

**Tip:** Use NI MAX (Measurement & Automation Explorer) to scan for and identify connected GPIB devices.

### Example Session

```
HP8902A Test Application
========================

Enter GPIB Address (e.g., GPIB0::8::INSTR): GPIB0::8::INSTR
Connected to HP8902A at GPIB0::8::INSTR

Select operation:
1. Measure Frequency
2. Measure AM Depth
3. Measure FM Deviation
4. Exit

Enter selection: 1
Measuring frequency...
Result: 1.000000000 GHz

Select operation:
...
```

## Development and Customization

### Adding New Tests

To add new test functionality to an existing test app:

1. Open the test app project in Visual Studio
2. Locate the `Program.cs` file
3. Add new menu options and test methods
4. Use the HPDevices library methods to interact with instruments
5. Follow existing patterns for error handling and user interaction

### Creating a New Test App

To create a test app for a new device:

1. Add a new Console Application project to the solution
2. Reference the HPDevices project
3. Reference TestAppCommon for shared utilities
4. Implement menu-driven interface following existing patterns
5. Add comprehensive tests for all device functionality

## Troubleshooting

### Connection Issues
- **"Resource not found" errors**:
  - Verify GPIB address using NI MAX
  - Check that instrument is powered on
  - Ensure GPIB interface card is installed and drivers are loaded
  - Test connection in NI MAX before running test app

- **Timeout errors**:
  - Some measurements take time; timeouts are pre-configured in device classes
  - Check GPIB cable connections
  - Verify instrument is not in local lockout mode
  - Try resetting the instrument

### Build Issues
- **Missing HPDevices reference**: Ensure HPDevices project builds successfully first
- **NuGet package errors**: Restore NuGet packages before building
- **Missing NI-VISA**: Install NI-VISA runtime from National Instruments

### Runtime Issues
- **Application crashes on startup**:
  - Verify NI-VISA runtime is installed
  - Check that GPIB hardware is connected
  - Try running NI MAX to test GPIB communication independently

- **Incorrect measurements**:
  - Verify instrument is properly configured
  - Check calibration of test equipment
  - Ensure signal levels are within instrument range
  - Review instrument manual for measurement requirements

## Testing Without Hardware

**Note:** These test applications **require physical GPIB hardware** to function. They cannot be run in CI/CD environments or systems without connected test equipment.

For development without hardware:
- Focus on code review and structure validation
- Use NI MAX simulation mode (limited functionality)
- Refer to instrument programming manuals for command validation
- Build the projects to verify compilation and dependencies

## Continuous Integration

Due to hardware dependencies:
- Build verification: ✓ Can be automated
- Unit tests: ✗ Require physical instruments
- Integration tests: ✗ Require physical instruments
- Runtime testing: ✗ Requires GPIB hardware setup

## Additional Resources

- [HPDevices Library Documentation](../HPDevices/README.md)
- [NI-VISA Documentation](https://www.ni.com/en-us/support/downloads/drivers/download.ni-visa.html)
- [NI MAX User Guide](https://www.ni.com/en-us/support/documentation/supplemental/21/getting-started-with-ni-measurement---automation-explorer--ni-max-.html)
- HP/Agilent Instrument Programming Manuals (device-specific)

## Related Projects

- **HPDevices**: Core class library containing device drivers (see `../HPDevices/`)
- **OtherDevices**: Device libraries for non-HP equipment (see `../OtherDevices/`)

## License

These test applications are part of the GPIBUtils repository. Refer to the repository's main license for usage terms.
