#!/bin/bash
# Сборка, установка и запуск RiotGalaxy-II на подключённом по USB телефоне (Android).
# Аналог run_game.sh, но для телефона. Всё идёт через Docker (см. docker/*.sh) — хост не засоряем.
#
# Что делает:
#   1) собирает APK (docker/build-apk.sh)
#   2) ставит и запускает на устройстве, снимает logcat (docker/run-on-device.sh)
#   3) дублирует весь вывод в riot_android.log в корне — чтобы лог можно было проанализировать
#      (там видны наши строки под тегом DOTNET: [DBG]/[ERR], а также ошибки/исключения).
#
# Использование:
#   ./run_phone.sh [Debug|Release] [секунды_лога]
# По умолчанию: Debug, 12 секунд logcat после старта.
#
# Требования: телефон подключён по USB и авторизован для отладки (см. docker/run-on-device.sh);
# образ собран один раз через docker/build-image.sh.
set -e
set -o pipefail   # код возврата пайпа = код run-on-device.sh, а не tee

cd "$(dirname "$0")"

CONFIG="${1:-Debug}"
LOG_SECONDS="${2:-12}"
LOG_FILE="riot_android.log"

echo "== Сборка APK ($CONFIG) =="
./docker/build-apk.sh "$CONFIG"

echo "== Установка, запуск и захват лога ($LOG_SECONDS c) =="
# tee: и в консоль, и в файл для последующего анализа. if — чтобы поймать сбой деплоя.
if ./docker/run-on-device.sh "$CONFIG" "$LOG_SECONDS" 2>&1 | tee "$LOG_FILE"; then
    echo "== Готово. Полный лог запуска — в $LOG_FILE =="
else
    echo "== Деплой не удался (телефон подключён по USB и авторизован для отладки?). Лог — $LOG_FILE =="
    exit 1
fi
