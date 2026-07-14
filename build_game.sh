#!/bin/bash
# Сборка и запуск десктопной версии RiotGalaxy-II (MonoGame DesktopGL).
set -e

cd "$(dirname "$0")"

LOG_FILE="riot_android.log"

echo "Сборка RiotGalaxy-II..."
dotnet build RiotGalaxy.sln

echo "Запуск..."


if dotnet run --project RiotGalaxy.DesktopGL/RiotGalaxy.DesktopGL.csproj 2>&1 | tee "$LOG_FILE"; then
    echo "== Готово. Полный лог запуска — в $LOG_FILE =="
else
    echo "== Запуск не удался. Лог — $LOG_FILE =="
    exit 1
fi

echo "Игра завершена."
