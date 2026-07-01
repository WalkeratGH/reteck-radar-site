#!/usr/bin/env bash
# Run this on macOS/Linux to start Partner Finder so your phone (on the same
# Wi-Fi) can open it. Keep this terminal open while you use it.
#   1) chmod +x run-on-lan.sh   (first time only)
#   2) ./run-on-lan.sh
cd "$(dirname "$0")" || exit 1
echo "Starting Partner Finder..."
echo "Open this on your phone:  http://YOUR-MAC-IP:5080"
echo "(Find YOUR-MAC-IP in System Settings > Wi-Fi > Details, or run: ipconfig getifaddr en0)"
echo "Press Ctrl+C here to stop."
echo
dotnet run --urls "http://0.0.0.0:5080"
