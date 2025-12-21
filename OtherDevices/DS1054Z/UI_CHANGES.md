# DS1054Z Configuration Feature - UI Changes

## Summary of Changes

This document describes the UI changes made to support configurable TCPIP address in the DS1054Z application.

## 1. Main Window Menu Bar (NEW)

A menu bar has been added to the top of the main window:

```
+----------------------------------------------------------+
| File                                                      |
|   Settings                                                |
|   -----                                                   |
|   Exit                                                    |
+----------------------------------------------------------+
```

The menu provides:
- **File → Settings**: Opens the configuration dialog to set the oscilloscope IP address
- **File → Exit**: Closes the application

## 2. Settings Dialog (NEW)

When you click **File → Settings**, a new dialog window appears:

```
+----------------------------------------------------+
| DS1054Z Settings                              [X] |
|----------------------------------------------------|
|                                                    |
| TCPIP Address Configuration                        |
|                                                    |
| Enter the IP address of the DS1054Z oscilloscope: |
|                                                    |
| [192.168.1.145                                 ]   |
|                                                    |
| [Status/error message appears here if invalid]     |
|                                                    |
|                                    [ OK ] [Cancel] |
+----------------------------------------------------+
```

Features:
- **IP Address Input**: Text box for entering the oscilloscope's IP address
- **Validation**: Real-time validation of IP address format (xxx.xxx.xxx.xxx)
- **Error Messages**: Clear feedback if the address is invalid
- **OK Button**: Saves the address and closes the dialog
- **Cancel Button**: Discards changes and closes the dialog

### Validation Rules:
1. Address cannot be empty
2. Must match format: xxx.xxx.xxx.xxx
3. Each octet must be 0-255
4. Shows specific error messages for each validation failure

### Example Error Messages:
- "IP address cannot be empty."
- "Invalid IP address format. Expected format: xxx.xxx.xxx.xxx"
- "IP address octets must be between 0 and 255."

## 3. Connection Error Dialog (ENHANCED)

When the application fails to connect to the oscilloscope, a new error dialog appears:

```
+----------------------------------------------------+
| Connection Error                              [X] |
|----------------------------------------------------|
|  [!]                                               |
|      Failed to connect to oscilloscope at          |
|      192.168.1.145                                 |
|                                                    |
|      Error: <specific error message>               |
|                                                    |
|      Click 'OK' to try again or 'Cancel' to        |
|      change settings.                              |
|                                                    |
|                                    [ OK ] [Cancel] |
+----------------------------------------------------+
```

Behavior:
- **OK Button**: Retries connection with the same address
- **Cancel Button**: Opens the Settings dialog to change the IP address
- If user cancels the Settings dialog, the application exits gracefully

## 4. Settings Persistence

Settings are now persisted using .NET application settings:

### Default Value:
- **Default IP Address**: 192.168.1.145

### Storage Location:
Settings are stored in the user's local application data folder:
```
%LOCALAPPDATA%\<Company>\DS1054Z.exe_<hash>\<version>\user.config
```

### Behavior:
1. On first run, the default address (192.168.1.145) is used
2. Changes made via the Settings dialog are saved immediately
3. The saved address is loaded on subsequent application starts
4. Application must be restarted for address changes to take effect

## 5. Application Flow

### First Time User:
1. Application starts with default address (192.168.1.145)
2. If connection fails, error dialog appears
3. User can change address via Settings dialog
4. Address is saved for future runs

### Returning User:
1. Application starts with saved address
2. If address needs to change, use **File → Settings**
3. Application prompts to restart after saving new address

## Implementation Details

### Files Modified:
- `MainWindow.xaml` - Added menu bar
- `MainWindow.xaml.cs` - Updated to use settings, added menu handlers and connection error handling
- `Properties/Settings.settings` - Added TCPIPAddress setting
- `Properties/Settings.Designer.cs` - Auto-generated setting property
- `App.config` - Added user settings configuration

### Files Created:
- `ConfigDialog.xaml` - Settings dialog UI
- `ConfigDialog.xaml.cs` - Settings dialog logic and validation

### Key Changes:
1. Removed hardcoded TCPIP address from MainWindow.xaml.cs
2. Address now retrieved from `Properties.Settings.Default.TCPIPAddress`
3. Added IP address validation with helpful error messages
4. Settings automatically persist across application restarts
5. Enhanced connection error handling with user-friendly dialogs
