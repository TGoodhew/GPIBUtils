# GitHub Copilot Instructions for GPIBUtils

## Project Overview

GPIBUtils is a .NET Framework class library for automating HP/Agilent test equipment via GPIB (General Purpose Interface Bus). The repository contains device-specific classes that provide high-level interfaces to various HP instruments.

## Repository Structure

- **HPDevices/**: Core class library containing device classes
  - Each device has its own subdirectory (e.g., HP8350B, HP8902A, HP53131A)
  - Main library is in `HPDevices/HPDevices/`
  - Device classes implement communication via NationalInstruments.Visa library
- **HPTestApps/**: Test applications for each device
  - Separate console applications for testing each device class
  - Shared test functionality in `TestAppCommon/`
- **OtherDevices/**: Non-HP device implementations

## Technology Stack

- **.NET Framework 4.7.2** (older csproj format)
- **NationalInstruments.Visa** for GPIB communication
- **Visual Studio 2015+ toolchain**
- Build with MSBuild or Visual Studio

## Coding Conventions

### General Style
- Use camelCase for private fields
- Use PascalCase for public properties and methods
- Include XML documentation comments for public APIs
- Keep device command methods simple and focused

### GPIB/VISA Patterns
- All device classes should:
  - Accept GPIB address in constructor (format: "GPIB0::10::INSTR")
  - Initialize ResourceManager and GpibSession in constructor
  - Set appropriate timeout values (consider instrument response time)
  - Enable termination characters for message-based communication
  - Clean up resources properly (dispose pattern)
- Use `SendCommand()` methods for write operations
- Use async/await patterns with SemaphoreSlim for SRQ (Service Request) handling
- Clear the device on initialization with `gpibSession.Clear()`

### Device Class Structure
```csharp
namespace HPDevices.DeviceName
{
    public class Device
    {
        public string gpibAddress { get; }
        private GpibSession gpibSession;
        private ResourceManager resManager;
        
        public Device(string GPIBAddress)
        {
            // Initialize VISA resources
            // Configure timeouts and termination
            // Clear device
        }
        
        // Public methods for device functionality
        // Private helper methods for communication
    }
}
```

## Building and Testing

### Build Commands
- Build specific project: `msbuild HPDevices/HPDevices.sln`
- Build test apps: `msbuild HPTestApps/HPTestApps.sln`
- Or use Visual Studio to build the solution
- Note: These are .NET Framework projects using older csproj format, use `msbuild` not `dotnet build`

### Test Applications
- Each device has a corresponding test application in HPTestApps/
- Test apps are console applications that demonstrate device functionality
- Run test apps only when hardware is connected

## Important Notes

### Hardware Dependencies
- Code requires physical GPIB hardware and NI-VISA drivers installed
- Test apps cannot run in CI/CD without hardware
- Focus on code structure and API design when reviewing

### Device-Specific Considerations
- HP instrument commands are device-specific (see device manuals)
- Command syntax varies between device families
- Some devices use different termination characters or timing requirements
- Frequency, power, and measurement units should be clearly documented in method names and comments

### Common Patterns
- Frequency units: specify in method names (e.g., SetCWFrequency expects Hz)
- Power levels: typically in dBm (DM suffix in commands)
- Use String.Format for command construction with parameters
- Timeouts should account for instrument settling time and measurement duration

## Adding New Devices

When adding a new device class:
1. Add new .cs file to `HPDevices/HPDevices/` directory (e.g., HP12345.cs)
2. Follow existing device class structure (see HP8350B.cs as example)
3. Update HPDevices.csproj to include the new file
4. Create corresponding test application in HPTestApps/
5. Document command syntax and units in XML comments
6. Add device-specific timing considerations
7. Include example usage in test application

## Dependencies

- NationalInstruments.Visa (GPIB communication)
- System.Text.Json (8.0.5)
- Microsoft.Bcl.AsyncInterfaces (8.0.0)
- Various System.* packages for .NET Framework compatibility

## Best Practices

- Always validate GPIB address format before use
- Include error handling for device communication failures
- Set appropriate timeouts based on instrument behavior
- Document expected device responses
- Use async patterns for operations that may block (measurements, sweeps)
- Clean up VISA resources properly to avoid driver issues
