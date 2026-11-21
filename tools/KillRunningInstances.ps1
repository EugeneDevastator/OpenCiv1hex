param(
	[switch]$Prompt,
	[switch]$Quiet
)

$ErrorActionPreference = 'Stop'

function Get-GameProcesses {
	Get-Process -ErrorAction SilentlyContinue |
		Where-Object { $_.ProcessName -like 'OpenCivOne*' -and $_.Id -ne $PID }
}

function Show-ConfirmMessageBox {
	param([string]$Text)

	if ($env:OS -eq 'Windows_NT') {
		if (-not ('OpenCivOne.NativeMessageBox' -as [type])) {
			Add-Type -Namespace OpenCivOne -Name NativeMessageBox -MemberDefinition @'
[DllImport("user32.dll", CharSet = CharSet.Unicode)]
public static extern int MessageBoxW(System.IntPtr hWnd, string text, string caption, uint type);
'@
		}

		# MB_YESNO | MB_DEFBUTTON2 | MB_ICONWARNING | MB_SETFOREGROUND | MB_TOPMOST
		return [OpenCivOne.NativeMessageBox]::MessageBoxW([System.IntPtr]::Zero, $Text, 'OpenCivOne is running', 0x4 -bor 0x100 -bor 0x30 -bor 0x10000 -bor 0x40000)
	}

	Write-Host $Text
	$answer = Read-Host 'Force kill it? (y/N)'
	if ($answer -match '^(y|yes)$') { return 6 }
	return 7
}

$processes = @(Get-GameProcesses)

if ($processes.Count -eq 0) {
	exit 0
}

$description = ($processes | ForEach-Object { "$($_.ProcessName) (PID $($_.Id))" }) -join ', '
$message = "OpenCivOne is already running:`n`n$description`n`n" +
	"It is keeping the executable locked and must be stopped before building.`n`n" +
	"Do you want to force kill it?"

if ($Quiet -or $env:CI) {
	Write-Host $message
}
else {
	$result = Show-ConfirmMessageBox -Text $message

	if ($result -ne 6) {
		Write-Host 'Build cancelled. Close OpenCivOne manually and try again.'
		exit 1
	}
}

foreach ($process in $processes) {
	try {
		Write-Host "Stopping $($process.ProcessName) (PID $($process.Id))..."
		Stop-Process -Id $process.Id -Force -ErrorAction Stop
		$process.WaitForExit(5000) | Out-Null
	}
	catch {
		Write-Host "Could not stop $($process.ProcessName) (PID $($process.Id)): $($_.Exception.Message)"
	}
	finally {
		$process.Dispose()
	}
}

Start-Sleep -Milliseconds 200
$remaining = @(Get-GameProcesses)

if ($remaining.Count -ne 0) {
	$remainingDescription = ($remaining | ForEach-Object { "$($_.ProcessName) (PID $($_.Id))" }) -join ', '
	Write-Host "OpenCivOne is still running: $remainingDescription"
	exit 1
}

exit 0
