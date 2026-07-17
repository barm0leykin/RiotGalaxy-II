using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Content;
using RiotGalaxy.Core.GameObjects;
using RiotGalaxy.Core.Components;
using RiotGalaxy.Core.Interface;

namespace RiotGalaxy.Core.Managers
{
    /// <summary>
    /// Класс GameManager - центральный диспетчер игры.
    /// Аналог GameManager из CocosSharp для MonoGame.
    /// </summary>
    public class GameManager
    {
        // Singleton pattern
        private static GameManager _instance;
        public static GameManager Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new GameManager();
                return _instance;
            }
        }

        // Ссылки на основные компоненты игры
        private GraphicsDeviceManager _graphics;
        private ContentManager _content;
        private SpriteBatch _spriteBatch;
        private SpriteFont _defaultFont;
        
        // Базовые игровые состояния
        public enum GameState { Splash, Profile, MainMenu, Settings, Playing, Paused, GameOver, Victory, NextLevel, Dialogue, Shop, DevMenu }
        public GameState CurrentGameState { get; private set; }

        // Диалог (брифинг/сюжет) и состояние, в которое перейти после него.
        private Utils.Dialogue _dialogue;
        private GameState _dialogueNext;
        private System.Action _dialogueThen; // если задано — вызвать вместо перехода в _dialogueNext
        public Utils.Dialogue CurrentDialogue => _dialogue;

        // Оркестратор уровней (World/Hive, спавн, прогрессия, счётчики врагов) — гоняет один бой.
        private LevelDirector _levels; // создаётся в конструкторе (нужен список GameObjects)

        // Оркестратор кампании: миссия = цепочка брифингов/боёв/босса/магазина.
        private readonly MissionDirector _mission = new MissionDirector();
        private CampaignFlow _campaign; // поток кампании (создаётся в конструкторе)
        private System.Action _shopThen;     // продолжение после закрытия магазина (поток миссии)

        private int _lastScore; // итоговый счёт для экранов GameOver/Victory
        public int LastScore => _lastScore;
        /// <summary>Зафиксировать итоговый счёт (зовёт CampaignFlow при финале кампании).</summary>
        public void SetLastScore(int score) => _lastScore = score;

        // Окно сбора звёзд после зачистки уровня (сек до перехода); 0 — не активно.

        // Время текущего кадра (для DrawGameplay, вызываемого из GameplayScreen.Draw).
        private GameTime _drawTime = new GameTime();

        // Счётчики и прогрессия — делегируются в LevelDirector (публичный API сохранён).
        public int EnemiesKilled => _levels.EnemiesKilled;
        public int EnemiesRemaining => _levels.EnemiesRemaining;
        public int CurrentLevel => _levels.CurrentLevel;
        public int TotalLevels => _levels.TotalLevels;
        public string CurrentLevelDescription => _levels.CurrentLevelDescription;

        // Кампания (для HUD): номер/название миссии и номер акта (по номеру миссии).
        public int CurrentMissionNumber => _mission.MissionNumber;
        /// <summary>Номер текущей волны внутри миссии (1-based) — для HUD «Миссия N/W».</summary>
        public int CurrentWaveNumber => _mission.CurrentWaveNumber;
        public int TotalMissions => _mission.TotalMissions;
        public string CurrentMissionTitle => _mission.CurrentMissionTitle;
        public int CurrentAct { get { int n = _mission.MissionNumber; return n <= 5 ? 1 : n <= 9 ? 2 : 3; } }

        // Система экранов меню (заставка/меню/настройки)
        public Screens.ScreenSystem Screens { get; private set; } = new Screens.ScreenSystem();

        // Доступ к шрифту для экранов
        public SpriteFont Font => _defaultFont;

        // Основные объекты игры
        public List<GameObject> GameObjects { get; private set; }
        public PlayerShip Player { get; private set; }

        // Базовые игровые параметры (ВИРТУАЛЬНОЕ разрешение — вся игровая логика в нём)
        public int ScreenWidth { get; private set; }
        public int ScreenHeight { get; private set; }

        // Letterbox-масштабирование виртуального кадра (1280x768) под реальный back buffer
        // (на Android — весь экран). Вся отрисовка идёт через _renderMatrix, ввод
        // преобразуется обратно через ScreenToVirtual.
        private Matrix _renderMatrix = Matrix.Identity;
        private float _renderScale = 1f;
        private Vector2 _renderOffset = Vector2.Zero;

        // Тряска экрана (вынесена в Effects.ScreenShake); смещение читает letterbox-матрица.
        private readonly Effects.ScreenShake _shake = new Effects.ScreenShake();

        // Система частиц (взрывы, искры). Обновляется в UpdateGameplay, рисуется в DrawGameplay.
        public Effects.ParticleSystem Particles { get; } = new Effects.ParticleSystem();

        // Фон: небо-градиент биома + параллакс-звёзды (вынесено в Effects.BackgroundRenderer).
        private readonly Effects.BackgroundRenderer _background = new Effects.BackgroundRenderer();

        // Отрисовщик боевого HUD (вынесен из GameManager).
        private readonly Interface.HudRenderer _hud = new Interface.HudRenderer();

        // Обработка столкновений (вынесена из GameManager). Создаётся в LoadContent
        // с явными зависимостями (частицы + тряска) — без обратной связи через Instance.
        private CollisionSystem _collisions;


        // Вспомогательные текстуры
        public Texture2D SimpleTexture { get; set; }
        /// <summary>Мягкая radial-glow текстура для неонового свечения (аддитивная отрисовка UI).</summary>
        public Texture2D GlowTexture { get; set; }
        public GraphicsDevice GraphicsDevice => _graphics.GraphicsDevice;

        // Bloom post-process (вынесен в Effects.BloomRenderer; null-эффект → прямой рендер).
        private Effects.BloomRenderer _bloomRenderer;

        /// <summary>Letterbox-матрица текущего кадра — чтобы экраны могли переоткрыть UI-батч
        /// с той же трансформацией (напр. аддитивный проход неонового свечения).</summary>
        public Matrix RenderMatrix => _renderMatrix;

        /// <summary>Переоткрыть UI-батч с заданным блендом и той же letterbox-матрицей
        /// (blend=null → стандартный AlphaBlend). Вызывать парой End()/Begin() из экрана.</summary>
        public void BeginUiBatch(SpriteBatch sb, BlendState blend) =>
            sb.Begin(SpriteSortMode.Deferred, blend, null, null, null, null, _renderMatrix);

        /// <summary>
        /// Пересчитывает letterbox-матрицу под текущий размер back buffer (вызывается каждый
        /// кадр перед отрисовкой — покрывает смену ориентации/размера экрана).
        /// </summary>
        private void UpdateRenderTransform()
        {
            var vp = _graphics.GraphicsDevice.Viewport;
            float scale = Math.Min((float)vp.Width / ScreenWidth, (float)vp.Height / ScreenHeight);
            if (scale <= 0f) scale = 1f;
            _renderScale = scale;
            _renderOffset = new Vector2(
                (vp.Width - ScreenWidth * scale) / 2f,
                (vp.Height - ScreenHeight * scale) / 2f);
            // Смещение screenshake задаётся в виртуальных пикселях → умножаем на scale,
            // чтобы амплитуда тряски одинаково выглядела при любом letterbox-масштабе.
            _renderMatrix = Matrix.CreateScale(scale, scale, 1f)
                          * Matrix.CreateTranslation(
                                _renderOffset.X + _shake.Offset.X * scale,
                                _renderOffset.Y + _shake.Offset.Y * scale, 0f);
        }

        /// <summary>Запустить тряску экрана (фасад над Effects.ScreenShake — call-sites не меняются).</summary>
        public void Shake(float magnitude, float duration = 0.3f) => _shake.Start(magnitude, duration);

        /// <summary>Переводит координаты экрана (пиксели мыши/тача) в виртуальные (1280x768).</summary>
        public Vector2 ScreenToVirtual(Vector2 screenPoint) =>
            (screenPoint - _renderOffset) / _renderScale;

        // Доступ к загрузчику контента (нужен игровым объектам для загрузки спрайтов)
        public ContentManager Content => _content;

        // Обработчик ввода пользователя
        public InputManager userInputHandler;

        // Приватный конструктор для singleton
        private GameManager()
        {
            CurrentGameState = GameState.MainMenu;
            GameObjects = new List<GameObject>();
            _levels = new LevelDirector(GameObjects);
            _campaign = new CampaignFlow(this, _mission, _levels);
            ScreenWidth = 1280;
            ScreenHeight = 768;

            // Инициализируем обработчик ввода
            userInputHandler = InputManager.Instance;
        }

        /// <summary>
        /// Инициализация GameManager
        /// </summary>
        public void Initialize(Game game, GraphicsDeviceManager graphics, ContentManager content)
        {
            _graphics = graphics;
            _content = content;

#if ANDROID
            // На Android оставляем back buffer равным размеру экрана (полноэкранно):
            // фиксированный PreferredBackBuffer заставил бы рисовать игру в углу.
            // Виртуальный кадр 1280x768 масштабируется letterbox'ом (см. UpdateRenderTransform).
            _graphics.IsFullScreen = true;
#else
            // Desktop: окно ровно под виртуальное разрешение (scale=1, без полей).
            _graphics.PreferredBackBufferWidth = ScreenWidth;
            _graphics.PreferredBackBufferHeight = ScreenHeight;
            _graphics.ApplyChanges();
#endif

            // SpriteBatch НЕ создаём здесь: в конструкторе Game1 GraphicsDevice ещё может быть
            // не создан (на Android он появляется позже, чем на DesktopGL). Создаём в LoadContent,
            // когда устройство гарантированно готово (см. LoadContent).
        }

        /// <summary>
        /// Загрузка контента
        /// </summary>
        public void LoadContent()
        {
            System.Diagnostics.Debug.WriteLine("=== GameManager Loading Content ===");

            // Создаём SpriteBatch здесь: GraphicsDevice уже готов на всех платформах
            // (на Android он недоступен в конструкторе Game1 — см. Initialize).
            _spriteBatch = new SpriteBatch(_graphics.GraphicsDevice);
            UpdateRenderTransform(); // первичный расчёт letterbox-матрицы (до первого Draw)

            // Загружаем звуковые эффекты (fire1, explode1)
            AudioManager.Instance.LoadContent(_content);

            // Загружаем шрифт для текста (меню, HUD)
            try
            {
                _defaultFont = _content.Load<SpriteFont>("TestFont");
            }
            catch (Exception ex)
            {
                Utils.Log.Error($"=== Failed to load font 'TestFont': {ex.Message} ===");
            }

            // Bloom-шейдер (только десктоп; на Android .xnb не собирается → останется null,
            // и пост-обработка просто выключится, свечение UI остаётся аддитивным).
            Effect bloomEffect = null;
            try
            {
                bloomEffect = _content.Load<Effect>("Effects/Bloom");
                Utils.Log.Debug("Bloom effect loaded");
            }
            catch (Exception ex)
            {
                Utils.Log.Debug($"Bloom effect not available: {ex.Message}");
            }
            _bloomRenderer = new Effects.BloomRenderer(bloomEffect);

            // Столкновения: явные зависимости (частицы + тряска) вместо Instance изнутри.
            _collisions = new CollisionSystem(Particles, (m, d) => _shake.Start(m, d));

            // Загружаем конфиги из YAML (оружие, враги, параметры игры) и сохранённые настройки
            Utils.GameSettings.Load();           // в т.ч. выбранный язык
            Utils.Loc.Load(Utils.GameSettings.Language); // локализация UI (Content/Locale/<lang>.yaml)
            Weapons.WeaponConfig.Load();
            Utils.EnemyConfig.Load();
            Utils.BonusConfig.Load();
            Utils.GameOptions.Load();
            Utils.EffectsConfig.Load();
            Utils.BiomeConfig.Load();      // биомы актов (небо/звёзды)
            Utils.UpgradeConfig.Load();    // определения апгрейдов (магазин)
            Utils.SkillsConfig.Load();     // активные навыки
            Utils.BarkConfig.Load();       // реплики пилота в бою (barks)
            Utils.AiConfig.Load();         // параметры ИИ/боссов (ai.yaml)
            Utils.SaveData.CurrentProfile = Utils.GameSettings.LastProfile; // последний выбранный слот
            Utils.SaveData.Load(); // профиль игрока: рекорд/прогресс/валюта/апгрейды/оружие

            // Стартовое оружие всегда открыто (ур. 1+).
            var starter = Weapons.WeaponConfig.Starter;
            if (starter != null && Utils.SaveData.GetWeaponLevel(starter.Id) < 1)
                Utils.SaveData.SetWeaponLevel(starter.Id, 1);

            // Параллакс-фон из звёзд (процедурный, без ассетов). Слои — из EffectsConfig,
            // поэтому создаём после загрузки конфигов.
            _background.Init(ScreenWidth, ScreenHeight);
            ApplyBiome("act1"); // биом по умолчанию (меню/старт), дальше меняется по акту миссии

            // Сколько уровней доступно (по файлам Content/Levels/level*.yaml)
            _levels.InitTotalLevels();
            Utils.Log.Debug($"Configs loaded. Levels found: {_levels.TotalLevels}, player HP: {Utils.GameOptions.PlayerMaxHp}");

            ChangeGameState(GameState.Splash);
        }


        /// <summary>
        /// Основной игровой цикл - обновление состояния игры
        /// Адаптировано из GamePlay.cs (CocosSharp)
        /// </summary>
        /// <summary>Игровое время с запуска, сек (для дебаунса кнопок и таймеров вне боя).</summary>
        public double TotalSeconds { get; private set; }

        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            TotalSeconds = gameTime.TotalGameTime.TotalSeconds;

            // Параллакс-фон анимируется во всех состояниях (живой фон в меню и в бою).
            _background.Update(deltaTime);

            // Все состояния — это экраны (ScreenSystem). Логику боя несёт GameplayScreen,
            // оверлеи паузы/итога — Paused/GameOver/VictoryScreen. Переходы между состояниями
            // (Esc/P/Q/Space) по-прежнему обрабатывает Game1.HandleGameplayKeys по CurrentGameState.
            Screens.Update(gameTime);
        }

        /// <summary>
        /// Отрисовка игры
        /// </summary>
        public void Draw(GameTime gameTime)
        {
            _drawTime = gameTime; // для GameplayScreen.Draw → DrawGameplay (нужен gameObject.Draw)
            var device = _graphics.GraphicsDevice;

            // Letterbox: пересчитываем матрицу под текущий back buffer и масштабируем всю сцену.
            UpdateRenderTransform();

            if (SimpleTexture == null) SimpleTexture = Utils.Textures.CreateSolid(GraphicsDevice, Color.White);
            if (GlowTexture == null) GlowTexture = Utils.Textures.CreateGlow(GraphicsDevice);

            // При bloom рисуем сцену в offscreen-таргет, иначе — прямо в back buffer.
            bool useBloom = _bloomRenderer != null && _bloomRenderer.BeginScene(device);
            device.Clear(Color.Black);

            // ── Сцена (как обычно) ──
            _spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, _renderMatrix);
            _background.Draw(_spriteBatch, SimpleTexture, ScreenWidth, ScreenHeight); // небо + звёзды
            Screens.Draw(_spriteBatch);                    // все состояния (вкл. бой/UI)
            _spriteBatch.End();

            if (useBloom)
                _bloomRenderer.EndScene(device, _spriteBatch); // сцена + свечение → back buffer
        }

        /// <summary>Применить биом акта: цвет неба + оттенок звёзд (Effects.BackgroundRenderer).</summary>
        private void ApplyBiome(string id)
        {
            _background.SetBiome(id);
            // Трек боёв биома (biomes.yaml music:, фолбэк — id биома); включится при входе в бой.
            AudioManager.Instance.BattleTrack = Utils.BiomeConfig.Get(id).Music ?? id;
        }

        /// <summary>
        /// Показать реплику босса текущей миссии (which: intro/phase2/phase3/defeat) через MessageLog —
        /// бой не прерывается. Нет реплики с таким тегом — молчит.
        /// </summary>
        public void ShowBossTaunt(string which)
        {
            // Реплики босса лежат в диалоге миссии Content/Dialogues/<mission>_boss.yaml,
            // строки помечены тегом (intro/phase2/phase3/defeat).
            var d = Utils.Dialogue.Load($"{_mission.CurrentMissionId}_boss");
            if (d?.Lines == null) return;
            Utils.DialogueLine line = null;
            foreach (var l in d.Lines)
                if (l.Tag == which) { line = l; break; }
            if (line == null || string.IsNullOrEmpty(line.Text)) return;
            string text = string.IsNullOrEmpty(line.Speaker) ? line.Text : $"{line.Speaker}: {line.Text}";
            MessageLog.Add(text, which == "defeat" ? Color.Gold : new Color(255, 120, 90));
        }

        /// <summary>Биом текущей миссии: явный `biome:` из YAML или по номеру акта (m1–5/6–9/далее).</summary>
        public void ApplyBiomeForCurrentMission()
        {
            int n = _mission.MissionNumber;
            string biome = !string.IsNullOrEmpty(_mission.CurrentBiome) ? _mission.CurrentBiome
                         : (n <= 5 ? "act1" : n <= 9 ? "act2" : "act3");
            ApplyBiome(biome);
        }

        // Запрошен переход «в меню» по кнопке Назад. Выставляется из UI-потока (OnBackPressed),
        // а сама смена состояния выполняется в игровом потоке (ProcessPendingBack) — иначе
        // гонка с циклом Update (чистка GameObjects) роняет игру.
        private volatile bool _backToMenuPending;

        /// <summary>
        /// Запрос «Назад» из UI-потока (Android Back, см. MainActivity). НЕ меняет состояние сам.
        /// </summary>
        /// <returns>true, если приложение должно закрыться (мы уже в меню/заставке);
        /// false — переход в меню отложен в игровой поток.</returns>
        public bool OnBackRequested()
        {
            switch (CurrentGameState)
            {
                case GameState.MainMenu:
                case GameState.Splash:
                    return true; // выходим из приложения (закрытие активности — на UI-потоке)
                default:
                    _backToMenuPending = true; // смену состояния сделает игровой поток
                    return false;
            }
        }

        /// <summary>Обрабатывает отложенный запрос «Назад» — вызывать из игрового потока (Game1.Update).</summary>
        public void ProcessPendingBack()
        {
            if (!_backToMenuPending)
                return;
            _backToMenuPending = false;
            ChangeGameState(GameState.MainMenu);
        }

        /// <summary>
        /// Смена состояния игры
        /// </summary>
        public void ChangeGameState(GameState newState)
        {
            GameState oldState = CurrentGameState;

            // Полная очистка партии только при настоящем завершении игры
            // (не на паузу и не между уровнями — там игрок сохраняется).
            bool endGame =
                (oldState == GameState.Playing &&
                    (newState == GameState.MainMenu || newState == GameState.GameOver || newState == GameState.Victory)) ||
                (oldState == GameState.Paused && newState == GameState.MainMenu);
            if (endGame)
            {
                if (Player != null)
                {
                    _lastScore = Player.Score;                 // запоминаем счёт до очистки
                    Utils.SaveData.Currency += Player.Currency; // банкуем заработанную валюту
                }
                Utils.SaveData.ReportScore(_lastScore);        // обновить рекорд
                Utils.SaveData.Save();                         // сохранить профиль (рекорд/прогресс/валюта)
                CleanupGameplay();
            }

            CurrentGameState = newState;

            // Музыка по состоянию: меню-экраны — трек меню; бой — трек биома (босс переключает сам);
            // итоговые экраны — тишина (+джингл победы). Пауза/диалог/магазин трек не трогают.
            switch (newState)
            {
                case GameState.Splash:
                case GameState.Profile:
                case GameState.MainMenu:
                case GameState.DevMenu:
                    AudioManager.Instance.PlayMusicKey("menu");
                    break;
                case GameState.Playing:
                    // Не с паузы: пауза посреди босс-боя не должна сбрасывать босс-трек.
                    if (oldState != GameState.Paused)
                        AudioManager.Instance.PlayMusic(AudioManager.Instance.BattleTrack);
                    break;
                case GameState.GameOver:
                    AudioManager.Instance.StopMusic();
                    break;
                case GameState.Victory:
                    AudioManager.Instance.StopMusic();
                    AudioManager.Instance.Play("jingle.win");
                    break;
            }

            // Инициализация ресурсов при входе в состояние
            switch (newState)
            {
                case GameState.Splash:
                    Screens.Change(new Screens.SplashScreen());
                    break;
                case GameState.Profile:
                    Screens.Change(new Screens.ProfileScreen());
                    break;
                case GameState.DevMenu:
                    Screens.Change(new Screens.DevMenuScreen());
                    break;
                case GameState.MainMenu:
                    Screens.Change(new Screens.MainMenuScreen());
                    break;
                case GameState.Settings:
                    Screens.Change(new Screens.SettingsScreen());
                    break;
                case GameState.NextLevel:
                    // Убираем остатки прошлого уровня (пули/бонусы), игрок остаётся
                    ClearNonPlayerObjects();
                    Screens.Change(new Screens.NextLevelScreen());
                    break;
                case GameState.Playing:
                    // Бой загружается явно через поток миссии (EnterBattle); здесь только показываем
                    // игровой экран. Сюда приходим из паузы (продолжение), из брифинга/магазина
                    // (бой уже загружен EnterBattle) — переинициализация партии тут не нужна.
                    Screens.Change(new Screens.GameplayScreen());
                    break;
                case GameState.Paused:
                    Screens.Change(new Screens.PausedScreen());
                    break;
                case GameState.GameOver:
                    Screens.Change(new Screens.GameOverScreen());
                    break;
                case GameState.Victory:
                    Screens.Change(new Screens.VictoryScreen());
                    break;
                case GameState.Dialogue:
                    Screens.Change(new Screens.DialogueScreen());
                    break;
                case GameState.Shop:
                    Screens.Change(new Screens.ShopScreen());
                    break;
            }
        }

        /// <summary>
        /// Показать диалог по имени (Content/Dialogues/&lt;name&gt;.yaml), затем перейти в состояние next.
        /// Если диалога нет — сразу переходит в next (безопасный фолбэк).
        /// </summary>
        public void PlayDialogue(string name, GameState next)
        {
            var d = Utils.Dialogue.Load(name);
            if (d == null)
            {
                ChangeGameState(next);
                return;
            }
            _dialogue = d;
            _dialogueNext = next;
            _dialogueThen = null;
            ChangeGameState(GameState.Dialogue);
        }

        /// <summary>Показать диалог; по завершении вызвать продолжение (поток миссии). Нет диалога — сразу продолжение.</summary>
        public void PlayDialogueThen(string name, System.Action then)
        {
            var d = Utils.Dialogue.Load(name);
            if (d == null) { then?.Invoke(); return; }
            _dialogue = d;
            _dialogueThen = then;
            ChangeGameState(GameState.Dialogue);
        }

        /// <summary>Завершить диалог: либо продолжение миссии, либо переход в заранее заданное состояние.</summary>
        public void EndDialogue()
        {
            _dialogue = null;
            if (_dialogueThen != null)
            {
                var then = _dialogueThen;
                _dialogueThen = null;
                then();
            }
            else
            {
                ChangeGameState(_dialogueNext);
            }
        }

        // Куда вернуться из магазина (меню или экран между уровнями).
        private GameState _shopReturn = GameState.MainMenu;

        /// <summary>Открыть магазин; по выходу вернуться в returnTo.</summary>
        public void OpenShop(GameState returnTo)
        {
            _shopReturn = returnTo;
            _shopThen = null;
            ChangeGameState(GameState.Shop);
        }

        /// <summary>Открыть магазин (шаг миссии); по выходу вызвать продолжение.</summary>
        public void OpenShopThen(System.Action then)
        {
            _shopThen = then;
            ChangeGameState(GameState.Shop);
        }

        /// <summary>Закрыть магазин — продолжение миссии или возврат туда, откуда открыли.</summary>
        public void CloseShop()
        {
            if (_shopThen != null)
            {
                var then = _shopThen;
                _shopThen = null;
                then();
            }
            else
            {
                ChangeGameState(_shopReturn);
            }
        }

        public void UpdateGameplay(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Эффекты обновляем всегда.
            _shake.Update(deltaTime);
            Particles.Update(deltaTime);
            Effects.FloatingText.Update(deltaTime);

            // Поражение — игрок уничтожен.
            if (Player != null && Player.Health <= 0)
            {
                Utils.Log.Debug($"Game over: score={Player.Score}, level={_levels.CurrentLevel}");
                ChangeGameState(GameState.GameOver);
                return;
            }

            // Ввод и объекты работают всегда — в т.ч. в «окне сбора» после зачистки уровня
            // (чтобы магнит/корабль успели собрать оставшиеся звёзды).
            if (userInputHandler != null)
            {
                userInputHandler.Update();
                userInputHandler.HandleScGameInput();
            }
            _levels.Update(deltaTime);
            MessageLog.Update(deltaTime);
            Barks.Update(deltaTime);
            ProcessGameObjects(gameTime);

            // Зачистка боя и переход к следующему шагу миссии — ведёт CampaignFlow.
            _campaign.Update(deltaTime);
        }

        /// <summary>
        /// Оптимизированная обработка игровых объектов
        /// Аналог основного цикла из GamePlay.cs строка 112-145
        /// </summary>
        private void ProcessGameObjects(GameTime gameTime)
        {
            // 1. Обновляем все объекты (позиции/таймеры) — до проверки столкновений,
            //    чтобы столкновения считались по актуальным позициям.
            for (int i = 0; i < GameObjects.Count; i++)
                GameObjects[i]?.Update(gameTime);

            // 2. Столкновения (вынесено в CollisionSystem, O(n²/2)).
            _collisions.ResolveAll(GameObjects);

            // 3. Удаляем мёртвые объекты (с конца — безопасно при удалении по индексу).
            //    ProcessObjectRemoval может дозаписать бонусы в конец списка — они окажутся
            //    за пределами текущего i и в этой итерации не обрабатываются (это норм).
            for (int i = GameObjects.Count - 1; i >= 0; i--)
            {
                var obj = GameObjects[i];
                if (obj == null)
                {
                    GameObjects.RemoveAt(i);
                    continue;
                }
                // Игрока из списка не удаляем: его смерть обрабатывается через GameOver,
                // а объектом владеет GameManager (пересоздаётся в SetupNewPlayer).
                if (!obj.IsAlive && obj is not PlayerShip)
                {
                    ProcessObjectRemoval(obj);
                    GameObjects.RemoveAt(i);
                }
            }
        }

        #region Методы отрисовки для каждого состояния

        /// <summary>Найти живого босса на сцене (для шкалы HP); null — босса нет.</summary>
        private Enemy FindActiveBoss()
        {
            for (int i = 0; i < GameObjects.Count; i++)
                if (GameObjects[i] is Enemy e && e.IsAlive && e.IsBossType)
                    return e;
            return null;
        }

        public void DrawGameplay()
        {
            
            // Рисуем все игровые объекты
            foreach (var gameObject in GameObjects)
            {
                gameObject.Draw(_drawTime, _spriteBatch);
            }

            // Частицы (взрывы/искры) — поверх объектов, под HUD.
            if (SimpleTexture == null)
                SimpleTexture = Utils.Textures.CreateSolid(GraphicsDevice, Color.White);
            Particles.Draw(_spriteBatch, SimpleTexture);

            // Всплывающие числа (урон/очки) в координатах мира — поверх частиц, под HUD.
            Effects.FloatingText.Draw(_spriteBatch, _defaultFont);

            // Рисуем HUD
            _hud.Draw(_spriteBatch, _defaultFont, SimpleTexture, Player, ScreenWidth);

            // Шкала HP босса (если на сцене есть живой босс).
            var boss = FindActiveBoss();
            if (boss != null)
                _hud.DrawBossBar(_spriteBatch, _defaultFont, SimpleTexture, boss, CurrentLevelDescription, ScreenWidth);

            // Панель тестовых кнопок
            foreach (var btn in InputManager.Instance.GuiButtons)
                btn.Draw(_spriteBatch, SimpleTexture);

            // Всплывающие сообщения над кнопками
            MessageLog.Draw(_spriteBatch, _defaultFont, ScreenWidth, ScreenHeight);
        }

        // Оверлеи DrawPaused/DrawGameOver/DrawVictory вынесены в одноимённые Screen-классы.

        #endregion

        #region Вспомогательные методы

        // ── Кампания: тонкие фасады над CampaignFlow (call-sites в экранах/командах не меняются) ──
        public void StartCampaign() => _campaign.StartCampaign();
        public void ContinueCampaign() => _campaign.ContinueCampaign();
        public void RestartMission() => _campaign.RestartMission();
        public void DevStartMission(int missionIndex) => _campaign.DevStartMission(missionIndex);
        public void DevStartMissionAtBoss(int missionIndex) => _campaign.DevStartMissionAtBoss(missionIndex);

        /// <summary>Создать свежего игрока и очистить сцену (бой ещё не загружается).
        /// Зовёт CampaignFlow при старте/рестарте; игроком и сценой владеет GameManager.</summary>
        public void ResetPlayerAndScene()
        {
            GameObjects.Clear();

            Player = new PlayerShip(new Vector2(ScreenWidth / 2, ScreenHeight - 100));
            Player.SetGraphicsDevice(GraphicsDevice);
            Player.LoadContent(_content);            // спрайт корабля "Images/ship"
            Player.Health = Player.MaxHealth;
            Player.Score = 0;
            Player.Currency = 0;

            if (Player.Movement is PlayerMovementComponent playerMovement)
                playerMovement.SetBounds(0, ScreenWidth, 0, ScreenHeight);

            SubscribeToPlayerEvents();
            GameObjects.Add(Player);

            CreateDebugButtons();
            CreateSkillButtons();
        }

        /// <summary>Удалить все объекты кроме игрока (между боями).</summary>
        public void ClearNonPlayerObjects()
        {
            GameObjects.RemoveAll(o => !(o is PlayerShip));
        }

        // LoadLevel/SpawnEnemy/ParseRouteEnd вынесены в LevelDirector.

        /// <summary>Тестовый переход на следующий бой (кнопка/команда): засчитать текущий бой пройденным.</summary>
        public void DebugNextLevel()
        {
            if (CurrentGameState == GameState.Playing)
                _campaign.OnBattleCleared();
        }

        /// <summary>
        /// Создаёт панель кнопок боя внизу слева: смена оружия (живой UI, важен для тача) и —
        /// только в Debug-сборке — тестовые кнопки (лечение/убить всех/следующий уровень).
        /// </summary>
        private void CreateDebugButtons()
        {
            InputManager.Instance.GuiButtons.Clear();
            const int size = 50, gap = 6;
            int y = ScreenHeight - size - 8;
            int x = 10;
            // Кнопки смены оружия — по всем видам из реестра (иконки из WeaponDef.Icon).
            // Переключение сработает только на открытое оружие (иначе подсказка).
            foreach (var w in Weapons.WeaponConfig.All)
                AddDebugButton(new Interface.ButtonChWeapon(Vector2.Zero, w.Id), w.Icon, ref x, y, size, gap);

#if DEBUG
            // Тестовые кнопки — только в Debug-сборке (в релиз не попадают).
            AddDebugButton(new ButtonHpUp(Vector2.Zero), "Images/btn_hp_up", ref x, y, size, gap);
            AddDebugButton(new ButtonKillAll(Vector2.Zero), "Images/btn_killall", ref x, y, size, gap);
            AddDebugButton(new ButtonNextLevel(Vector2.Zero), "Images/btn_win", ref x, y, size, gap);
#endif
        }

        private void AddDebugButton(MyButton b, string sprite, ref int x, int y, int size, int gap)
        {
            b.Width = b.Height = size;
            b.Position = new Vector2(x + size / 2f, y + size / 2f); // GetRect центрирует по Position
            try { b.sprite = _content.Load<Texture2D>(sprite); }
            catch (Exception ex) { Utils.Log.Error($"=== Button sprite '{sprite}' load failed: {ex.Message} ==="); }
            InputManager.Instance.GuiButtons.Add(b);
            x += size + gap;
        }

        /// <summary>
        /// Кнопки активных навыков (тач) — справа внизу, иконки и кулдаун из SkillsConfig/PlayerShip.
        /// На десктопе навыки также активируются клавишами (InputManager).
        /// </summary>
        private void CreateSkillButtons()
        {
            const int size = 64, gap = 10;
            float cy = ScreenHeight - 8 - size / 2f;
            float cx = ScreenWidth - 8 - size / 2f; // первая кнопка у правого края, дальше влево
            foreach (var s in Utils.SkillsConfig.All)
            {
                var b = new Interface.ButtonSkill(new Vector2(cx, cy), s.Id) { Width = size, Height = size };
                try { b.sprite = _content.Load<Texture2D>(s.Icon); }
                catch (Exception ex) { Utils.Log.Error($"=== Skill icon '{s.Icon}' load failed: {ex.Message} ==="); }
                InputManager.Instance.GuiButtons.Add(b);
                cx -= size + gap;
            }
        }

        /// <summary>
        /// Подписка на события игрока
        /// </summary>
        private void SubscribeToPlayerEvents()
        {
            if (Player == null) return;
            
            // Подписываемся на события здоровья игрока
            Player.HealthChanged += OnPlayerHealthChanged;
            Player.PlayerDied += OnPlayerDied;
            Player.PlayerRespawned += OnPlayerRespawned;
            
        }
        
        /// <summary>
        /// Обработчик изменения здоровья игрока
        /// </summary>
        private void OnPlayerHealthChanged(int oldHealth, int newHealth)
        {
            // Визуальная обратная связь по урону: тряска экрана + искры у корабля.
            if (newHealth < oldHealth && Player != null)
            {
                Shake(Utils.EffectsConfig.PlayerHitShake.Magnitude, Utils.EffectsConfig.PlayerHitShake.Duration);
                Particles.HitSpark(Player.Position, new Color(255, 80, 80));
            }
        }
        
        /// <summary>
        /// Обработчик смерти игрока
        /// </summary>
        private void OnPlayerDied()
        {
            // НЕ меняем состояние здесь: смерть происходит внутри цикла обработки объектов
            // (ProcessCollision), а ChangeGameState→CleanupGameplay чистит список объектов и
            // привёл бы к падению. Переход в GameOver безопасно выполнит CheckGameEndConditions
            // в начале следующего кадра (Player.Health <= 0).
        }
        
        /// <summary>
        /// Обработчик воскрешения игрока
        /// </summary>
        private void OnPlayerRespawned()
        {
            
            // Здесь можно добавить дополнительную логику при воскрешении
            // Например: сброс бонусов, перезапуск уровня и т.д.
        }

        /// <summary>
        /// Обработка удаления объекта (аналог GamePlay.cs строка 126-141)
        /// Выполняет дополнительные действия при удалении врагов
        /// </summary>
        private void ProcessObjectRemoval(GameObject obj)
        {
            if (obj == null) return;
            
            
            // Для врагов выполняем дополнительные действия (аналог GamePlay.cs)
            if (obj is Enemy enemy)
            {
                // Визуальный взрыв + тряска экрана (босс — заметно мощнее). Параметры — из effects.yaml.
                bool isBoss = enemy.IsBossType;
                Particles.Explosion(obj.Position, enemy.ExplosionColor,
                    isBoss ? Utils.EffectsConfig.BossExplosion : Utils.EffectsConfig.EnemyExplosion);
                var deathShake = isBoss ? Utils.EffectsConfig.BossDeathShake : Utils.EffectsConfig.EnemyDeathShake;
                Shake(deathShake.Magnitude, deathShake.Duration);

                if (isBoss) ShowBossTaunt("defeat"); // предсмертная реплика босса

                // Очки за убийство (идут в рекорд). Кредиты игрок получит, собрав звезду.
                if (Player != null)
                    Player.Score += enemy.Reward;

                if (!isBoss) Barks.RegisterKill(); // килстрик — только по рядовым врагам

                // Запускаем ивент смерти врага
                TriggerEnemyDeathEvent(obj);

                // Выпадение бонусов: звёзды (кредиты) — со всех; авторский бонус — из YAML уровня.
                BonusSpawner.SpawnStars(obj.Position, enemy.Reward, GameObjects);
                BonusSpawner.SpawnAuthoredDrop(enemy, GameObjects);
            }

            // Выполняем базовое удаление объекта
            obj.IsAlive = false; // Помечаем объект как мертвый
        }

        // Спавн бонусов при смерти врага вынесен в BonusSpawner (звёзды + авторские дропы).

        private void CleanupGameplay()
        {
            // Отписываемся от событий игрока
            UnsubscribeFromPlayerEvents();
            
            // Очистка ресурсов игрового процесса
            GameObjects.Clear();
            Particles.Clear();
            Effects.FloatingText.Clear();
            InputManager.Instance.GuiButtons.Clear(); // убрать тестовые кнопки
            MessageLog.Clear();
            Player = null;
        }
        
        /// <summary>
        /// Отписка от событий игрока
        /// </summary>
        private void UnsubscribeFromPlayerEvents()
        {
            if (Player == null) return;
            
            Player.HealthChanged -= OnPlayerHealthChanged;
            Player.PlayerDied -= OnPlayerDied;
            Player.PlayerRespawned -= OnPlayerRespawned;
            
        }

        /// <summary>Уничтожить всех врагов на экране (бонус NukeBomb). Делегирует в CollisionSystem.</summary>
        public void KillAllEnemies()
        {
            AudioManager.Instance.Play("nuke");
            _collisions.KillAllEnemies(GameObjects);
        }

        // ProcessCollision/ShellHitsEnemy/EnemyHitsPlayer/ShellHitsPlayer вынесены в CollisionSystem.
        // DrawHUD/DrawHealthBar вынесены в Interface.HudRenderer.
        // CreateSimpleTexture вынесен в Utils.Textures.CreateSolid (был продублирован).

        private void TriggerEnemyDeathEvent(GameObject enemy)
        {
            // Обновляем счётчики врагов уровня (в LevelDirector).
            _levels.OnEnemyKilled();
        }

        #endregion
    }
}
