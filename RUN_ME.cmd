@echo off
setlocal
cd /d "%~dp0"
if not exist "Metraik.exe" (
  call "%~dp0BUILD.cmd"
  if errorlevel 1 (
    echo Build failed. No app was started.
    pause
    exit /b 1
  )
)
start "" "%~dp0Metraik.exe"
exit /b 0
