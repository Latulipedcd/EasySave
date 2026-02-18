@echo off
setlocal

set "SCRIPT_DIR=%~dp0"
for %%I in ("%SCRIPT_DIR%..") do set "REPO_ROOT=%%~fI"

if defined EASYSAVE_INSTALL_DIR (
    set "INSTALL_DIR=%EASYSAVE_INSTALL_DIR%"
) else (
    set "INSTALL_DIR=%LOCALAPPDATA%\EasySave\bin"
)
set "GUI_DIR=%INSTALL_DIR%\gui"

set "SKIP_PATH=0"
if /I "%~1"=="--skip-path" set "SKIP_PATH=1"

echo [1/5] Preparing install directory "%INSTALL_DIR%"
if exist "%INSTALL_DIR%" (
    rmdir /S /Q "%INSTALL_DIR%"
)
mkdir "%INSTALL_DIR%"
if errorlevel 1 goto :error

echo [2/5] Publishing EasySave CLI to "%INSTALL_DIR%"
dotnet publish "%REPO_ROOT%\src\Console\Console.csproj" -c Release -o "%INSTALL_DIR%"
if errorlevel 1 goto :error

if not exist "%INSTALL_DIR%\EasySave.exe" (
    echo EasySave.exe was not generated.
    goto :error
)

echo [3/5] Publishing EasySave GUI to "%GUI_DIR%"
dotnet publish "%REPO_ROOT%\src\GUI\GUI.csproj" -c Release -o "%GUI_DIR%"
if errorlevel 1 goto :error

if not exist "%GUI_DIR%\EasySave.exe" (
    echo EasySave.exe was not generated.
    goto :error
)

(
    echo @echo off
    echo "%%~dp0EasySave.exe" %%*
) > "%INSTALL_DIR%\EasySave.cmd"
if errorlevel 1 goto :error

if "%SKIP_PATH%"=="1" goto :done

echo [4/5] Updating user PATH
powershell -NoProfile -ExecutionPolicy Bypass -Command "$dir=[Environment]::ExpandEnvironmentVariables('%INSTALL_DIR%');$userPath=[Environment]::GetEnvironmentVariable('Path','User');$parts=@();if($userPath){$parts=$userPath -split ';'};if($parts -contains $dir){Write-Host 'PATH already contains' $dir;exit 0};$newParts=($parts + $dir | Where-Object { $_ -and $_.Trim().Length -gt 0 } | Select-Object -Unique);[Environment]::SetEnvironmentVariable('Path',($newParts -join ';'),'User');Write-Host 'PATH updated with' $dir"
if errorlevel 1 goto :error

:done
echo [5/5] Done
echo Open a new terminal and run:
echo   EasySave 1-3
echo   EasySave "1;3"
echo   EasySave 1 3
exit /b 0

:error
echo Installation failed.
exit /b 1
