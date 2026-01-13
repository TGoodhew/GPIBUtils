# DM3058 User Interface Overview

## Main Window Layout

```
┌─────────────────────────────────────────────────────────────────────┐
│ File   Update Interval                                              │
│  ├─ Settings                                                         │
│  ├─ Logging Configuration...                    [NEW MENU ITEM]     │
│  ├─ Exit                                                             │
│                                                                      │
├─────────────────────────────────────────────────────────────────────┤
│                                                                      │
│  DMM Reading                                                         │
│                                                                      │
│                        3.14159 V                                     │
│                   [LARGE READING DISPLAY]                            │
│                                                                      │
│                                                                      │
├─────────────────────────────────────────────────────────────────────┤
│ [DCV][ACV][DCI][ACI][OHM]  [    RUN    ]  [LOG]  Update: [1 s ▼]  │
│                              (Toggle)    (Toggle)                    │
│                                           [NEW]                      │
└─────────────────────────────────────────────────────────────────────┘
│ ● Connected to 192.168.1.213   Logging: CSV ●  Last: 10:30:45  [Test]│
│   (Status)                     [NEW STATUS]     (Time)        (Button)│
└─────────────────────────────────────────────────────────────────────┘
```

## UI Element Details

### Menu Bar
- **File Menu**
  - Settings: Configure device IP address
  - **Logging Configuration...**: [NEW] Configure log file path and format
  - Exit: Close application

- **Update Interval Menu**
  - Select measurement update rate: 0.1s, 0.5s, 1s, 2s, 5s

### Main Display Area
- Large text showing current measurement value with engineering notation
- Automatically scaled units (V, mV, µV, A, mA, µA, Ω, kΩ, MΩ)

### Control Buttons Row
1. **Measurement Mode Buttons** (Radio buttons)
   - DCV: DC Voltage
   - ACV: AC Voltage
   - DCI: DC Current
   - ACI: AC Current
   - OHM: Resistance

2. **RUN Toggle Button**
   - Green when off: "Run"
   - Red when on: "Stop"
   - Starts/stops measurement acquisition

3. **LOG Toggle Button** [NEW]
   - Green when off: "Log"
   - Red when on: "Log" (active)
   - Starts/stops data logging
   - Independent of RUN button

4. **Update Interval Dropdown**
   - Quick access to change measurement rate
   - Synced with menu selection

### Status Bar
Three sections showing:

1. **Connection Status** (Left)
   - ● Indicator (Gray/Yellow/Green/Red)
   - Text: Connection status and IP address

2. **Logging Status** (Center) [NEW]
   - Text: "Logging: Not configured" / "Logging: Ready" / "Logging: CSV" / "Logging: XML"
   - ● Indicator (Gray/Orange/Green)
     - Gray: Not configured
     - Orange: Configured but not logging
     - Green: Actively logging

3. **Last Update Time** (Right)
   - Timestamp of last successful measurement
   - Test Connection button

## Logging Configuration Dialog

```
┌───────────────────────────────────────────────────────────┐
│ Logging Configuration                                  [X]│
├───────────────────────────────────────────────────────────┤
│                                                           │
│  Log File Path:                                          │
│  ┌───────────────────────────────────────┐  [Browse...]  │
│  │ C:\Users\Name\Documents\data.csv      │               │
│  └───────────────────────────────────────┘               │
│                                                           │
│  Log Format:                                             │
│  ○ CSV    ○ XML                                          │
│                                                           │
│  Note: The log file will be created when logging         │
│  starts. If the file already exists, new data will be    │
│  appended.                                               │
│                                                           │
│                                    [  OK  ]  [ Cancel ]  │
└───────────────────────────────────────────────────────────┘
```

## Color Coding

### Toggle Buttons
- **Green**: Off/Ready state (MediumSeaGreen)
- **Red**: On/Active state (Tomato)

### Status Indicators
- **Gray**: Disconnected/Not configured
- **Yellow**: Connecting/Processing
- **Green**: Connected/Active
- **Red**: Error/Failed
- **Orange**: Configured but inactive

## Workflow Examples

### Example 1: Basic Measurement
```
1. Application starts → Status: Gray "Initializing..."
2. Connection succeeds → Status: Green "Connected to 192.168.1.213"
3. Click [RUN] → Button turns red, shows "Stop"
4. Measurements appear in large display
5. Click [Stop] → Button turns green, shows "Run"
```

### Example 2: Logging Data
```
1. File → Logging Configuration...
2. Browse to select file: C:\data\test.csv
3. Select format: CSV
4. Click OK → Logging Status: Orange "Logging: Ready"
5. Click [RUN] to start measurements
6. Click [LOG] → Button turns red
   Logging Status: Green "Logging: CSV"
7. Data is now being written to file
8. Click [LOG] again → Button turns green
   Logging Status: Orange "Logging: Ready"
   Log file is closed
```

### Example 3: Multiple Sessions (XML)
```
1. Configure XML logging
2. Start measurements [RUN]
3. Start logging [LOG] → Session 1 begins
4. Let it run for 5 minutes
5. Stop logging [LOG] → Session 1 ends
6. Change measurement mode to ACV
7. Start logging [LOG] → Session 2 begins
8. Let it run for 10 minutes
9. Stop logging [LOG] → Session 2 ends
10. XML file contains both sessions with start/end times
```

## Key Features

### Independent Operation
- **Measurements** and **Logging** are completely independent
- You can:
  - Take measurements without logging
  - Configure logging while measurements are running
  - Start/stop logging while measurements continue
  - Change measurement modes while logging

### Persistent Settings
- Log file path and format are saved
- Settings persist across application restarts
- Connection settings are saved separately

### Error Handling
- Invalid file paths are detected during configuration
- Directory creation is offered if path doesn't exist
- Logging automatically stops on errors (prevents data loss)
- Status bar shows clear error messages

### Data Safety
- CSV files are flushed after each write
- XML files are properly closed when logging stops
- Session markers prevent data confusion
- Timestamps ensure data traceability
