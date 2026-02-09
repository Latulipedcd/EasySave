@echo off
setlocal

if defined EASYSAVE_INSTALL_DIR (
    set "INSTALL_DIR=%EASYSAVE_INSTALL_DIR%"
) else (
    set "INSTALL_DIR=%LOCALAPPDATA%\EasySave\bin"
)

echo [1/2] Removing "%INSTALL_DIR%"
if exist "%INSTALL_DIR%" (
    rmdir /S /Q "%INSTALL_DIR%"
)

echo [2/2] Cleaning user PATH
powershell -NoProfile -ExecutionPolicy Bypass -Command "$dir=[Environment]::ExpandEnvironmentVariables('%INSTALL_DIR%');$userPath=[Environment]::GetEnvironmentVariable('Path','User');if(-not $userPath){exit 0};$parts=$userPath -split ';' | Where-Object { $_ -and $_.Trim().Length -gt 0 };$filtered=$parts | Where-Object { $_ -ine $dir };[Environment]::SetEnvironmentVariable('Path',($filtered -join ';'),'User');Write-Host 'PATH cleaned for' $dir"
if errorlevel 1 goto :error

echo Done. Open a new terminal.
exit /b 0

:error
echo Uninstall failed.
exit /b 1
