param(
    [string]$Owner = "TGoodhew",
    [string]$RepoName = "GPIBUtils",
    [switch]$DryRun
)

# Prompt for PAT every run
$securePat = Read-Host -Prompt "Enter GitHub PAT (scope: repo or public_repo)" -AsSecureString
$Token = [Runtime.InteropServices.Marshal]::PtrToStringAuto(
    [Runtime.InteropServices.Marshal]::SecureStringToBSTR($securePat)
)

if ([string]::IsNullOrWhiteSpace($Token)) {
    Write-Error "No token provided. Aborting."
    exit 1
}

# Resolve repo owner/name from git remote if available
try {
    $remote = (git remote get-url origin 2>$null)
    if ($remote -match "github\.com[:/](?<owner>[^/]+)/(?<repo>[^\.]+)") {
        $Owner = $Matches.owner
        $RepoName = $Matches.repo
        Write-Host "Targeting repo $Owner/$RepoName"
    } else {
        Write-Host "Using provided Owner/RepoName: $Owner/$RepoName"
    }
} catch {
    Write-Host "git not available; using provided Owner/RepoName: $Owner/$RepoName"
}

$BaseUri = "https://api.github.com"
$IssuesEndpoint = "$BaseUri/repos/$Owner/$RepoName/issues"
$Headers = @{
    "Authorization" = "Bearer $Token"
    "User-Agent"    = "GPIBUtils-IssueScript"
    "Accept"        = "application/vnd.github+json"
}

function New-GitHubIssue {
    param(
        [string]$Title,
        [string]$Body,
        [string[]]$Labels = @("triage")
    )

    $payload = @{
        title  = $Title
        body   = $Body
        labels = $Labels
    } | ConvertTo-Json -Depth 6

    if ($DryRun) {
        Write-Host "[DRY RUN] POST $IssuesEndpoint"
        Write-Host $payload
        return
    }

    try {
        $resp = Invoke-RestMethod -Method Post -Uri $IssuesEndpoint -Headers $Headers -Body $payload -ContentType "application/json"
        Write-Host "Created issue #$($resp.number): $($resp.title)"
    }
    catch {
        Write-Error "Failed to create issue '$Title': $($_.Exception.Message)"
        if ($_.ErrorDetails.Message) { Write-Host $_.ErrorDetails.Message }
    }
}

$issues = @(
    @{
        Title  = "DS1054Z: SCPI binary block handling should not assume headers or discard bytes"
        Body   = @"
Summary
- QueryBinaryBlock correctly parses SCPI definite-length binary blocks and returns only the payload.
- MainWindow.xaml.cs previously assumed a fixed header size and subtracted 1 from the payload length, which can cause out-of-range access and malformed data.
- Code now uses payload directly; track ensuring all paths avoid header slicing and arbitrary byte discards.

Impact
- Potential ArgumentOutOfRangeException and corrupted waveform data.
- VISA stream desynchronization when mixing text and binary reads.

Fix Recommendations
- Use payload returned from ScpiSession.QueryBinaryBlock() as-is.
- Keep TerminationCharacterEnabled = false for binary operations or manage terminators with short timeouts only if needed.
- Maintain swallowTerminator = false in GetDisplayWaveform.

References
- DS1054Z/MainWindow.xaml.cs → GetDisplayWaveform()
- DS1054Z/ScpiSession.cs → QueryBinaryBlock()
"@
        Labels = @("bug","visa","scpi")
    },
    @{
        Title  = "DS1054Z: Use 1-based channels in initialization"
        Body   = @"
Summary
- DS1054Z channel indices are 1–4. Ensure initialization loops address channels 1–4.

Fix
- for (int ch = 1; ch <= 4; ch++) SendCommand(":CHANnel" + ch + ":DISPlay OFF");

References
- DS1054Z/MainWindow.xaml.cs → InitializeScope()
"@
        Labels = @("bug","scpi")
    },
    @{
        Title  = "DS1054Z: RawIO vs FormattedIO terminator handling can cause IO timeouts"
        Body   = @"
Summary
- With termination char enabled, mixing RawIO and FormattedIO can lead to IOTimeout when reading a terminator after binary blocks.

Fix
- Keep TerminationCharacterEnabled = false for binary reads.
- Avoid forced terminator reads; if needed, use short temporary timeout and ignore if none present.

References
- DS1054Z/MainWindow.xaml.cs → InitializeComms()
- DS1054Z/ScpiSession.cs → QueryBinaryBlock()
"@
        Labels = @("bug","visa","scpi")
    },
    @{
        Title  = "DS1054Z: UI update loop should be non-blocking and paced"
        Body   = @"
Summary
- GetDisplayWaveform runs an infinite loop, uses Dispatcher.Invoke synchronously, and sleeps only 10ms.

Fix
- Use Dispatcher.BeginInvoke to avoid blocking UI thread.
- Increase pacing or adapt based on instrument response.
- Consider CancellationToken to stop cleanly on close.

References
- DS1054Z/MainWindow.xaml.cs → GetDisplayWaveform()
"@
        Labels = @("performance","ui","wpf")
    },
    @{
        Title  = "DS1054Z: Replace Environment.Exit with graceful shutdown"
        Body   = @"
Summary
- Environment.Exit(0) in Window_Closing is abrupt; can leak resources.

Fix
- Signal thread to stop (CancellationTokenSource), join thread.
- Dispose ScpiSession and TcpipSession.

References
- DS1054Z/MainWindow.xaml.cs → Window_Closing()
- DS1054Z/ScpiSession.cs → Dispose()
"@
        Labels = @("cleanup","stability")
    },
    @{
        Title  = "DS1054Z: Hardcoded TCPIP address should be configurable"
        Body   = @"
Summary
- TCPIP address is hardcoded in MainWindow.xaml.cs.

Fix
- Move to config/app settings or UI selection.
- Add connectivity validation and helpful errors.

References
- DS1054Z/MainWindow.xaml.cs → TCPIPAddress
"@
        Labels = @("enhancement","config")
    },
    @{
        Title  = "DS1054Z: Label updates via collection replacement vs property change"
        Body   = @"
Summary
- Replacing LabelItem in the collection raises collection change; fine for ItemsControl. If bound elsewhere, prefer INotifyPropertyChanged.

Fix
- Optionally implement INotifyPropertyChanged in LabelItem and update Text property.

References
- DS1054Z/MainWindow.xaml.cs → Labels updates in GetDisplayWaveform()
"@
        Labels = @("enhancement","wpf")
    },
    @{
        Title  = "DS1054Z: Align waveform STOP range with points"
        Body   = @"
Summary
- ':WAVeform:STOP 1200' is hardcoded; actual points vary with timebase.

Fix
- Query points (':WAVeform:POINts?') or set supported points mode.
- Ensure expected bytes = preamble.points * bytesPerPoint matches payload.

References
- DS1054Z/MainWindow.xaml.cs → InitializeScope(), GetDisplayWaveform()
"@
        Labels = @("bug","scpi")
    },
    @{
        Title  = "TestAppCommon: Console buffer height is fixed at 500"
        Body   = @"
Summary
- Output.SetupConsole fixes BufferHeight=500 which may truncate logs.

Fix
- Allow configuration or increase default.
- Consider file logging.

References
- HPTestApps/TestAppCommon/Output.cs
"@
        Labels = @("enhancement","dx")
    }
)

foreach ($issue in $issues) {
    New-GitHubIssue -Title $issue.Title -Body $issue.Body -Labels $issue.Labels
}

Write-Host "Issue creation completed."