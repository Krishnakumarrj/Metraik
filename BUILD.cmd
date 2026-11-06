@echo off
setlocal
cd /d "%~dp0"
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
if not exist "%CSC%" (
  echo .NET Framework 4.x C# compiler was not found.
  exit /b 1
)
echo Building Metraik...
"%CSC%" /nologo /target:winexe /optimize+ /warnaserror+ /platform:anycpu /out:"Metraik.exe" /win32manifest:"source\app.manifest" /reference:System.dll /reference:System.Core.dll /reference:System.Drawing.dll /reference:System.Windows.Forms.dll /reference:System.Xml.dll "source\*.cs"
if errorlevel 1 exit /b 1
echo Build complete: Metraik.exe
exit /b 0
