@echo off
title GameTrack Client
cd /d "%~dp0client"

if not exist "node_modules" (
    echo Installing dependencies...
    call npm.cmd install
)

echo ==========================================
echo   Frontend: http://localhost:4200
echo ==========================================
echo.

call npm.cmd start

echo.
echo Frontend stopped. Press any key to close.
pause >nul
