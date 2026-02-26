[CmdletBinding()]
param()

$ErrorActionPreference = "Stop"

function Stop-EasySaveProcess {
    $toStop = Get-Process -Name "EasySave" -ErrorAction SilentlyContinue
    foreach ($p in $toStop) {
        try { Stop-Process -Id $p.Id -Force -ErrorAction Stop } catch {}
    }
}

function Reset-VlcState {
    $vlcPaths = @(
        (Join-Path $env:APPDATA "vlc"),
        (Join-Path $env:LOCALAPPDATA "vlc"),
        (Join-Path $env:LOCALAPPDATA "Temp\.net\EasySave")
    )

    foreach ($path in $vlcPaths) {
        if (Test-Path $path) {
            Remove-Item -Path $path -Recurse -Force -ErrorAction SilentlyContinue
        }
    }
}

Stop-EasySaveProcess
Start-Sleep -Milliseconds 500

Write-Host "Reset etat VLC (cache/profil) ..."
Reset-VlcState
Write-Host "Termine. VLC reset effectue."
