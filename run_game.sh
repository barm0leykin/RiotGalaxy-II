#!/bin/bash
# Сборка и запуск десктопной версии RiotGalaxy-II (MonoGame DesktopGL).
set -e

cd "$(dirname "$0")"

echo "Сборка RiotGalaxy-II..."
dotnet build RiotGalaxy.sln

echo "Запуск..."
dotnet run --project RiotGalaxy.DesktopGL/RiotGalaxy.DesktopGL.csproj

echo "Игра завершена."
