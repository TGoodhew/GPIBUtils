# Quick Start Guide - Data Logging

## 5-Minute Setup

### Step 1: Configure Logging (One-Time Setup)
1. Open the application
2. Go to **File → Logging Configuration...**
3. Click **Browse...** 
4. Choose a location like `C:\Users\YourName\Documents\DMM_Logs\measurements.csv`
5. Select **CSV** format
6. Click **OK**

### Step 2: Start Taking Measurements
1. Click the **Run** button (it will turn red)
2. Watch measurements appear on screen
3. Select measurement mode as needed (DCV, ACV, DCI, ACI, OHM)

### Step 3: Start Logging
1. Click the **Log** button (it will turn red)
2. Status bar shows "Logging: CSV" with green indicator
3. Your measurements are now being saved!

### Step 4: Stop Logging
1. Click the **Log** button again (it will turn green)
2. Status bar shows "Logging: Ready" with orange indicator
3. Your log file is closed and saved

## View Your Data

### In Excel
1. Open Excel
2. Go to **Data → From Text/CSV**
3. Select your log file
4. Click **Import**
5. Data appears in columns: Timestamp, Mode, Value, Unit

### In Notepad
1. Right-click your log file
2. Select **Open with → Notepad**
3. View raw CSV data

## Common Usage Patterns

### Pattern 1: Quick Test
```
Configure logging once → Start Run → Start Log → Wait 30 seconds → Stop Log
```
Result: 30 seconds of data in your file

### Pattern 2: Long-term Monitoring
```
Configure logging once → Start Run → Start Log → Leave overnight → Stop Log
```
Result: Hours of continuous data

### Pattern 3: Multiple Sessions
```
Start Log → Test condition 1 → Stop Log
Change settings or mode
Start Log → Test condition 2 → Stop Log
```
Result: Two separate sessions in the same file

## Tips

✓ **DO:**
- Configure logging once, use it many times
- Keep the Log and Run buttons independent
- Use descriptive filenames with dates
- Check the status bar to confirm logging is active

✗ **DON'T:**
- Don't open the log file in Excel while logging (file will be locked)
- Don't worry about overwriting - new data is always appended
- Don't forget to click Stop Log when finished (or it logs forever!)

## Troubleshooting

**Problem**: Log button turns off immediately
- **Solution**: Configure logging first (File → Logging Configuration)

**Problem**: Can't find my log file
- **Solution**: Check the path in File → Logging Configuration

**Problem**: Excel shows weird formatting
- **Solution**: Use Data → From Text/CSV instead of just opening the file

**Problem**: Log file is huge
- **Solution**: Stop logging when not needed, start a new file periodically

## What's Being Logged?

Every line in your CSV contains:
```
Timestamp              Mode  Value      Unit
2026-01-13 10:15:30.125, DCV, 3.14159,  V
```

You can sort by timestamp, filter by mode, calculate statistics, create graphs, etc.

## Next Steps

For more details, see:
- [LOGGING_FEATURE.md](LOGGING_FEATURE.md) - Complete documentation
- [UI_OVERVIEW.md](UI_OVERVIEW.md) - User interface guide
- [example_measurements.csv](example_measurements.csv) - Example output
- [example_measurements.xml](example_measurements.xml) - Example XML output
