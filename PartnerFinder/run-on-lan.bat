@echo off
REM Double-click this file on Windows to start Partner Finder so your phone
REM (on the same Wi-Fi) can open it. Keep this window open while you use it.
cd /d "%~dp0"
echo Starting Partner Finder...
echo Open this on your phone:  http://YOUR-PC-IP:5080
echo (Find YOUR-PC-IP by running ipconfig - look for IPv4 Address)
echo Press Ctrl+C in this window to stop.
echo.
dotnet run --urls "http://0.0.0.0:5080"
pause
