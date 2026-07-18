#!/bin/bash
# Сборка десктопной версии RiotGalaxy-II В DOCKER + запуск на хосте.
#
# Почему Docker: HLSL-шейдер Bloom.fx (EffectsDesktop.mgcb) компилируется mgfxc через wine.
# Образ docker/Dockerfile.desktop несёт всё нужное (.NET 6 SDK, шрифты, wine-префикс), поэтому
# сборка не зависит от системы хоста. Запуск — уже собранного бинарника на хосте напрямую
# (dotnet <dll>, без MSBuild → wine на хосте не нужен; нужен лишь рантайм .NET 6).
#
# Использование:  ./build_game.sh [Debug|Release]   (по умолчанию Debug)
set -e

cd "$(dirname "$0")"

IMAGE="riotgalaxy-desktop-build"
CONFIG="${1:-Debug}"
LOG_FILE="riot_desktop.log"
HOME_CACHE="docker/.home"   # HOME контейнера (кэш NuGet/dotnet + wine-префикс), gitignored

mkdir -p "$HOME_CACHE"

echo ">> Сборочный образ ${IMAGE} (docker build — быстро при кэше слоёв)"
docker build -f docker/Dockerfile.desktop -t "$IMAGE" .

echo ">> Сборка DesktopGL в контейнере (${CONFIG})"
# --user: артефакты bin/obj принадлежат тебе. wine не работает с «чужим» префиксом, поэтому
# эталон из образа (/opt/.winemonogame) копируем в свой HOME (копия — наша, владелец совпал).
# Собираем только проект DesktopGL: весь .sln тут не соберётся (Android-проект без workload).
docker run --rm \
    --user "$(id -u):$(id -g)" \
    -e HOME=/home/build \
    -e XDG_RUNTIME_DIR=/tmp/xdg \
    -e MGFXC_WINE_PATH=/home/build/.winemonogame \
    -v "$PWD/${HOME_CACHE}:/home/build" \
    -v "$PWD:/src" \
    -w /src \
    "$IMAGE" \
    bash -c "mkdir -p /tmp/xdg && \
        { [ -d /home/build/.winemonogame ] || cp -a /opt/.winemonogame /home/build/.winemonogame; } && \
        dotnet build RiotGalaxy.DesktopGL/RiotGalaxy.DesktopGL.csproj -c ${CONFIG}"

DLL="RiotGalaxy.DesktopGL/bin/${CONFIG}/net6.0/RiotGalaxy.DesktopGL.dll"
if [ ! -f "$DLL" ]; then
    echo "== Сборка не дала бинарник: $DLL =="
    exit 1
fi

echo ">> Запуск на хосте: $DLL"
if dotnet "$DLL" 2>&1 | tee "$LOG_FILE"; then
    echo "== Готово. Полный лог запуска — в $LOG_FILE =="
else
    echo "== Запуск не удался. Лог — $LOG_FILE =="
    exit 1
fi

echo "Игра завершена."
