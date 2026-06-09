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
        public enum GameState { Splash, MainMenu, Settings, Playing, Paused, GameOver, Victory, NextLevel }
        public GameState CurrentGameState { get; private set; }

        // Оркестратор уровней (World/Hive, спавн, прогрессия, счётчики врагов).
        private readonly LevelDirector _levels = new LevelDirector();

        private int _lastScore; // итоговый счёт для экранов GameOver/Victory
        public int LastScore => _lastScore;

        // Время текущего кадра (для DrawGameplay, вызываемого из GameplayScreen.Draw).
        private GameTime _drawTime = new GameTime();

        // Счётчики и прогрессия — делегируются в LevelDirector (публичный API сохранён).
        public int EnemiesKilled => _levels.EnemiesKilled;
        public int EnemiesRemaining => _levels.EnemiesRemaining;
        public int CurrentLevel => _levels.CurrentLevel;
        public int TotalLevels => _levels.TotalLevels;
        public string CurrentLevelDescription => _levels.CurrentLevelDescription;

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

        // Screenshake: тряска виртуального кадра при взрывах/боссе/нюке/уроне.
        private Vector2 _shakeOffset = Vector2.Zero;
        private float _shakeTime = 0f;
        private float _shakeDuration = 0f;
        private float _shakeMagnitude = 0f;
        private readonly Random _shakeRng = new Random();

        // Система частиц (взрывы, искры). Обновляется в UpdateGameplay, рисуется в DrawGameplay.
        public Effects.ParticleSystem Particles { get; } = new Effects.ParticleSystem();

        // Параллакс-фон (звёзды). Анимируется во всех состояниях, рисуется под сценой.
        private Effects.StarField _starField;

        // Отрисовщик боевого HUD (вынесен из GameManager).
        private readonly Interface.HudRenderer _hud = new Interface.HudRenderer();

        // Обработка столкновений (вынесена из GameManager).
        private readonly CollisionSystem _collisions = new CollisionSystem();


        // Вспомогательные текстуры
        public Texture2D SimpleTexture { get; set; }
        public GraphicsDevice GraphicsDevice => _graphics.GraphicsDevice;

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
                                _renderOffset.X + _shakeOffset.X * scale,
                                _renderOffset.Y + _shakeOffset.Y * scale, 0f);
        }

        /// <summary>
        /// Запустить тряску экрана. Слабая тряска не перебивает более сильную активную.
        /// </summary>
        /// <param name="magnitude">амплитуда в виртуальных пикселях</param>
        /// <param name="duration">длительность, сек</param>
        public void Shake(float magnitude, float duration = 0.3f)
        {
            if (magnitude <= 0f) return;
            if (_shakeTime <= 0f || magnitude >= _shakeMagnitude)
            {
                _shakeMagnitude = magnitude;
                _shakeDuration = duration;
                _shakeTime = duration;
            }
        }

        /// <summary>Затухание тряски и пересчёт случайного смещения кадра.</summary>
        private void UpdateScreenShake(float dt)
        {
            if (_shakeTime <= 0f)
            {
                _shakeOffset = Vector2.Zero;
                return;
            }
            _shakeTime -= dt;
            float k = Math.Max(0f, _shakeTime / _shakeDuration); // 1 → 0, линейное затухание
            float mag = _shakeMagnitude * k;
            _shakeOffset = new Vector2(
                (float)(_shakeRng.NextDouble() * 2.0 - 1.0) * mag,
                (float)(_shakeRng.NextDouble() * 2.0 - 1.0) * mag);
        }

        /// <summary>Переводит координаты экрана (пиксели мыши/тача) в виртуальные (1280x768).</summary>
        public Vector2 ScreenToVirtual(Vector2 screenPoint) =>
            (screenPoint - _renderOffset) / _renderScale;

        // Доступ к загрузчику контента (нужен игровым объектам для загрузки спрайтов)
        public ContentManager Content => _content;

        // Фоновое изображение (задник)
        private Texture2D _background;
        
        // Обработчик ввода пользователя
        public InputManager userInputHandler;

        // Приватный конструктор для singleton
        private GameManager()
        {
            CurrentGameState = GameState.MainMenu;
            GameObjects = new List<GameObject>();
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

            // Загружаем фоновое изображение (1280x768, точно под разрешение игры)
            try
            {
                _background = _content.Load<Texture2D>("Backgrounds/background_blue");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Failed to load background: {ex.Message} ===");
            }

            // Загружаем звуковые эффекты (fire1, explode1)
            AudioManager.Instance.LoadContent(_content);

            // Загружаем шрифт для текста (меню, HUD)
            try
            {
                _defaultFont = _content.Load<SpriteFont>("TestFont");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"=== Failed to load font 'TestFont': {ex.Message} ===");
            }

            // Загружаем конфиги из YAML (оружие, враги, параметры игры) и сохранённые настройки
            Weapons.WeaponConfig.Load();
            Utils.EnemyConfig.Load();
            Utils.BonusConfig.Load();
            Utils.GameOptions.Load();
            Utils.EffectsConfig.Load();
            Utils.GameSettings.Load();

            // Параллакс-фон из звёзд (процедурный, без ассетов). Слои — из EffectsConfig,
            // поэтому создаём после загрузки конфигов.
            _starField = new Effects.StarField(ScreenWidth, ScreenHeight);

            // Сколько уровней доступно (по файлам Content/Levels/level*.yaml)
            _levels.InitTotalLevels();
            Utils.Log.Debug($"Configs loaded. Levels found: {_levels.TotalLevels}, player HP: {Utils.GameOptions.PlayerMaxHp}");

            ChangeGameState(GameState.Splash);
        }


        /// <summary>
        /// Основной игровой цикл - обновление состояния игры
        /// Адаптировано из GamePlay.cs (CocosSharp)
        /// </summary>
        public void Update(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Параллакс-фон анимируется во всех состояниях (живой фон в меню и в бою).
            _starField?.Update(deltaTime);

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
            _graphics.GraphicsDevice.Clear(Color.Black);

            // Letterbox: пересчитываем матрицу под текущий back buffer и масштабируем всю сцену.
            UpdateRenderTransform();
            _spriteBatch.Begin(SpriteSortMode.Deferred, null, null, null, null, null, _renderMatrix);

            // Рисуем фоновое изображение (задник) под всеми состояниями
            if (_background != null)
            {
                _spriteBatch.Draw(_background, new Rectangle(0, 0, ScreenWidth, ScreenHeight), Color.White);
            }

            // Параллакс-звёзды поверх задника, под игровой сценой/UI.
            if (SimpleTexture == null)
                SimpleTexture = Utils.Textures.CreateSolid(GraphicsDevice, Color.White);
            _starField?.Draw(_spriteBatch, SimpleTexture);

            // Все состояния рисует ScreenSystem (включая GameplayScreen → DrawGameplay).
            Screens.Draw(_spriteBatch);

            _spriteBatch.End();
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
                if (Player != null) _lastScore = Player.Score; // запоминаем счёт до очистки
                CleanupGameplay();
            }

            CurrentGameState = newState;

            // Инициализация ресурсов при входе в состояние
            switch (newState)
            {
                case GameState.Splash:
                    Screens.Change(new Screens.SplashScreen());
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
                    // Новая партия — только если пришли из меню/конца игры.
                    // Из паузы — продолжение; из NextLevel — уровень уже загружен.
                    if (oldState != GameState.Paused && oldState != GameState.NextLevel)
                        InitializeGameplay();
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
            }
        }

        public void UpdateGameplay(GameTime gameTime)
        {
            float deltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;

            // Эффекты обновляем всегда (продолжают доигрывать даже на кадре завершения уровня).
            UpdateScreenShake(deltaTime);
            Particles.Update(deltaTime);

            // Проверка условий завершения игры (аналог GamePlay.cs)
            if (!CheckGameEndConditions())
            {
                // Обрабатываем пользовательский ввод (аналог SceneGame.Activity)
                if (userInputHandler != null)
                {
                    userInputHandler.Update();
                    userInputHandler.HandleScGameInput();
                }
                
                // Барражирование улья + спавн врагов по таймлайну (вынесено в LevelDirector)
                _levels.Update(deltaTime);

                // Всплывающие сообщения
                MessageLog.Update(deltaTime);

                // Аналог основного цикла из GamePlay.cs - обрабатываем все объекты
                // (удаление мёртвых объектов встроено в ProcessGameObjects)
                ProcessGameObjects(gameTime);
            }
        }

        /// <summary>
        /// Проверка условий завершения игры (победа или поражение)
        /// Аналог проверок в GamePlay.cs строка 94-109
        /// </summary>
        private bool CheckGameEndConditions()
        {
            // Проверка поражения - игрок уничтожен
            if (Player != null && Player.Health <= 0)
            {
                Utils.Log.Debug($"Game over: score={Player.Score}, level={_levels.CurrentLevel}");
                ChangeGameState(GameState.GameOver);
                return true;
            }

            // Уровень пройден: все враги уровня заспавнены и уничтожены
            if (_levels.LevelComplete)
            {
                AdvanceLevel();
                return true;
            }

            return false;
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
                if (!obj.IsAlive)
                {
                    ProcessObjectRemoval(obj);
                    GameObjects.RemoveAt(i);
                }
            }
        }

        #region Методы отрисовки для каждого состояния

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

            // Рисуем HUD
            _hud.Draw(_spriteBatch, _defaultFont, SimpleTexture, Player, ScreenWidth);

            // Панель тестовых кнопок
            foreach (var btn in InputManager.Instance.GuiButtons)
                btn.Draw(_spriteBatch, SimpleTexture);

            // Всплывающие сообщения над кнопками
            MessageLog.Draw(_spriteBatch, _defaultFont, ScreenWidth, ScreenHeight);
        }

        // Оверлеи DrawPaused/DrawGameOver/DrawVictory вынесены в одноимённые Screen-классы.

        #endregion

        #region Вспомогательные методы

        private void InitializeGameplay()
        {
            try
            {
                // Новая игра — с первого уровня
                _levels.ResetToFirst();

                // Инициализация игрового процесса
                GameObjects.Clear();
                
                // Создаем игрока (аналог GamePlay.cs строка 49-58)
                Player = new PlayerShip(new Vector2(ScreenWidth / 2, ScreenHeight - 100));
                Player.SetGraphicsDevice(GraphicsDevice);
                Player.LoadContent(_content); // Загружаем реальный спрайт корабля "Images/ship"
                Player.Health = Player.MaxHealth; // Сбрасываем здоровье игрока до максимума
                Player.Score = 0;
                
                // Устанавливаем границы движения для компонента движения игрока
                if (Player.Movement is PlayerMovementComponent playerMovement)
                {
                    playerMovement.SetBounds(0, ScreenWidth, 0, ScreenHeight);
                }
                
                // Подписываемся на события игрока
                SubscribeToPlayerEvents();
                
                GameObjects.Add(Player);

                // Загружаем первый уровень (враги спавнятся по таймлайну в UpdateGameplay)
                _levels.Load(_levels.CurrentLevel, ScreenWidth, ScreenHeight);

                // Панель тестовых кнопок
                CreateDebugButtons();

            }
            catch (Exception ex)
            {
Console.WriteLine($"Error initializing gameplay: {ex.Message}");
            }
        }

        /// <summary>Удалить все объекты кроме игрока (между уровнями).</summary>
        private void ClearNonPlayerObjects()
        {
            GameObjects.RemoveAll(o => !(o is PlayerShip));
        }

        /// <summary>Перейти к следующему уровню или к финальной победе.</summary>
        private void AdvanceLevel()
        {
            if (!_levels.HasNextLevel)
            {
                ChangeGameState(GameState.Victory); // все уровни пройдены
            }
            else
            {
                _levels.GoToNextLevel(ScreenWidth, ScreenHeight); // подготовить следующий уровень
                ChangeGameState(GameState.NextLevel);             // показать экран между уровнями
            }
        }

        // LoadLevel/SpawnEnemy/ParseRouteEnd вынесены в LevelDirector.

        /// <summary>Тестовый переход на следующий уровень (кнопка/команда).</summary>
        public void DebugNextLevel()
        {
            if (CurrentGameState == GameState.Playing)
                AdvanceLevel();
        }

        /// <summary>
        /// Создаёт панель тестовых кнопок (смена оружия, апгрейд, лечение, убить всех,
        /// следующий уровень) и регистрирует их в InputManager. Внизу слева.
        /// </summary>
        private void CreateDebugButtons()
        {
            InputManager.Instance.GuiButtons.Clear();
            const int size = 50, gap = 6;
            int y = ScreenHeight - size - 8;
            int x = 10;
            AddDebugButton(new ButtonCannon(Vector2.Zero), "Images/btn_cannon", ref x, y, size, gap);
            AddDebugButton(new ButtonMinigun(Vector2.Zero), "Images/btn_minigun", ref x, y, size, gap);
            AddDebugButton(new ButtonLaser(Vector2.Zero), "Images/btn_laser", ref x, y, size, gap);
            AddDebugButton(new ButtonUpgradeGun(Vector2.Zero), "Images/btn_BulletUp", ref x, y, size, gap);
            AddDebugButton(new ButtonHpUp(Vector2.Zero), "Images/btn_hp_up", ref x, y, size, gap);
            AddDebugButton(new ButtonKillAll(Vector2.Zero), "Images/btn_killall", ref x, y, size, gap);
            AddDebugButton(new ButtonNextLevel(Vector2.Zero), "Images/btn_win", ref x, y, size, gap);
        }

        private void AddDebugButton(MyButton b, string sprite, ref int x, int y, int size, int gap)
        {
            b.Width = b.Height = size;
            b.Position = new Vector2(x + size / 2f, y + size / 2f); // GetRect центрирует по Position
            try { b.sprite = _content.Load<Texture2D>(sprite); }
            catch (Exception ex) { Console.WriteLine($"=== Button sprite '{sprite}' load failed: {ex.Message} ==="); }
            InputManager.Instance.GuiButtons.Add(b);
            x += size + gap;
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
                bool isBoss = enemy.Type == EnemyType.BOSS;
                Particles.Explosion(obj.Position, enemy.ExplosionColor,
                    isBoss ? Utils.EffectsConfig.BossExplosion : Utils.EffectsConfig.EnemyExplosion);
                var deathShake = isBoss ? Utils.EffectsConfig.BossDeathShake : Utils.EffectsConfig.EnemyDeathShake;
                Shake(deathShake.Magnitude, deathShake.Duration);

                // Запускаем ивент смерти врага
                TriggerEnemyDeathEvent(obj);

                // Выпадение бонусов из убитого врага
                SpawnBonusOnEnemyDeath(obj.Position);
            }

            // Выполняем базовое удаление объекта
            obj.IsAlive = false; // Помечаем объект как мертвый
        }

        private static readonly Random _bonusRnd = new Random();

        /// <summary>
        /// Выпадение бонусов из убитого врага: всегда звезда (очки) + иногда усиление.
        /// Аналог CommandStarBonus + CommandSpawnRandomBonus из CocosSharp.
        /// </summary>
        private void SpawnBonusOnEnemyDeath(Vector2 pos)
        {
            // Звезда выпадает всегда
            GameObjects.Add(new BonusStar(pos));

            // С шансом 30% — случайное усиление
            int roll = _bonusRnd.Next(100);
            if (roll < 30)
            {
                int kind = _bonusRnd.Next(100);
                BonusType bt;
                if (kind < 45)
                    bt = BonusType.HP_UP;      // 45%
                else if (kind < 90)
                    bt = BonusType.BULLET_UP;  // 45%
                else
                    bt = BonusType.NUKE_BOMB;  // 10%
                GameObjects.Add(new Bonus(bt, pos));
            }
        }

        private void CleanupGameplay()
        {
            // Отписываемся от событий игрока
            UnsubscribeFromPlayerEvents();
            
            // Очистка ресурсов игрового процесса
            GameObjects.Clear();
            Particles.Clear();
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
        public void KillAllEnemies() => _collisions.KillAllEnemies(GameObjects);

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
