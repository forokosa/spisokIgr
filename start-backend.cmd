@echo off
title GameTrack API
cd /d "%~dp0server"

echo ==========================================
echo   Backend: http://localhost:5080
echo   Swagger: http://localhost:5080/swagger
echo ==========================================
echo.

dotnet run --project src/GameTrack.Api

echo.
echo Backend stopped. Press any key to close.
pause >nul
