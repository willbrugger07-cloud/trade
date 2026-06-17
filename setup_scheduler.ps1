# Run this script once in PowerShell as Administrator to schedule the trading bot
# It will start automatically at 9:30 AM ET every weekday (Mon-Fri)

$scriptPath = Split-Path -Parent $MyInvocation.MyCommand.Path
$batFile = Join-Path $scriptPath "run_trader.bat"

$action = New-ScheduledTaskAction -Execute $batFile
$trigger = New-ScheduledTaskTrigger -Weekly `
    -DaysOfWeek Monday, Tuesday, Wednesday, Thursday, Friday `
    -At "9:30AM"

$settings = New-ScheduledTaskSettingsSet `
    -ExecutionTimeLimit (New-TimeSpan -Hours 8) `
    -StartWhenAvailable

Register-ScheduledTask `
    -TaskName "RobinhoodTrader" `
    -Action $action `
    -Trigger $trigger `
    -Settings $settings `
    -RunLevel Highest `
    -Force

Write-Host "Trading bot scheduled successfully! It will run every weekday at 9:30 AM."
Write-Host "To remove it later, run: Unregister-ScheduledTask -TaskName 'RobinhoodTrader' -Confirm:`$false"
