@echo off
chcp 65001 > nul
title obhod сборка

if not exist "Resources\obhod_core.exe"   ( echo Ошибка: нет Resources\obhod_core.exe & pause & exit /b 1 )
if not exist "Resources\WinDivert.dll" ( echo Ошибка: нет Resources\WinDivert.dll & pause & exit /b 1 )

set DOTNET_EXE="C:\Program Files\dotnet\dotnet.exe"
%DOTNET_EXE% --version > nul 2>&1
if %ERRORLEVEL% NEQ 0 ( echo Ошибка: .NET 8 SDK не найден в C:\Program Files\dotnet & pause & exit /b 1 )

if exist "dist" rd /s /q "dist"
mkdir "dist"

echo Компилирую...
%DOTNET_EXE% publish obhod.csproj -c Release -r win-x64 --self-contained true -p:PublishSingleFile=true -p:EnableCompressionInSingleFile=true -p:IncludeNativeLibrariesForSelfExtract=true -o "dist"

if %ERRORLEVEL% NEQ 0 ( echo Сборка упала & pause & exit /b 1 )

echo.
echo Готово. Ищи obhod.exe в папке dist
pause
