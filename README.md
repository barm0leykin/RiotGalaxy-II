# RiotGalaxy II

2D космический шутер (в духе Galaga/Space Invaders) на **MonoGame**. Кросс-платформенный:
десктоп (Windows/Linux/macOS через OpenGL) и Android.

Проект вырос из миграции оригинальной игры с CocosSharp на MonoGame (миграция завершена —
её лог сохранён в [MIGRATION.md](MIGRATION.md)). Дальнейшая разработка ведётся здесь
самостоятельно, без оглядки на старый движок.

## Возможности

- Игрок: движение, 3 вида оружия (пушка/пулемёт/лазер) с уровнями прокачки, щит/неуязвимость.
- Враги: типы (scout/blue/green/red) + босс; **машина состояний ИИ** (влёт → роение → атака).
- **Улей (Galaga)**: формация вверху, из которой юниты по очереди пикируют в атаку и
  возвращаются; тактики пике — `random`/`snake`/`ram`/`ellipse`.
- Уровни-кампания с нарастанием (формации, маршруты-«змейки», синхронные волны, босс),
  описываются в YAML.
- Бонусы (HP, апгрейд оружия, нюк, звёзды-очки с магнитом), меню/настройки, всплывающие
  сообщения.
- Почти всё конфигурируется из YAML без перекомпиляции (оружие, враги, бонусы, опции, уровни).

## Структура

```text
RiotGalaxy.Core/        # вся игровая логика (платформонезависимая .dll)
  GameObjects/          #   корабль, враги, снаряды, бонусы, World/Hive/Route
  Components/           #   движение/стрельба/столкновения (паттерн «Стратегия»)
  AI/                   #   машина состояний врагов (EnemyAI/AIState)
  Weapons/ Managers/ Screens/ Commands/ Utils/ Interface/
RiotGalaxy.Content/     # ассеты (спрайты/звуки/шрифт) + YAML-конфиги и уровни
RiotGalaxy.DesktopGL/   # десктопное приложение (точка входа Program.cs)
RiotGalaxy.Android/     # Android-обёртка (net9.0-android, вне .sln)
docker/                 # сборочное окружение Android (по PRD — только в Docker)
```

Подробно об архитектуре — в [ARCHITECTURE.md](ARCHITECTURE.md). Список открытых задач —
в [TODO.md](TODO.md).

## Сборка и запуск

### Десктоп (для разработки)

```bash
dotnet tool restore            # один раз после клона — ставит dotnet-mgcb (компилятор ассетов)
./run_game.sh                  # собрать и запустить
# или вручную:
dotnet run --project RiotGalaxy.DesktopGL/RiotGalaxy.DesktopGL.csproj
```

Требуется **.NET SDK** (проект на `net6.0`, `RollForward=Major` — собирается и на новых SDK).

Управление: ←/→ или A/D — движение, **Space** — огонь, **1/2/3** — оружие, **Esc/P** — пауза.

### Android (APK)

Сборка только в Docker (хост не засоряется):

```bash
./docker/build-image.sh        # один раз: образ riotgalaxy-android-build (.NET9+JDK17+AndroidSDK)
./docker/build-apk.sh Debug    # → RiotGalaxy.Android/bin/Debug/net9.0-android/*-Signed.apk
./docker/run-on-device.sh      # установить и запустить на USB-устройстве (adb из контейнера)
```

На Android: тач-управление, автоогонь, letterbox-масштаб, кнопка «Назад». Подробности —
[ARCHITECTURE.md](ARCHITECTURE.md) (§18.5).

## Отладка в VSCode

`.vscode/` содержит конфигурации: **F5** — запуск DesktopGL с отладчиком; задачи сборки
desktop и Android (Ctrl+Shift+B / палитра задач). Логи — `Log.Debug/Error` → консоль и файл
`riot.log` рядом с бинарником (на Android — в logcat, тег `DOTNET`).
