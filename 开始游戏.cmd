@echo off
setlocal
set "GAME_DIR=%~dp0Builds\Windows"
set "GAME_EXE=%GAME_DIR%\2087 Dungeons.exe"
if not exist "%GAME_EXE%" (
    echo 2087 Dungeons.exe was not found in Builds\Windows.
    pause
    exit /b 1
)
start "" /D "%GAME_DIR%" "%GAME_EXE%"
