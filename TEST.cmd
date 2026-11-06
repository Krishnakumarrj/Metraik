@echo off
setlocal
cd /d "%~dp0"
call BUILD.cmd
if errorlevel 1 exit /b 1
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "artifacts" mkdir "artifacts"
"%CSC%" /nologo /target:exe /warnaserror+ /platform:anycpu /main:Metraik.Tests.Suite /out:"artifacts\Metraik.Tests.exe" /win32manifest:"source\app.manifest" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll "source\*.cs" "tests\*.cs"
if errorlevel 1 exit /b 1
"artifacts\Metraik.Tests.exe"
exit /b %errorlevel%
