@echo off
setlocal
set CSC=%SystemRoot%\Microsoft.NET\Framework64\v4.0.30319\csc.exe
if not exist "%CSC%" set CSC=%SystemRoot%\Microsoft.NET\Framework\v4.0.30319\csc.exe
if not exist bin mkdir bin
"%CSC%" /nologo /target:winexe /out:bin\NetFix.exe /win32manifest:app.manifest ^
  Program.cs MainForm.cs Core\NetDiag.cs Core\NetRepair.cs ^
  /reference:System.dll /reference:System.Core.dll /reference:System.Windows.Forms.dll /reference:System.Drawing.dll /reference:System.Net.dll /reference:System.Management.dll
if errorlevel 1 (
  echo Build FAILED.
  exit /b 1
)
echo Build OK: bin\NetFix.exe
