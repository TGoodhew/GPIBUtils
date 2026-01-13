# Data Logging Feature Documentation

## Overview
The DM3058 Display Utility now includes comprehensive data logging capabilities that allow you to record measurements to either CSV or XML format files.

## Features

### Independent Operation
- Logging can be started and stopped independently of measurements
- Measurements continue running while logging is turned on or off
- Logging state is preserved across configuration changes

### Supported Formats

#### CSV Format
- Simple comma-separated values format
- Columns: Timestamp, Mode, Value, Unit
- Example:
  ```
  Timestamp,Mode,Value,Unit
  # Logging started at 2026-01-13 12:30:45
  2026-01-13 12:30:45.123,DCV,3.14159,V
  2026-01-13 12:30:46.234,DCV,3.14155,V
  ```
- Ideal for import into Excel, MATLAB, Python pandas, etc.

#### XML Format
- Structured XML with session tracking
- Each logging session is recorded with start and end times
- Individual readings are nested within sessions
- Example:
  ```xml
  <?xml version="1.0" encoding="utf-8"?>
  <MeasurementLog>
    <Session StartTime="2026-01-13 12:30:45" EndTime="2026-01-13 12:35:12">
      <Reading Timestamp="2026-01-13 12:30:45.123" Mode="DCV" Value="3.14159" Unit="V"/>
      <Reading Timestamp="2026-01-13 12:30:46.234" Mode="DCV" Value="3.14155" Unit="V"/>
    </Session>
  </MeasurementLog>
  ```
- Ideal for structured data analysis and long-term archival

## How to Use

### Configuring Logging

1. **Open Configuration Dialog**
   - Click **File → Logging Configuration...** in the menu
   
2. **Set Log File Path**
   - Click the **Browse...** button to select a location
   - Choose a filename (e.g., `measurements.csv` or `data.xml`)
   - The directory will be created automatically if it doesn't exist

3. **Choose Format**
   - Select **CSV** for simple comma-separated format
   - Select **XML** for structured XML format
   
4. **Save Configuration**
   - Click **OK** to save your settings
   - Settings are persisted across application restarts

### Starting and Stopping Logging

1. **Start Logging**
   - Click the **Log** toggle button (turns red when active)
   - Logging status in the status bar changes to show active logging
   - All measurements taken while button is checked are logged

2. **Stop Logging**
   - Click the **Log** toggle button again to turn it off
   - The log file is properly closed and saved
   - You can restart logging at any time

### Understanding the Status Bar

The status bar shows the current logging state:

- **"Logging: Not configured"** (Gray indicator)
  - Logging has not been configured yet
  - Click File → Logging Configuration to set it up

- **"Logging: Ready"** (Orange indicator)
  - Logging is configured but not currently active
  - Click the Log button to start logging

- **"Logging: CSV"** or **"Logging: XML"** (Green indicator)
  - Actively logging data in the specified format
  - Data is being written to the log file

## File Management

### Appending vs. Creating New Files
- If the log file already exists, new data is **appended** to it
- For CSV: A session start marker comment is added
- For XML: A new Session element is added to the existing structure

### File Locations
- Log files can be saved anywhere on your system
- Recommended locations:
  - `Documents\DM3058_Logs\`
  - Desktop for quick access
  - Network drives for shared access

### Automatic Directory Creation
- The application automatically creates directories if they don't exist
- You'll be prompted to confirm directory creation

## Data Analysis

### CSV Files
CSV files can be easily imported into:
- **Microsoft Excel**: Open directly or use Data → From Text/CSV
- **Python pandas**: `pd.read_csv('measurements.csv', comment='#')`
- **MATLAB**: `readtable('measurements.csv')`
- **R**: `read.csv('measurements.csv', comment.char='#')`

### XML Files
XML files can be processed with:
- **Python**: Use `xml.etree.ElementTree` or `lxml`
- **.NET**: Use `System.Xml.Linq` (XDocument)
- **XSLT**: Transform to other formats
- **XML editors**: View/edit structure directly

## Troubleshooting

### "Logging Not Configured" Error
**Problem**: Clicking the Log button shows an error about logging not being configured.
**Solution**: Go to File → Logging Configuration and set up a log file path.

### File Access Errors
**Problem**: Error message when starting logging about file access.
**Solution**: 
- Check that the file is not open in another application
- Ensure you have write permissions to the directory
- Try a different file location

### Logging Stops Unexpectedly
**Problem**: The Log button turns off automatically.
**Solution**: 
- Check the log file hasn't reached a size limit
- Ensure disk space is available
- Check the debug output for error messages

### Configuration Changes Stop Logging
**Problem**: Opening the configuration dialog stops active logging.
**Solution**: This is by design to safely change settings. Simply click the Log button again to resume logging with the new configuration.

## Best Practices

1. **File Organization**
   - Use descriptive filenames with dates: `dmm_measurements_2026-01-13.csv`
   - Keep logs organized in dedicated folders
   - Consider separate folders for different projects or experiments

2. **Format Selection**
   - Use **CSV** for simple data collection and analysis
   - Use **XML** when you need session tracking or structured data
   - Use **CSV** for maximum compatibility with analysis tools

3. **Long-term Logging**
   - For extended logging sessions, periodically check disk space
   - Consider using XML for better organization of multiple sessions
   - Close and restart logging sessions periodically to create checkpoints

4. **Data Review**
   - Periodically review log files to ensure data quality
   - Back up important measurement data
   - Document experimental conditions in separate notes files

## Technical Details

### Timestamps
- All timestamps are in local time
- Format: `yyyy-MM-dd HH:mm:ss.fff` (includes milliseconds)
- Consistent across both CSV and XML formats

### Measurement Modes
- **DCV**: DC Voltage measurement
- **ACV**: AC Voltage measurement  
- **DCI**: DC Current measurement
- **ACI**: AC Current measurement
- **OHM**: Resistance measurement

### Units
- Voltage: **V** (Volts)
- Current: **A** (Amperes)
- Resistance: **Ω** (Ohms)

### Data Flushing
- CSV files are flushed after each write for data safety
- XML files are saved completely when logging stops
- Data loss is minimized even if application crashes during CSV logging

## Examples

### Example 1: Basic CSV Logging
1. File → Logging Configuration
2. Browse to `C:\Users\YourName\Documents\test.csv`
3. Select CSV format
4. Click OK
5. Click Run to start measurements
6. Click Log to start logging
7. Let it run for desired duration
8. Click Log to stop logging

Result: CSV file with timestamped measurements ready for analysis.

### Example 2: Multi-Session XML Logging
1. Configure XML logging to `experiment_data.xml`
2. Click Log to start first session
3. Click Log to stop after condition 1
4. Change measurement mode or conditions
5. Click Log to start second session
6. Click Log to stop

Result: XML file containing multiple sessions, each with its own start/end time and measurements.

### Example 3: Long-term Monitoring
1. Configure logging to a descriptive filename
2. Start measurements with Run button
3. Start logging with Log button
4. Leave running for hours/days
5. Check status bar periodically to ensure logging is active
6. Stop logging when monitoring period is complete

Result: Complete measurement history for trend analysis and quality control.
