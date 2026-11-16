@echo off
setlocal
set "CSC=%WINDIR%\Microsoft.NET\Framework64\v4.0.30319\csc.exe"
if not exist "%CSC%" set "CSC=%WINDIR%\Microsoft.NET\Framework\v4.0.30319\csc.exe"
"%CSC%" /nologo /codepage:65001 /target:winexe /optimize+ /out:"%~dp0P-EyeRest.exe" /win32icon:"%~dp0EyeRest.ico" /resource:"%~dp0EyeRest.ico",EyeRest.ico /r:System.Windows.Forms.dll /r:System.Drawing.dll "%~dp0EyeRest.cs"
exit /b %ERRORLEVEL%
