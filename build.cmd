@echo off
setlocal

rem Builds the Windows game. If OpenCivOne is still running, a message box asks
rem whether to force kill it so the executable can be replaced.

powershell -NoProfile -ExecutionPolicy Bypass -File "%~dp0build.ps1" %*
set "RESULT=%ERRORLEVEL%"

if not "%RESULT%"=="0" (
	echo.
	echo Build failed with exit code %RESULT%.
	pause
)

exit /b %RESULT%
