# RiotGalaxy на MonoGame — как всё устроено

Гайд для тех, кто хочет разобраться в проекте. Объясняет структуру проекта, точку входа,
главный цикл, загрузку ресурсов и ключевые архитектурные решения. Привязан к этому
конкретному проекту, а не абстрактный.

> Документация MonoGame: <https://docs.monogame.net/> · исходники: <https://github.com/MonoGame/MonoGame>

---

## 1. Что такое MonoGame (в двух словах)

MonoGame — это open-source реализация фреймворка **XNA** от Microsoft. Он даёт
низкоуровневые кирпичики: окно, игровой цикл, отрисовку спрайтов, ввод, звук, загрузку
ресурсов. Это **не движок с редактором** (как Unity/Godot) — здесь нет визуального
редактора сцен, всю логику и сцены пишешь кодом на C#.

Базовые понятия, которые встретятся везде:

| Понятие | Что это |
|---|---|
| `Game` | Базовый класс приложения. Держит окно и главный цикл. У нас наследник — `Game1`. |
| `GraphicsDeviceManager` | Управляет видеоустройством, разрешением, полноэкранным режимом. |
| `SpriteBatch` | «Пакетная» отрисовка 2D-спрайтов. Всё рисование идёт через него. |
| `Texture2D` | Картинка в памяти видеокарты (спрайт, фон). |
| `SoundEffect` | Звуковой эффект. |
| `GameTime` | Время: сколько прошло с прошлого кадра и всего. Нужно для плавности. |
| `ContentManager` (`Content`) | Загрузчик ресурсов из скомпилированных `.xnb`. |

---

## 2. Структура решения (`RiotGalaxy.sln`)

Решение состоит из нескольких проектов. Это типичный для MonoGame расклад:
«общая логика отдельно, платформа отдельно».

```text
RiotGalaxy-II/                      # корень репозитория
├── RiotGalaxy.sln                  # файл решения (Core + Content + DesktopGL)
├── RiotGalaxy.Core/                # 📦 БИБЛИОТЕКА: вся игровая логика (платформонезависимая)
├── RiotGalaxy.Content/             # 🎨 РЕСУРСЫ: спрайты, звуки, шрифты + сборка их в .xnb
├── RiotGalaxy.DesktopGL/           # 🖥️ ПРИЛОЖЕНИЕ для ПК (Windows/Linux/macOS) — точка входа
├── RiotGalaxy.Android/             # 📱 приложение под Android (net9.0-android, вне .sln)
├── docker/                         # сборочное окружение Android (Docker)
├── tools/                          # вспомогательные скрипты (напр. split_atlas.py)
├── README.md · TODO.md · MIGRATION.md   # обзор · открытые задачи · лог миграции
└── ARCHITECTURE.md                 # этот файл
```

Зачем такое разделение:

- **`RiotGalaxy.Core`** — это `.dll`-библиотека (в [csproj](RiotGalaxy.Core/RiotGalaxy.Core.csproj)
  нет `OutputType`, значит по умолчанию library). Здесь *вся* игра: классы кораблей,
  врагов, менеджеры, компоненты. Не знает, на какой платформе запускается.
- **`RiotGalaxy.DesktopGL`** — это `.exe` (в [csproj](RiotGalaxy.DesktopGL/RiotGalaxy.DesktopGL.csproj)
  стоит `OutputType=WinExe`). Он ссылается на `Core` и на скомпилированные ресурсы.
  Именно его мы запускаем. «GL» = рендеринг через OpenGL (кроссплатформенно).
- **`RiotGalaxy.Content`** — не код, а ресурсы + рецепт их сборки ([.mgcb](RiotGalaxy.Content/RiotGalaxy.Content.mgcb)).
- **Android/iOS** — отдельные «обёртки» под мобильные платформы (пока не в фокусе).

Идея: чтобы добавить новую платформу, пишешь только маленький проект-обёртку,
а `Core` переиспользуется как есть.

---

## 3. Структура `RiotGalaxy.Core`

```text
RiotGalaxy.Core/
├── Game1.cs                 # главный класс игры (наследник Game) — делегирует в GameManager
├── Managers/                # глобальные системы (синглтоны)
│   ├── GameManager.cs       #   состояния, список объектов, игровой цикл, отрисовка
│   ├── CollisionSystem.cs   #   разрешение столкновений (вынесено из GameManager)
│   ├── LevelDirector.cs     #   ОДИН бой: World/Hive, спавн врагов по таймлайну, счётчики
│   ├── MissionDirector.cs   #   кампания: миссия = цепочка брифингов/боёв/босса/магазина
│   ├── InputManager.cs      #   ввод (клавиатура/мышь), GUI-кнопки
│   └── AudioManager.cs      #   загрузка/проигрывание звуков
├── GameObjects/             # всё, что живёт на экране
│   ├── GameObject.cs        #   базовый класс (позиция/размер/текстура/Update/Draw)
│   ├── PlayerShip.cs        #   корабль игрока (HP, щит, оружие, очки)
│   ├── Enemy.cs             #   ОДИН класс врага, конфигурируется из enemies.yaml по типу
│   ├── Shell.cs             #   ОДИН класс снаряда (спрайт/пробивание — параметры)
│   ├── Bonus.cs             #   простые бонусы (тип+эффект) + BonusStar (уникальный)
│   └── World.cs (+ Cell), Hive.cs, Route.cs   # сетка мира, формации, маршруты
├── Weapons/                 # система оружия (паттерн «Стратегия»)
│   ├── Weapon.cs            #   база + WeaponCannon/Minigun/Laser/NoWeapon
│   └── WeaponOptions.cs, WeaponConfig.cs (грузит weapons.yaml)
├── Components/              # компоненты поведения (паттерн «Стратегия»)
│   ├── MovementComponent.cs, EnemyBounceMovement.cs
│   ├── FormationMovement.cs, RouteMovement.cs
│   └── ShootingComponent.cs, CollisionComponent.cs
├── Screens/                 # ВСЕ состояния — экраны (см. §14)
│   ├── Screen.cs, ScreenSystem.cs
│   ├── SplashScreen / MainMenuScreen / SettingsScreen / NextLevelScreen
│   ├── GameplayScreen / PausedScreen / GameOverScreen / VictoryScreen
│   └── DialogueScreen (диалоги/брифинги; контент — Content/Dialogues/*.yaml)
├── Commands/                # паттерн «Команда» (смена оружия, kill all, next level…)
├── Interface/               # MyButton + кнопки (тестовая панель), HudRenderer.cs (боевой HUD)
├── Effects/                 # ParticleSystem.cs (взрывы/искры), StarField.cs (параллакс), см. §6
├── Utils/                   # Level.cs, GameOptions/GameSettings/EnemyConfig/BonusConfig/EffectsConfig.cs, Yaml.cs, Textures.cs, Log.cs
└── AI/                      # машина состояний врагов: EnemyAI.cs, AIState.cs (см. §12)
```

Эта раскладка повторяет архитектуру оригинала на CocosSharp (см. [prd.md](../prd.md)):
сохранены паттерны **Strategy** (компоненты/оружие), **Command** (команды), **State**
(состояния игры).

> Папка `AI/` — машина состояний врагов (`EnemyAI`/`AIState`, порт `BehAI` из CocoSharp), см. §12.
> Открытые задачи — в [TODO.md](TODO.md); лог завершённой миграции — в [MIGRATION.md](MIGRATION.md).

---

## 4. Точка входа — откуда всё стартует

Запуск идёт по цепочке:

```text
run_game.sh
  └─ dotnet run --project RiotGalaxy.DesktopGL
       └─ Program.Main()                          ← RiotGalaxy.DesktopGL/Program.cs
            └─ Game1.Instance.Run()               ← запускает игровой цикл
```

[Program.cs](RiotGalaxy.DesktopGL/Program.cs) — крошечный файл, вся его работа:

```csharp
static void Main()
{
    using (var game = RiotGalaxy.Core.Game1.Instance)
        game.Run();   // ← здесь начинается бесконечный игровой цикл
}
```

`game.Run()` — это метод базового класса `Game`. Он создаёт окно и запускает цикл,
который сам вызывает `Update()` и `Draw()` ~60 раз в секунду, пока окно не закроют.

> ⚠️ В этом проекте `Game1` и менеджеры сделаны **синглтонами** (`Game1.Instance`,
> `GameManager.Instance`). Это не «канонический» стиль MonoGame (обычно объект игры
> создают через `new`), но здесь так — наследие переноса с CocosSharp.

---

## 5. Жизненный цикл `Game` и главный цикл

Базовый класс `Game` вызывает методы в строгом порядке. В нашем
[Game1.cs](RiotGalaxy.Core/Game1.cs) переопределены ключевые из них:

```text
1. Конструктор Game1()        → создаём GraphicsDeviceManager, задаём Content.RootDirectory,
                                 создаём GameManager и вызываем его Initialize()
2. Initialize()               → разовая инициализация (после создания графики)
3. LoadContent()              → ОДИН раз: грузим все ресурсы (спрайты, звуки, фон)
   ┌──────────────────────────────────────────────────────────┐
   │ ГЛАВНЫЙ ЦИКЛ (повторяется каждый кадр, ~60 раз/сек):       │
   │ 4. Update(gameTime)  → пересчёт логики (движение, ввод…)   │
   │ 5. Draw(gameTime)    → отрисовка кадра                     │
   └──────────────────────────────────────────────────────────┘
6. UnloadContent() / выход
```

Важная идея — **разделение Update и Draw**:

- `Update(GameTime)` — меняет *состояние мира*: позиции, здоровье, таймеры. Здесь
  читается ввод. **Никакого рисования.**
- `Draw(GameTime)` — только *рисует* текущее состояние. **Ничего не меняет** в логике.

`GameTime` даёт `gameTime.ElapsedGameTime.TotalSeconds` — сколько секунд прошло с
прошлого кадра. Скорости считают как `позиция += скорость * deltaTime`, чтобы движение
было одинаковым при любом FPS.

### Как это выглядит в нашем `Game1`

`Game1` сам по себе тонкий — он **делегирует** всю работу в `GameManager`:

```csharp
protected override void LoadContent() => _gameManager.LoadContent();
protected override void Update(GameTime gameTime)  { …ввод…; _gameManager.Update(gameTime); }
protected override void Draw(GameTime gameTime)    { _gameManager.Draw(gameTime); }
```

Плюс `Game1.Update` обрабатывает клавиши **игровых** состояний (Playing/Paused/GameOver/
Victory): Esc/P — пауза и снятие, Space — рестарт на экранах конца игры. Ввод **меню**
(Splash/MainMenu/Settings/NextLevel) обрабатывают сами экраны (см. §14). «Детект нажатия»
— через `_previousKeyboardState` (реагируем на *момент* нажатия, а не удержание).

> 💡 При старте `GameManager.LoadContent` вызывает `ChangeGameState(Splash)` — игра
> начинается с заставки → меню → игра (прежний авто-переход сразу в Playing убран).

---

## 6. `GameManager` — сердце игры

[GameManager.cs](RiotGalaxy.Core/Managers/GameManager.cs) — центральный диспетчер
(синглтон `GameManager.Instance`). Отвечает за:

- **Состояния игры** (паттерн State) —
  `enum GameState { Splash, Profile, MainMenu, Settings, Playing, Paused, GameOver, Victory, NextLevel, Dialogue, Shop, DevMenu }`.
  `DevMenu` ([DevMenuScreen](RiotGalaxy.Core/Screens/DevMenuScreen.cs)) — выбор миссии для тестов; пункт
  входа в главном меню под `#if DEBUG`, в релизе недоступен.
  После заставки — экран **выбора профиля** (`Profile` → [ProfileScreen](RiotGalaxy.Core/Screens/ProfileScreen.cs)).
  **Все** состояния — экраны: `Update`/`Draw` целиком делегируются в `ScreenSystem` (см. §14),
  без `switch`. Переходы — через `ChangeGameState`, который заводит нужный `Screen` и решает,
  что чистить/инициализировать (новая партия, пауза = сохранить, между уровнями = сохранить игрока).
  Боевую петлю несут публичные `UpdateGameplay`/`DrawGameplay` (их зовёт `GameplayScreen`).
- **Список игровых объектов** — `List<GameObject> GameObjects`. В `Playing` каждый кадр:
  спавн врагов по таймлайну уровня (`LevelDirector`), `Update` каждого объекта, разрешение
  столкновений (`CollisionSystem`, O(n²/2)), удаление «мёртвых» (`ProcessObjectRemoval`).
- **Уровни/прогрессия** — два уровня оркестрации:
  [LevelDirector.cs](RiotGalaxy.Core/Managers/LevelDirector.cs) гоняет ОДИН бой (World/Hive, спавн,
  счётчики), а [MissionDirector.cs](RiotGalaxy.Core/Managers/MissionDirector.cs) ведёт кампанию:
  **миссия = последовательность шагов** (брифинг → бой → … → босс → брифинг → магазин).
  Поток шагов в `GameManager`: `StartCampaign` → `RunNextStep` (диспетчер) → `EnterBattle` /
  брифинг (`PlayDialogueThen`) / магазин (`OpenShopThen`); по зачистке боя — `OnBattleCleared`
  → следующий шаг; конец кампании — `FinishCampaign` → Victory. См. §15.
- **Столкновения** — вынесено в [CollisionSystem.cs](RiotGalaxy.Core/Managers/CollisionSystem.cs).
- **Отрисовку** — держит `SpriteBatch`, рисует фон/параллакс; объекты, HUD и кнопки рисует
  `GameplayScreen` → `DrawGameplay` (HUD — [Interface/HudRenderer.cs](RiotGalaxy.Core/Interface/HudRenderer.cs)).
- **Экран** — `ScreenWidth/Height` (из `options.yaml`, по умолчанию 1280×768).

### Как рисуется кадр (`GameManager.Draw`)

```csharp
GraphicsDevice.Clear(Color.Black);     // 1. очистить экран
_spriteBatch.Begin(..., _renderMatrix);// 2. открыть «пакет» (с letterbox+screenshake)
   if (_background != null) …           // 3. фон во всю ширину
   _starField.Draw(...);                // 4. параллакс-звёзды
   Screens.Draw(_spriteBatch);          // 5. активный экран (для Playing → DrawGameplay)
_spriteBatch.End();                     // 6. закрыть пакет → всё уходит на видеокарту
```

**Правило:** любое рисование спрайтов обязано быть между `spriteBatch.Begin()` и `End()`.

### Эффекты: частицы и screenshake

«Сочность» боя обеспечивают две лёгкие системы внутри `GameManager`:

- **Частицы** — [Effects/ParticleSystem.cs](RiotGalaxy.Core/Effects/ParticleSystem.cs).
  Пул фиксированного размера (1024), рисуется простой текстурой `SimpleTexture` (без ассетов).
  Обновляется в `UpdateGameplay`, рисуется в `DrawGameplay` поверх объектов, под HUD.
  Методы: `Explosion(...)` (разлёт при гибели врага, цвет — по `EnemyType`), `HitSpark(...)`
  (искра попадания/урона), `Clear()` (смена уровня/состояния). Дульная вспышка — из
  `Weapon.FireOnce` (`MuzzleFlash`), трассер-след — из `Shell.Update` (`ShellTrail`, снаряды игрока).
- **Screenshake** — `GameManager.Shake(magnitude, duration)`. Смещение `_shakeOffset` (в
  виртуальных пикселях) подмешивается в `_renderMatrix` letterbox'а (× scale), затухает
  линейно в `UpdateScreenShake`. Слабая тряска не перебивает более сильную активную.
  Триггеры: гибель врага/босса, «нюк» (`KillAllEnemies`), урон игроку (`OnPlayerHealthChanged`).
- **Параллакс-фон** — [Effects/StarField.cs](RiotGalaxy.Core/Effects/StarField.cs). Слои
  процедурных звёзд (дальний/средний/ближний) плывут вниз с разной скоростью и яркостью,
  при уходе за край возвращаются сверху. Анимируется в `Update` (во всех состояниях),
  рисуется в `Draw` поверх задника, под сценой/UI. Тоже без ассетов (`SimpleTexture`).

- **Всплывающие числа** — [Effects/FloatingText.cs](RiotGalaxy.Core/Effects/FloatingText.cs)
  (статический, в координатах мира): урон над врагом (`CollisionSystem`), очки над звездой
  (`BonusStar.Apply`). Плывут вверх и затухают. В отличие от `MessageLog` (фикс. позиция внизу).
  Обновляется в `UpdateGameplay`, рисуется в `DrawGameplay` поверх частиц, под HUD.

Все числовые параметры этих систем (частицы, тряска, слои звёзд, дульная вспышка/трассер,
рост снаряда по уровню `shellLevelScaleStep`, всплывающие числа `floatingText`) задаются в
[Content/Config/effects.yaml](RiotGalaxy.Content/Config/effects.yaml) и грузятся в
`Utils.EffectsConfig` (дефолты в коде, фолбэк без файла). См. §16. Оттенок спрайта — `GameObject.Tint`.

---

## 7. Игровые объекты и компоненты

[GameObject.cs](RiotGalaxy.Core/GameObjects/GameObject.cs) — базовый класс всего, что
есть на экране. Ключевое:

- поля: `Position`, `Size`, `Scale`, `Rotation`, `Opacity`, `Texture`;
- `virtual void Update(GameTime)` — обновляет свои компоненты;
- `virtual void Draw(GameTime, SpriteBatch)` — рисует `Texture` (с центром в середине
  спрайта). Если `Texture == null` — рисует цветную заглушку.

[PlayerShip.cs](RiotGalaxy.Core/GameObjects/PlayerShip.cs) — наследник `GameObject`:
здоровье, неуязвимость (щит), оружие, и загрузка своего спрайта `Images/ship`.

**Компоненты** (папка `Components/`) — это паттерн «Стратегия»: вместо того чтобы
зашивать поведение в сам объект, оно вынесено в подключаемые компоненты:

```csharp
Movement = new PlayerMovementComponent(this, Speed);  // как двигаться
Shooting = new PlayerShootingComponent(this);         // как стрелять
Collision = new PlayerCollisionComponent(this);       // как сталкиваться
```

`GameObject.Update` просто вызывает `Movement?.Update()`, `Shooting?.Update()` и т.д.
Так одно и то же поведение можно переиспользовать у разных объектов.

---

## 8. Ресурсы: Content Pipeline (САМОЕ ВАЖНОЕ для новичка)

Это место, которое чаще всего путает новичков в MonoGame.

### Зачем нужен «пайплайн»

MonoGame **не грузит PNG/WAV напрямую** в релизе. Сначала специальная утилита
компилирует исходные файлы (`.png`, `.wav`, `.spritefont`) в бинарный формат **`.xnb`**,
оптимизированный под платформу. В игре ты грузишь уже `.xnb`.

```text
ship.png  ──[ MGCB / dotnet-mgcb ]──►  ship.xnb  ──[ Content.Load<Texture2D> ]──►  Texture2D в игре
```

### Действующие лица

1. **`.mgcb`-файл** — [RiotGalaxy.Content.mgcb](RiotGalaxy.Content/RiotGalaxy.Content.mgcb).
   Это текстовый «рецепт»: список всех ресурсов и как каждый собирать. Пример блока:

   ```text
   #begin Images/ship.png
   /importer:TextureImporter
   /processor:TextureProcessor
   /processorParam:PremultiplyAlpha=True
   /build:Images/ship.png
   ```

2. **Утилита `dotnet-mgcb`** — собственно компилятор ресурсов. Ставится как локальный
   инструмент (см. [.config/dotnet-tools.json](.config/dotnet-tools.json)). Если её нет —
   сборка контента падает. Восстановить: `dotnet tool restore`.

3. **`MonoGameContentReference`** в [DesktopGL.csproj](RiotGalaxy.DesktopGL/RiotGalaxy.DesktopGL.csproj) —
   связывает `.mgcb` с приложением. При сборке `.xnb` автоматически копируются в
   выходную папку игры (в подпапку `Content/`).

4. **`Content.RootDirectory = "Content"`** в `Game1` — говорит, что грузить ресурсы надо
   из папки `Content/` рядом с `.exe`.

### Где лежат ресурсы

```text
RiotGalaxy.Content/
├── RiotGalaxy.Content.mgcb     # рецепт сборки (.xnb): Images, Backgrounds, Sounds, шрифт
├── Images/                     # спрайты (нарезаны из старого атласа images.png)
│   ├── ship.png, bullet.png, shield.png …
│   └── Enemies/                # 60 спрайтов врагов (нарезаны из art/units*.png):
│       │                       #   thin_<color>_1..6 и fat_<color>_1..6 (6 силуэтов на цвет)
│       │                       #   thin: green/blue/pink/yellow/red; fat: green/blue/purple/orange/red
│       └── …                   # фон убран в прозрачность; на тип врага вешаются через enemies.yaml `sprite:`
├── Backgrounds/                # фоны: background_blue (1280×768), background, SCConvoy_0
├── Sounds/                     # звуки: fire1.wav, explode1.wav
├── TestFont.spritefont         # шрифт DejaVu Sans Mono: ASCII + Latin-1 + кириллица + тире/стрелки
├── Config/                     # YAML-конфиги (НЕ через MGCB): weapons.yaml, options.yaml
├── Levels/                     # бои: level1..5.yaml + m1_b*/m1_boss.yaml (НЕ через MGCB)
├── Dialogues/                  # брифинги/диалоги (speaker/text/portrait)
└── Missions/                   # кампания: campaign.yaml + m<id>.yaml (шаги миссии)
```

> Важная тонкость: пути в `.mgcb` отсчитываются от папки самого `.mgcb`, поэтому
> бинарные ресурсы (`Images/Sounds/Backgrounds/`шрифт) лежат **прямо** в `RiotGalaxy.Content/`,
> а имя ассета для загрузки — путь без расширения (напр. `"Images/ship"`).
>
> **Два пути доставки контента:** бинарные ассеты идут через **MGCB → `.xnb`** и грузятся
> `Content.Load<…>`. А **текстовые** конфиги/уровни (`Config/*.yaml`, `Levels/*.yaml`)
> компилировать незачем — они копируются в выход «как есть» через `<None ... CopyToOutputDirectory>`
> в [DesktopGL.csproj](RiotGalaxy.DesktopGL/RiotGalaxy.DesktopGL.csproj) и читаются из
> `AppContext.BaseDirectory/Content/...` (см. §16).

### Как грузят ресурсы в коде

```csharp
// Texture2D (спрайт/фон):
Texture = content.Load<Texture2D>("Images/ship");
_background = content.Load<Texture2D>("Backgrounds/background_blue");

// SoundEffect (звук):
_effects["fire1"] = content.Load<SoundEffect>("Sounds/fire1");

// SpriteFont (шрифт):
var font = content.Load<SpriteFont>("TestFont");
```

В проекте загрузка собрана в одном месте — `GameManager.LoadContent()` (фон + звуки
через `AudioManager`), а корабль грузит себя сам в `PlayerShip.LoadContent()`.

#### YAML-конфиги/уровни/маршруты — через `TitleContainer` (кросс-платформенно)

Бинарные ассеты (`.xnb`) грузит `ContentManager.Load`. А текстовый контент
(`Content/Config|Levels|Routes/*.yaml`) читается **не** через `File.ReadAllText`, а через
`TitleContainer.OpenStream("Content/...")` — это работает и на DesktopGL (файлы рядом с
приложением), и на Android (ассеты внутри APK). Помощники — в
[Yaml.cs](RiotGalaxy.Core/Utils/Yaml.cs):

```csharp
var weapons = Yaml.LoadAsset<WeaponsYaml>(Yaml.ConfigAsset("weapons.yaml")); // Content/Config/weapons.yaml
bool hasLevel = Yaml.AssetExists("Content/Levels/level1.yaml");
```

Так грузятся `WeaponConfig/EnemyConfig/BonusConfig/GameOptions` (Config), `Level`
(Levels + `CountLevels`), `Route` (Routes). Пути — относительные, со слешами `/`.

Исключение — `settings.yaml` (его игра **пишет**): идёт через `File.Write/ReadAllText` по
абсолютному пути (`Yaml.LoadFile`), т.к. бандл приложения только для чтения. Writable-путь
под Android настроим на этапе Android-проекта.

### 🔧 Как добавить НОВЫЙ ресурс (пошагово)

1. Положить файл в нужную папку, напр. `RiotGalaxy.Content/Images/boss.png`.
2. Добавить блок в `.mgcb` (можно руками по образцу выше; GUI-редактор MGCB не нужен,
   тем более он не работает в Linux-сборке без дисплея).
3. Пересобрать — `.xnb` соберётся и скопируется автоматически.
4. В коде: `var tex = Content.Load<Texture2D>("Images/boss");`

> Спрайты массово нарезались из старого атласа скриптом
> [tools/split_atlas.py](tools/split_atlas.py) — он по описанию `images.plist` режет
> `images.png` на отдельные PNG.

---

## 9. Ввод

[InputManager.cs](RiotGalaxy.Core/Managers/InputManager.cs) — адаптация системы ввода из
оригинала. Базовый принцип MonoGame: **состояние ввода опрашивается каждый кадр**
(не события):

```csharp
var kb = Keyboard.GetState();
if (kb.IsKeyDown(Keys.Space)) { … }
var mouse = Mouse.GetState();
var pad = GamePad.GetState(PlayerIndex.One);
```

Чтобы поймать *момент нажатия* (а не удержание), сравнивают с прошлым кадром — см.
`_previousKeyboardState` в `Game1`.

---

## 10. Аудио

[AudioManager.cs](RiotGalaxy.Core/Managers/AudioManager.cs) (синглтон) загружает звуки в
словарь и играет их по имени:

```csharp
AudioManager.Instance.PlayEffect("fire1");   // громкость 0.1, как в оригинале
```

Загрузка — в `GameManager.LoadContent`. `fire1` играет на выстреле игрока (оружие),
`explode1` — при гибели врага. Громкость `EffectsVolume` берётся из `settings.yaml`
(меню «Настройки», см. §14/§16). Музыки (BGM) в проекте нет.

---

## 11. Оружие и снаряды

**Data-driven реестр** ([Weapons/WeaponConfig.cs](RiotGalaxy.Core/Weapons/WeaponConfig.cs) ← `weapons.yaml`).
Подклассов оружия **нет** — единый настраиваемый класс [Weapon.cs](RiotGalaxy.Core/Weapons/Weapon.cs)
читает `WeaponDef`: спрайт/`piercing` снаряда, `jitterDeg` (разброс пулемёта), веер (`fanCount`/`fanPerLevel`/
`fanStepDeg`), параметры по уровням (`WeaponOptions`: burst/burstInterval/reloadSpeed/damage/shellSpeed),
цены (`unlockCost`, `baseCost`/`costGrowth`), клавиша (`key`), иконка.

- **Получение/прокачка — через магазин (персистентно).** Старт — «Бластер» (слабый, очень
  скорострельный; `unlockCost: 0`). Остальные (пушка/пулемёт/лазер/разлёт) **открываются** покупкой,
  у каждого **свои уровни** (индивидуальная прокачка). Уровни хранятся в `SaveData.WeaponLevels`
  (id→уровень; 0 = не открыто). Глобальные апгрейды (урон/темп) и баффы умножают всё оружие.
- **`Weapon.SetWeapon(def, level)`** — экипировать; `Update/Fire` ведут очередь и перезарядку;
  `Aim(угол)` — направление. `FireOnce`: веер (если `fanCount>1`) или одиночный выстрел (+jitter).
- **`PlayerShip`**: `CurrentWeaponId`, `EquipWeapon(id)`/`ChangeWeapon(id)` (только открытое),
  переэкипировка в `ApplyUpgrades` (подхват купленного уровня). Переключение — клавиши из `WeaponDef.Key`
  (D1..D5) и тач-кнопки (`ButtonChWeapon`). У врагов — простой `Weapon` без `Def`.

Снаряды — единый класс [Shell.cs](RiotGalaxy.Core/GameObjects/Shell.cs): спрайт + `piercing`;
`Speed/Damage/Direction/PlayerSide` задаёт оружие при выстреле. Размер/оттенок растут с уровнем оружия.
Стрельба игрока — удержание **Space** (темп держит оружие).

## 12. Враги

**Один класс** [Enemy.cs](RiotGalaxy.Core/GameObjects/Enemy.cs) (`EnemyType { RND, SM_SCOUT, BLUE,
GREEN, RED, BOSS, UKRO, KAMIK, HEAVY, UKRO_BOSS }`; последние четыре — укропитеки Акта II:
грунт/камикадзе-таран/тяжёлый-Космерика/полевой командир). Прежних подклассов
(`EnemySmallBlue/Green/Red/Scout`, `EnemyBoss`) **нет** —
враг конфигурирует себя из `enemies.yaml` по типу (data-driven): спрайт, масштаб, режим стрельбы
(`shoot: none/down/aim`), ИИ (`ai: none/blue/red`), блуждание (`wander`), диапазон курса
(`dirMin/dirMax`), плюс hp/урон/скорость/интервал (с рандомом по диапазонам), см. §16.

- **Движение** — [EnemyBounceMovement](RiotGalaxy.Core/Components/EnemyBounceMovement.cs):
  отскок от боковых границ (поле −10%) + телепорт снизу-вверх. `SetDirection(угол)`.
  При `wander: true` (зелёный) курс/скорость периодически меняются.
- **Стрельба**: по таймеру `ShootInterval`, режим из конфига `shoot`: `down` (вниз),
  `aim` (прицельно в игрока), `none` (не стреляет). Флаг `ShootSafe` временно глушит
  стрельбу (им управляют состояния ИИ).
- Гибель: `TakeDamage`→`Die` (звук `explode1`), уменьшает `EnemiesRemaining`, роняет бонус,
  начисляет игроку валюту `Reward` (поле `reward` в enemies.yaml).

**ИИ — машина состояний** ([AI/](RiotGalaxy.Core/AI/), порт `BehAI`/`AIState` из CocoSharp).
У врага есть опциональный `Ai` ([EnemyAI](RiotGalaxy.Core/AI/EnemyAI.cs)) — контроллер с
состояниями ([AIState.cs](RiotGalaxy.Core/AI/AIState.cs)): **TakeOff** (влетает, не стреляет),
**Swarming** (медленно роится, стреляет), **Attack** (быстро, стреляет чаще). Контроллеры по типам:
`EnemyAIRed` (TakeOff→Swarming), `EnemyAIBlue` (TakeOff→Swarming↔Attack), `EnemyAIDumb` (ничего).
Состояния через API `Enemy` управляют движением/скоростью/темпом стрельбы
(`UseBounceMovement`, `SetMoveDirection`, `SetShootInterval`, `ShootSafe`). `Ai` отключается
при входе в формацию/маршрут (там движение задаёт YAML).

**Вылеты из улья (sortie, Galaga)** — поверх формации. В описании уровня `sortie: true`
(+ `sortieInterval`, `sortieCount`) включает у `Hive` координатор: раз в N секунд он
отправляет `count` осевших юнитов в пике-атаку ([SortieMovement](RiotGalaxy.Core/Components/SortieMovement.cs)).
Юнит пикирует вниз, уходит за нижнюю границу, появляется сверху и возвращается в свою ячейку
(снова `FormationMovement`). **Тактика пике** выбирается случайно из списка `tactics` типа врага
(`enemies.yaml`): `random` (вниз с отскоком), `snake` (змейка), `ram` (таран в точку игрока),
`ellipse` (петля). Скорость вылета — `attackSpeed` (траектории привязаны к ней, в т.ч. `ellipse`).
В строю улья враги **не стреляют** (как в Galaga, `ShootSafe=true`) — огонь только в вылете.
`Hive` ведёт учёт членов (`Register`/`NotifyReturned`).

**Мир и формации** ([World.cs](RiotGalaxy.Core/GameObjects/World.cs), [Hive.cs](RiotGalaxy.Core/GameObjects/Hive.cs)):
`World` — координатная сетка ячеек (16×10), центрирована на экране. `Hive` — формация-улей
(8×2) поверх ячеек: враги занимают ячейки (`TryTakeCell`) и синхронно барражируют
(весь улей качается, `Offset`). Враг входит в формацию через `Enemy.JoinFormation` —
движение сменяется на [FormationMovement](RiotGalaxy.Core/Components/FormationMovement.cs)
(летит к своей ячейке, затем держит строй). Формация задаётся в YAML-уровне флагом `formation: true`.

**Маршруты** ([Route.cs](RiotGalaxy.Core/GameObjects/Route.cs) + [RouteMovement](RiotGalaxy.Core/Components/RouteMovement.cs)):
враг летит по точкам из `Content/Routes/<name>.yaml` (координаты ячеек World). После последней
точки переключается на стратегию `after`: `bounce` (вниз с отскоком, по умолчанию),
`scatter` (случайный разлёт) или `formation` (занять ячейку улья и встать в строй).
В уровне: `{ enemy: blue, route: zmeyka1-left, after: formation }`.

**Боссы** — `EnemyType.BOSS` / `UKRO_BOSS` в `enemies.yaml` (отдельного класса нет): живучие
крупные враги (`scale: 2.5`/`2.6`). С `ai: boss` подключается [BossAI.cs](RiotGalaxy.Core/AI/BossAI.cs) —
**фазовая** машина: влёт → горизонтальный свип у верхней кромки; 3 фазы по доле HP (>66% / >33% / ≤33%)
с разными паттернами (прицельная очередь → веер вниз → радиальный залп); перед каждым залпом —
**телеграф** (вспышка `Tint` ~0.6с + почти остановка), в фазе 3 — подмога (`SpawnAdds`) и тряска.
BossAI сам ведёт движение (`Movement=null`) и огонь (`ShootSafe=true`, через `Weapon.FireShell`),
таймерная стрельба `Enemy` отключена.

## 13. Бонусы и столкновения (бой)

**Столкновения** — без физдвижка, в [CollisionSystem.cs](RiotGalaxy.Core/Managers/CollisionSystem.cs)
(`GameManager.ProcessGameObjects` вызывает `_collisions.ResolveAll(GameObjects)`). Каждая пара
проверяется **один раз** (O(n²/2), AABB через `GameObject.GetBounds/Intersects`); обработчики
несимметричны, поэтому для пары вызываются оба порядка. Диспетчер: снаряд игрока↔враг,
враг↔игрок (таран), вражеский снаряд↔игрок, бонус↔игрок. Лазер не исчезает при попадании.

**Бонусы** — [Bonus.cs](RiotGalaxy.Core/GameObjects/Bonus.cs): усиления задаёт `BonusType`
(`HP_UP` — хил, `POWER`/`BULLET_UP` — ×2 урон, `RAPID` — темп, `SPEED` — скорость, `NUKE_BOMB` — убить всех) —
один класс `Bonus`, спрайт/эффект по типу (`Apply`). `BonusStar` — отдельный класс: даёт **кредиты**
(несёт номинал = `reward` убитого врага), притягивается магнитом.

**Дроп — авторский, а не случайный.** Звёзды (кредиты) падают со **всех** врагов:
`SpawnBonusOnEnemyDeath` дробит награду на несколько звёзд (число = `reward / starValue`, тяжёлые
сыплют больше; разлёт от точки). Усиления же **не выпадают случайно** — они прописаны в YAML уровня
полем `drop: <hp|power|rapid|speed|nuke>` (+ опц. `dropChance: %`) на конкретном враге; при его гибели
`GameManager.SpawnAuthoredDrop` создаёт этот бонус (проброс `Level.SpawnInfo.Drop` → `Enemy.DropBonus`).
Опциональный «фоновый» случайный бафф включается только если `bonuses.yaml buffDropChance > 0`
(по умолчанию **0**). После зачистки уровня — **окно сбора** звёзд (`levelClearCollectSeconds`,
`_levelClearTimer`) перед переходом.

**Очки vs кредиты (роли).** **Очки** (`Player.Score`) начисляются за **убийства** (= `reward` врага),
идут в рекорд (`SaveData.HighScore`), не тратятся. **Кредиты** (`Currency`) — мета-валюта магазина:
их даёт **сбор звёзд** (+ бонус за уровень), банкуются в `SaveData.Currency`. Пропустил звезду —
потерял кредиты, очки за кил остаются. Апгрейд «Магнит» напрямую повышает сбор кредитов.

**Временные баффы (этап 3).** Подборы `POWER`/`RAPID`/`SPEED` (`BonusType`) дают временный эффект:
`Apply` → `PlayerShip.ApplyBuff(id)`, бафф тикает по времени (`TickBuffs`), показан в HUD. Множители
урона/темпа берёт оружие через `PlayerShip.EffectiveDamageMult/EffectiveFireRateMult` (апгрейд ×
активный бафф), скорость — через `MaxSpeed` в `Update`. Параметры (множитель/длительность/шанс
выпадения) — в `bonuses.yaml` (`BonusConfig.Buffs`). Сбрасываются при новой партии (корабль пересоздаётся).

**Магнит звезды** — оборудование корабля, параметры в `weapons.yaml` (`magnet`:
`radius`/`pullSpeed`/`turnSpeed`, см. `WeaponConfig.Magnet`). В радиусе `radius` звезда
**плавно доворачивает** курс на игрока (скорость доворота `turnSpeed` град/сек, кратчайшим
путём — `ApproachAngle` нормализует угол через ±180°, иначе на границе разворот «не туда»)
и летит к нему со скоростью `pullSpeed`; вне радиуса — медленно падает вниз.

## 14. Меню и экраны (ScreenSystem)

Папка [Screens/](RiotGalaxy.Core/Screens/). **Все** игровые состояния — это экраны:

- **`Screen`** (база) — сам читает клавиатуру/мышь/тач (с защитой от ложного клика на 1-м кадре),
  хелпер центрированного текста `DrawCentered(..., scale)` и **тач-дружелюбные** константы
  масштаба (`TitleScale`/`ItemScale`/`HintScale`) + `CenteredItemRect(text, y, scale)` — крупная
  зона нажатия (мин. высота `MinTouchHeight≈84` вирт.px), совпадающая с позицией текста.
  Оформление меню (без ассетов, через `SimpleTexture`): `DrawDimmer` (затемнить фон/звёзды),
  `DrawPanel`/`PanelRect` (тёмная панель с рамкой-акцентом под контентом), `DrawMenuItem`
  (выбранный пункт — жёлтый в маркерах `« »`, без «белых прямоугольников»).
- **`ScreenSystem`** — хранит активный экран, проксирует Update/Draw.
- Меню: **`SplashScreen`** (логотип + таймер → меню), **`MainMenuScreen`**
  (Начать/Настройки/Выход), **`SettingsScreen`** (громкость → `settings.yaml`),
  **`NextLevelScreen`** (между уровнями: номер/описание/очки).
- Игровые: **`GameplayScreen`** (тонкая обёртка → `GameManager.UpdateGameplay/DrawGameplay`),
  **`PausedScreen`** (замороженная игра + затемнение), **`GameOverScreen`**/**`VictoryScreen`**
  (рестарт по **тапу**/Enter — на телефоне нет Space/Esc; «в меню» — Esc/кнопка «Назад»).
- **`DialogueScreen`** — диалоги/брифинги: реплики по очереди (имя + текст с переносом + опц.
  портрет) в нижней панели; тап/пробел — далее, Esc — пропустить. Контент —
  `Content/Dialogues/*.yaml` ([Utils/Dialogue.cs](RiotGalaxy.Core/Utils/Dialogue.cs)). Запуск:
  `GameManager.PlayDialogue(name, next)` → `GameState.Dialogue` → `EndDialogue()` переходит в `next`
  (нет файла — сразу `next`). Основа сюжета (этап 4). Демо: интро `intro.yaml` перед 1-м уровнем.
- **`ShopScreen`** (`GameState.Shop`) — магазин постоянных апгрейдов: список из `upgrades.yaml`
  с уровнем/ценой, покупка за `SaveData.Currency` (тап), сохранение в профиль. Вход — из меню
  и **между уровнями** (`NextLevelScreen` → «Магазин»). Возврат — `GameManager.OpenShop(returnTo)`/
  `CloseShop()`. Эффект применяется при старте партии и при продолжении между уровнями (см. §15).

`GameManager.Update/Draw` для ВСЕХ состояний делегируют в `ScreenSystem`; экран создаётся в
`ChangeGameState`. Боевая петля и отрисовка боя остаются в `GameManager` (их вызывает
`GameplayScreen`). Переходы между состояниями (Esc/P — пауза, Space — заново, Q/Esc — меню)
обрабатывает `Game1.HandleGameplayKeys` по `CurrentGameState`.

## 15. Уровни и прогрессия

[Utils/Level.cs](RiotGalaxy.Core/Utils/Level.cs) грузит `Content/Levels/level{N}.yaml`
и разворачивает события в таймлайн спавна. [LevelDirector](RiotGalaxy.Core/Managers/LevelDirector.cs)
спавнит врагов по `Level.Tick(dt)`, ведёт `EnemiesRemaining`/`CurrentLevel` (а `GameManager`
отдаёт их делегирующими свойствами). Когда все враги боя заспавнены и убиты — `OnBattleCleared`
(окно сбора звёзд → бонус/банк) спрашивает [MissionDirector](RiotGalaxy.Core/Managers/MissionDirector.cs)
следующий шаг: брифинг / следующий бой / босс / магазин (`Content/Missions/<id>.yaml`). Бои внутри
миссии идут без экрана-проставки (`EnterBattle` грузит бой по имени и остаётся в `Playing`),
магазин — в конце миссии; когда миссии в `campaign.yaml` закончились — `Victory`. Игрок и счёт
переносятся между боями. **Гибель → рестарт текущей миссии** с её начала (`RestartMission`,
не вся кампания и не текущая волна); мета-прогресс (кредиты/апгрейды/оружие) остаётся в профиле.
**Чекпоинт/«Продолжить»:** при входе в каждый бой `EnterBattle` пишет позицию (миссия/волна/счёт)
в профиль (`SaveData.SetCheckpoint`); пункт меню **«Продолжить»** (`ContinueCampaign`→`MissionDirector.ResumeAt`)
виден при наличии чекпоинта и возобновляет с той волны; чекпоинт стирается по завершении кампании
(`FinishCampaign`) и при «Начать игру». **Профили:** 3 слота (слот 1 — legacy `save.yaml`, слоты 2/3 — `save2/3.yaml`);
выбор — на экране Profile (Enter — играть, R — «начать заново»/сброс с подтверждением), текущий
слот помнится в `settings.yaml` (`LastProfile`), сменить — пункт меню «Сменить профиль».
Рекорд/прогресс/валюта/апгрейды сохраняются в профиле ([Utils/SaveData.cs](RiotGalaxy.Core/Utils/SaveData.cs),
`save<N>.yaml`): грузится при старте (`GameManager.LoadContent`), пишется в конце партии
(`ChangeGameState`) и при загрузке уровня (`LevelDirector.Load`). См. §16.

**Прогрессия/магазин (этап 3).** Убийства дают **очки** (рекорд). **Кредиты** (на магазин) даёт
**сбор звёзд** (звезда несёт `reward` врага) + бонус за прохождение уровня (`levelClearBonus*` в
bonuses.yaml, в `AdvanceLevel`); банкуются в `SaveData.Currency`. Баланс (награды/цены) — целиком в YAML
(полный забег при хорошем сборе ≈ ~1955 кредитов, весь арсенал ≈ ~8 забегов). Магнит влияет на сбор. В магазине (`ShopScreen`, §14) её тратят на
постоянные апгрейды ([Utils/UpgradeConfig.cs](RiotGalaxy.Core/Utils/UpgradeConfig.cs) ← `upgrades.yaml`),
купленные уровни лежат в `SaveData.Upgrades` (а уровни/разблокировка оружия — в
`SaveData.WeaponLevels`, см. §11). Эффект применяется в `PlayerShip.ApplyUpgrades()`
(HP/скорость + множители урона/темпа, читаемые `Weapon`; радиус магнита — в `BonusStar`) — при
создании корабля и при продолжении между уровнями (после магазина). Между уровнями валюта
банкуется в профиль (`AdvanceLevel`), магазин открывается с экрана `NextLevelScreen`.

**Активные навыки (этап 3).** Данные — `skills.yaml` ([Utils/SkillsConfig.cs](RiotGalaxy.Core/Utils/SkillsConfig.cs)),
рантайм — `PlayerShip`: `UseSkill(id)` применяет эффект (по id: `shield` → неуязвимость на `duration`,
`nuke` → `KillAllEnemies`) и запускает кулдаун (`TickSkillCooldowns`). Активация — клавишей из конфига
(десктоп, `InputManager`) или тач-кнопкой `ButtonSkill` (рисует затемнение-кулдаун; команда
`CommandUseSkill`). Кнопки создаются в `CreateSkillButtons` (справа внизу). Новый навык: id в yaml +
ветка в `UseSkill`.

Формат уровня (YAML):

```yaml
description: "First battle"
spawnInterval: 1.0          # интервал между спавнами по умолчанию
events:
  - { enemy: blue, count: 3 }            # типы: blue/green/red/scout/boss
  - { interval: 3 }                      # сменить интервал
  - { enemy: red, count: 5 }
  - { wait: 2 }                          # пауза
  - { enemy: green, count: 8, formation: true }  # спавн в формацию (улей)
  - { enemy: blue, count: 3, route: zmeyka1-left, after: formation } # вход по маршруту; after: bounce/scatter/formation
  - { enemy: boss, count: 1 }            # босс
  - parallel:                            # синхронные волны: группы спавнятся одновременно,
      - - { enemy: blue, count: 3, route: zmeyka1-left }   # основной таймлайн ждёт их завершения
      - - { enemy: blue, count: 3, route: zmeyka1-right }  # (аналог trigger/sync_cmd из оригинала)
```

## 16. Конфиги (YAML)

Через библиотеку **YamlDotNet**. Хелпер [Utils/Yaml.cs](RiotGalaxy.Core/Utils/Yaml.cs)
(camelCase, тихий фолбэк). Файлы:

| Файл | Что | Загрузчик |
|---|---|---|
| `Content/Config/weapons.yaml` | реестр оружия (поведение/уровни/цены) + `magnet` | `Weapons.WeaponConfig.Load()` |
| `Content/Config/enemies.yaml` | враги: hp/урон/скорость/`attackSpeed`/`tactics`/`reward` + вид и поведение (`sprite`/`scale`/`shoot`/`ai`/`wander`/`dirMin`/`dirMax`) | `Utils.EnemyConfig.Load()` |
| `Content/Config/bonuses.yaml` | параметры бонусов (хил HP, очки за звезду) | `Utils.BonusConfig.Load()` |
| `Content/Config/options.yaml` | экран + игрок (HP, скорость, время неуязвимости…) | `Utils.GameOptions.Load()` |
| `Content/Config/effects.yaml` | частицы (взрывы/искры), screenshake, слои параллакса | `Utils.EffectsConfig.Load()` |
| `Content/Config/upgrades.yaml` | постоянные апгрейды магазина (цена/рост/эффект) | `Utils.UpgradeConfig.Load()` |
| `Content/Config/skills.yaml` | активные навыки (иконка/клавиша/кулдаун/длительность) | `Utils.SkillsConfig.Load()` |
| `settings.yaml` (рядом с .exe) | громкость (пользовательская) | `Utils.GameSettings` |
| `save.yaml` (рядом с .exe) | профиль игрока: рекорд, дальний уровень, валюта | `Utils.SaveData` |

Все три читаются в `GameManager.LoadContent`. У каждого конфига есть дефолты в коде —
игра работает и без файлов. Уровни (`Level`) — тоже YAML (§15). Диалоги —
`Content/Dialogues/*.yaml` ([Utils/Dialogue.cs](RiotGalaxy.Core/Utils/Dialogue.cs), грузятся по
требованию через `Dialogue.Load(name)`; см. §14).

**Локализация UI** — [Utils/Loc.cs](RiotGalaxy.Core/Utils/Loc.cs): строки меню/настроек/итогов/
паузы/HUD/диалога вынесены из кода в `Content/Locale/<lang>.yaml` (плоская карта ключ→текст,
эталон — `ru.yaml`, есть `en.yaml`). Доступ: `Loc.T("menu.start")`, шаблоны — `Loc.F("hud.hp", hp, max)`.
Грузится в `GameManager.LoadContent` по выбранному языку (`Loc.Load(GameSettings.Language)`; настройки
читаются перед локалью). **Переключение языка** — в настройках (`SettingsScreen.ToggleLanguage` → `Loc.Load` +
сохранение в `settings.yaml`), применяется сразу (строки читаются каждый кадр). Нет ключа/файла → возвращается
сам ключ. Новая локаль — копия `ru.yaml` с теми же ключами. *(имена оружия/часть боевых сообщений пока в коде).*

> Папки YAML-контента (`Config`/`Levels`/`Routes`/`Dialogues`) копируются в выход: на desktop —
> `<None CopyToOutputDirectory>`, на Android — `<AndroidAsset>` в `Assets/Content/...` (оба csproj).
> Добавляешь новую папку YAML — пропиши её в **обоих** проектах.
>
> Почему YAML, а не Content Pipeline: это **текстовые** конфиги, их не нужно компилировать
> в `.xnb`. Они копируются в выход через `<None CopyToOutputDirectory>` (см. §8) и читаются
> обычным `File.ReadAllText` из `AppContext.BaseDirectory`.

## 17. Тестовая панель и сообщения

**Сообщения** — [MessageLog.cs](RiotGalaxy.Core/Managers/MessageLog.cs): короткие всплывающие
подписи над панелью кнопок («+25 HP», «Оружие: лазер», «+10 очк.»), затухают. Вызываются из
точек событий (`Bonus.Apply`, `PlayerShip.ChangeWeapon/UpgradeWeapon`, команды кнопок).

**Тестовая панель (debug).** Внизу слева во время игры — ряд кнопок ([Interface/MyButton.cs](RiotGalaxy.Core/Interface/MyButton.cs),
регистрируются в `InputManager.GuiButtons`): смена оружия (Cannon/Minigun/Laser), апгрейд,
лечение, «убить всех», «следующий уровень». Каждая кнопка несёт `ICommand` (папка
[Commands/](RiotGalaxy.Core/Commands/)). Создаются при старте партии, чистятся при выходе в меню.

---

## 18. Как собрать и запустить

### 18.0. Что нужно один раз

- **.NET SDK** (проект на `net6.0`, но `RollForward=Major` позволяет собирать и на новых SDK — например, на установленном 9.x).
- **`dotnet-mgcb`** — компилятор ресурсов. Ставится локально из манифеста [.config/dotnet-tools.json](.config/dotnet-tools.json):

  ```bash
  cd MonoGame
  dotnet tool restore        # один раз после клона: поставит dotnet-mgcb
  ```

  Без него сборка контента (`.xnb`) падает с `dotnet mgcb … not found`.

### 18.1. Быстрый запуск (дев)

```bash
# из корня репозитория — собирает и запускает:
./run_game.sh

# или вручную из папки MonoGame:
dotnet build RiotGalaxy.sln
dotnet run --project RiotGalaxy.DesktopGL/RiotGalaxy.DesktopGL.csproj
```

Управление сейчас:

- **Меню**: мышь или стрелки + Enter; Space — начать; Esc — выход.
- **В игре**: ←/→ или A/D — движение, **Space** — огонь, **1/2/3** — смена оружия,
  **Esc/P** — пауза (в паузе **Q** — выход в меню).
- **Конец игры** (победа/поражение): Space — заново, Esc — в меню.

### 18.2. Dev (Debug) vs Release сборка

`dotnet` собирает в конфигурации **Debug** по умолчанию. Флаг `-c` (`--configuration`)
переключает:

```bash
# Debug — для разработки: без оптимизаций, с отладочной информацией (.pdb), быстрее компиляция
dotnet build -c Debug

# Release — для распространения: с оптимизациями, работает быстрее
dotnet build -c Release
dotnet run -c Release --project RiotGalaxy.DesktopGL/RiotGalaxy.DesktopGL.csproj
```

Куда кладётся результат:

| Конфигурация | Путь к бинарникам |
|---|---|
| Debug | `RiotGalaxy.DesktopGL/bin/Debug/net6.0/` |
| Release | `RiotGalaxy.DesktopGL/bin/Release/net6.0/` |

В обоих случаях рядом появляется папка `Content/` со скомпилированными `.xnb`
(копируется автоматически благодаря `MonoGameContentReference`).

### 18.3. Сборка дистрибутива под разные ОС (`dotnet publish`)

`build`/`run` годятся для разработки. Чтобы получить **готовый дистрибутив** для
конкретной операционной системы, используют `dotnet publish` с указанием **RID**
(Runtime Identifier) — кода целевой платформы.

Проект `RiotGalaxy.DesktopGL` (OpenGL) кроссплатформенный — один и тот же код собирается
под все десктопные ОС, меняется только RID:

| ОС | RID |
|---|---|
| Windows 64-bit | `win-x64` |
| Linux 64-bit | `linux-x64` |
| macOS (Intel) | `osx-x64` |
| macOS (Apple Silicon) | `osx-arm64` |

```bash
# Self-contained (включает .NET runtime — на машине пользователя SDK не нужен):
dotnet publish RiotGalaxy.DesktopGL/RiotGalaxy.DesktopGL.csproj \
    -c Release -r win-x64 --self-contained true

# Под Linux:
dotnet publish RiotGalaxy.DesktopGL/RiotGalaxy.DesktopGL.csproj \
    -c Release -r linux-x64 --self-contained true

# Под macOS (Apple Silicon):
dotnet publish RiotGalaxy.DesktopGL/RiotGalaxy.DesktopGL.csproj \
    -c Release -r osx-arm64 --self-contained true
```

Результат — в `bin/Release/net6.0/<RID>/publish/` (вместе с папкой `Content/`).

Полезные флаги:

- `--self-contained true` — упаковать .NET в дистрибутив (пользователю не нужен
  установленный runtime). `false` — дистрибутив меньше, но требует .NET на машине.
- `-p:PublishSingleFile=true` — собрать в один исполняемый файл.
- `-p:PublishTrimmed=true` — отрезать неиспользуемый код (меньше размер; с MonoGame
  тестируйте — тримминг иногда удаляет нужное через рефлексию).

> ⚠️ Кросс-публикация скачивает runtime-пакеты целевой платформы с nuget при первой
> сборке — нужен интернет. Сборка под `osx-*`/`win-*` с Linux работает (это просто
> упаковка), но саму игру для финальной проверки лучше запускать на целевой ОС.

### 18.4. Сборка в Docker (требование PRD)

Согласно [prd.md](../prd.md), сборка должна идти в контейнере, без установки пакетов в
хост. Идея: в образе есть .NET SDK, внутри выполняется `dotnet tool restore` (ставит
`dotnet-mgcb`) и затем `dotnet publish` под нужный RID. Dockerfile в проекте пока не
заведён — это часть будущего этапа CI/CD.

### 18.5. Android-сборка

Проект `RiotGalaxy.Android/` (`net9.0-android`) собирает APK. Он **вне** `RiotGalaxy.sln`,
чтобы не требовать android-workload при обычной desktop-сборке на хосте.

**Ключевое решение — линковка исходников, а не ProjectReference.** `Core` ссылается на
пакет `MonoGame.Framework.DesktopGL`, а Android-обёртке нужен `MonoGame.Framework.Android`.
Поэтому Android-проект не делает ProjectReference на Core, а **компилирует его исходники**:

```xml
<Compile Include="../RiotGalaxy.Core/**/*.cs" Exclude="...obj...;...bin..." LinkBase="CoreShared" />
```

Тот же код (включая `Game1`) собирается против Android-фреймворка. Точка входа —
`MainActivity : AndroidGameActivity` (создаёт `Game1.Instance`, `SetContentView`, `Run()`).

**Контент в APK:**
- `.xnb` — `MonoGameContentReference` + `<MonoGamePlatform>Android</MonoGamePlatform>`
  (MGCB пересобирает контент под `/platform:Android`, перекрывая `/platform:DesktopGL`
  из `.mgcb`).
- YAML — как `AndroidAsset` с `Link="Assets/Content/..."`, чтобы лечь в `assets/Content/...`
  внутри APK. Там их находит `TitleContainer.OpenStream("Content/...")` (см. §8) — тот же
  путь, что и на desktop.

**Сборка — только в Docker** (по PRD, хост не засоряем):

```bash
cd MonoGame
./docker/build-image.sh          # один раз: образ riotgalaxy-android-build (.NET9+JDK17+AndroidSDK+workload)
./run_phone.sh [Debug] [сек]     # ⭐ всё разом: собрать APK + поставить/запустить + лог → riot_android.log
./docker/build-apk.sh Debug      # APK → RiotGalaxy.Android/bin/Debug/net9.0-android/*-Signed.apk
./docker/run-on-device.sh        # установить и запустить на USB-телефоне (adb из контейнера), снять logcat
./docker/shell.sh                # интерактивная оболочка в контейнере (исходники в /src)
```

`run_phone.sh` (корень) — удобный аналог `run_game.sh` для Android: пересобирает APK, ставит на
телефон, запускает и дублирует лог запуска в `riot_android.log` (тег `DOTNET`: `[DBG]/[ERR]` + ошибки),
чтобы можно было проанализировать. Важно: `run-on-device.sh` сам APK **не** пересобирает —
после правок кода нужен либо `run_phone.sh`, либо `build-apk.sh` перед `run-on-device.sh`.

`run-on-device.sh` пробрасывает USB в контейнер (`--privileged -v /dev/bus/usb`). При первом
подключении телефон спросит разрешение на отладку — подтвердить (ключ adb хранится в
`docker/.home/.android`). Наши `Log.Debug` видны в logcat под тегом **DOTNET**
(`Console.WriteLine` на .NET Android идёт в logcat).

**Грабли первого запуска (исправлены, важно для понимания):**

- **`EmbedAssembliesIntoApk=true` для Debug** — иначе .NET Android использует Fast Deployment
  (managed-сборки доставляются отдельно при `dotnet build -t:Run`), и установка готового APK
  через `adb install` падает с `SIGABRT: No assemblies found in .__override__`.
- **`<ContentFolder>Content</ContentFolder>` у `MonoGameContentReference`** — иначе `.xnb`
  пакуются в `assets/RiotGalaxy.Content/`, а `ContentManager` (RootDirectory `Content`) ищет
  их в `assets/Content/` и не находит.
- **`SpriteBatch` создаётся в `GameManager.LoadContent`, а не в конструкторе** — на Android
  `GraphicsDevice` в конструкторе `Game1` ещё null (на DesktopGL `ApplyChanges()` создаёт его
  сразу, поэтому там работало).

Образ описан в [docker/Dockerfile.android](docker/Dockerfile.android). В него входят шрифты
(`fonts-dejavu-core`, `fontconfig`) — без них MGCB `FontDescriptionProcessor` не находит
шрифт `DejaVu Sans Mono` из `TestFont.spritefont`.

**Масштаб экрана (letterbox) и тач.** Игра логически работает в фиксированных 1280×768
(`GameManager.ScreenWidth/Height` — виртуальные). На Android back buffer = весь экран
(`IsFullScreen=true`); вся сцена рисуется через матрицу масштаба в единственном
`SpriteBatch.Begin(..., _renderMatrix)` (см. `UpdateRenderTransform()`), пропорции
сохраняются (чёрные поля по бокам). Координаты ввода переводятся обратно
`GameManager.ScreenToVirtual()`. Тач: `TouchPanel` под `#if ANDROID` в `InputManager`
(игра) и `Screen` (меню), вместо мыши. На desktop scale=1 — поведение не меняется.

`settings.yaml` на Android пишется в `AppContext.BaseDirectory`
(`/data/user/0/<pkg>/files/`, writable) — отдельного пути не понадобилось.

**Стрельба и «Назад» на Android.** Стрельба — автоогонь (`#if ANDROID` в
`InputManager.HandleScGameInput`: `player?.Fire()` каждый кадр; темп ограничивает оружие).
Кнопка «Назад»: на Android 13+ системный back приходит ТОЛЬКО через predictive back
(`OnBackInvokedDispatcher`), а не через `OnBackPressed`/`KEYCODE_BACK`. Поэтому в
`MainActivity` зарегистрирован `IOnBackInvokedCallback`, и — обязательно — в манифесте
выставлен `android:enableOnBackInvokedCallback="true"` (без флага система игнорирует
наш колбэк). Логика разнесена по потокам: `GameManager.OnBackRequested()` (из UI-потока только ставит
флаг) и `ProcessPendingBack()` (смена состояния в игровом потоке, без гонки с `Update`).
Из игры → меню, из меню/заставки → выход.

> ⚠️ Осталось: запуск на эмуляторе/CI (этап 6 PRD). iOS — не настраивался.

### 18.6. Отладка в VSCode и лог

В корне репозитория есть `.vscode/`:

- **`launch.json`** — конфигурация «RiotGalaxy DesktopGL (Debug)». Жмёшь **F5** —
  VSCode сначала собирает проект (preLaunchTask `build-desktopgl`), затем запускает
  `RiotGalaxy.DesktopGL.dll` через `coreclr` с выводом в интегрированный терминал.
  Работают точки останова, шаги, watch.
- **`tasks.json`** — задачи `build-desktopgl` (сборка, дефолтная по Ctrl+Shift+B) и
  `run-desktopgl` (запуск без отладчика).

**Логирование** — [Log.cs](RiotGalaxy.Core/Utils/Log.cs), статический класс
`RiotGalaxy.Utils.Log`:

- `Log.Debug(msg)` / `Log.Error(msg)` — пишут одновременно в `Console`
  (видно в терминале/Debug Console VSCode) и в файл `riot.log`.
- Файл лежит рядом с приложением: `RiotGalaxy.DesktopGL/bin/Debug/net6.0/riot.log`
  (через `AppContext.BaseDirectory`); **очищается при каждом старте** игры.
- Сейчас логируются старт уровня и конец игры (без покадрового спама). Добавляй
  `Log.Debug` точечно для отладки.
- На Android позже добавим ветку `Android.Util.Log` → `logcat`
  (`adb logcat -s RiotGalaxy:*`).

---

## 19. Частые грабли новичка (и как их обходим здесь)

| Симптом | Причина | Решение |
|---|---|---|
| `Could not find … .xnb` при запуске | имя в `Content.Load` ≠ путь в `.mgcb`, либо `RootDirectory` неверный | имя = путь без расширения; `RootDirectory="Content"` |
| Сборка контента падает: `dotnet mgcb … not found` | не установлен `dotnet-mgcb` | `dotnet tool restore` |
| Спрайт с прозрачностью рисуется с тёмной каймой | несоответствие premultiplied alpha | в `.mgcb` `PremultiplyAlpha=True` + стандартный `SpriteBatch.Begin()` |
| `InvalidOperationException` при `Draw` | рисование вне `Begin()/End()` | всё рисование — между `spriteBatch.Begin()` и `End()` |
| Движение «дёргается» при разном FPS | скорость без учёта времени | умножать на `gameTime.ElapsedGameTime.TotalSeconds` |
| Меняешь PNG, а в игре старый | не пересобрал контент | пересборка (`dotnet build`) перекомпилирует `.xnb` |
| В тексте вместо символа `*` (тире `—`, `·`, `«»`, стрелки) | символ вне `CharacterRegions` шрифта → подставляется `DefaultCharacter` | добавить диапазон в `TestFont.spritefont` (там есть ASCII/Latin-1/кириллица/тире/стрелки) |

---

## 20. Куда смотреть дальше

- **Хочешь понять поток управления** → начни с [Program.cs](RiotGalaxy.DesktopGL/Program.cs)
  → [Game1.cs](RiotGalaxy.Core/Game1.cs) → [GameManager.cs](RiotGalaxy.Core/Managers/GameManager.cs).
- **Хочешь понять объект на экране** → [GameObject.cs](RiotGalaxy.Core/GameObjects/GameObject.cs)
  → [PlayerShip.cs](RiotGalaxy.Core/GameObjects/PlayerShip.cs).
- **Открытые задачи** → [TODO.md](TODO.md); **лог миграции** → [MIGRATION.md](MIGRATION.md).
- **Требования и архитектурные решения** → [prd.md](../prd.md).
- **Официальные туториалы MonoGame** → <https://docs.monogame.net/articles/tutorials.html>
