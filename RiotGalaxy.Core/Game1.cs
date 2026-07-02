using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core
{
    /// <summary>
    /// Это главный класс игры для RiotGalaxy на MonoGame.
    /// Наследуется от Microsoft.Xna.Framework.Game
    /// </summary>
    public class Game1 : Game
    {
        private static Game1 _instance;
        public static Game1 Instance
        {
            get
            {
                if (_instance == null)
                    _instance = new Game1();
                return _instance;
            }
        }

        private GraphicsDeviceManager _graphics;
        private GameManager _gameManager;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            // Создаем экземпляр GameManager
            _gameManager = GameManager.Instance;
            
            // Инициализируем GameManager в конструкторе
            _gameManager.Initialize(this, _graphics, Content);
        }

        protected override void Initialize()
        {
            // Дополнительная инициализация, если нужна
            
            base.Initialize();
        }

        protected override void LoadContent()
        {
            // Загружаем контент через GameManager
            _gameManager.LoadContent();
            
            // Создаем простую текстуру для заглушек
            _gameManager.SimpleTexture = RiotGalaxy.Core.Utils.Textures.CreateSolid(GraphicsDevice, Color.White);
        }

        // Скриншот по F12: сохраняет текущий кадр в screenshot.png рядом с бинарником
        // (для тестов/ревью — файл затем можно прочитать/переслать).
        private bool _prevF12;
        private bool _screenshotRequested;

        protected override void Update(GameTime gameTime)
        {
            // Ввод игровых состояний (меню/настройки обрабатывают сами экраны)
            HandleGameplayKeys();

            // F12 — запрос скриншота (edge-детект, работает во всех состояниях).
            bool f12 = Keyboard.GetState().IsKeyDown(Keys.F12);
            if (f12 && !_prevF12) _screenshotRequested = true;
            _prevF12 = f12;

            // Передаем обновление в GameManager
            _gameManager.Update(gameTime);

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            // Передаем отрисовку в GameManager
            _gameManager.Draw(gameTime);

            base.Draw(gameTime);

            if (_screenshotRequested)
            {
                _screenshotRequested = false;
                SaveScreenshot();
            }
        }

        /// <summary>Сохранить текущий кадр (back buffer) в screenshot.png рядом с бинарником.</summary>
        private void SaveScreenshot()
        {
            try
            {
                int w = GraphicsDevice.PresentationParameters.BackBufferWidth;
                int h = GraphicsDevice.PresentationParameters.BackBufferHeight;
                var data = new Color[w * h];
                GraphicsDevice.GetBackBufferData(data);
                using var tex = new Texture2D(GraphicsDevice, w, h);
                tex.SetData(data);
                string path = System.IO.Path.Combine(AppContext.BaseDirectory, "screenshot.png");
                using var fs = System.IO.File.Create(path);
                tex.SaveAsPng(fs, w, h);
                RiotGalaxy.Core.Utils.Log.Debug($"Screenshot saved: {path} ({w}x{h})");
            }
            catch (Exception ex)
            {
                RiotGalaxy.Core.Utils.Log.Debug($"Screenshot failed: {ex.Message}");
            }
        }
        
        /// <summary>
        /// Обработка ввода с клавиатуры
        /// </summary>
        private void HandleGameplayKeys()
        {
            var keyboardState = Keyboard.GetState();

            // Кнопка «Назад» на Android: MainActivity.OnBackPressed (UI-поток) выставляет флаг,
            // а смену состояния делаем здесь, в игровом потоке (без гонки с циклом Update).
            _gameManager.ProcessPendingBack();

            switch (_gameManager.CurrentGameState)
            {
                case GameManager.GameState.Playing:
                    // В игре - Esc (или P) ставит на паузу
                    if ((keyboardState.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape)) ||
                        (keyboardState.IsKeyDown(Keys.P) && !_previousKeyboardState.IsKeyDown(Keys.P)))
                    {
                        _gameManager.ChangeGameState(GameManager.GameState.Paused);
                    }
                    break;

                case GameManager.GameState.Paused:
                    // В паузе - Esc (или P) продолжает игру, Q - выход в главное меню
                    if ((keyboardState.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape)) ||
                        (keyboardState.IsKeyDown(Keys.P) && !_previousKeyboardState.IsKeyDown(Keys.P)))
                    {
                        _gameManager.ChangeGameState(GameManager.GameState.Playing);
                    }
                    else if (keyboardState.IsKeyDown(Keys.Q) && !_previousKeyboardState.IsKeyDown(Keys.Q))
                    {
                        _gameManager.ChangeGameState(GameManager.GameState.MainMenu);
                    }
                    break;
                    
                case GameManager.GameState.GameOver:
                    // В экране Game Over - Пробел для рестарта текущей миссии, ESC для выхода в меню
                    if (keyboardState.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space))
                    {
                        _gameManager.RestartMission();
                    }
                    else if (keyboardState.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape))
                    {
                        _gameManager.ChangeGameState(GameManager.GameState.MainMenu);
                    }
                    break;

                case GameManager.GameState.Victory:
                    // На экране победы - Space перезапуск кампании, Esc в меню
                    if (keyboardState.IsKeyDown(Keys.Space) && !_previousKeyboardState.IsKeyDown(Keys.Space))
                    {
                        _gameManager.StartCampaign();
                    }
                    else if (keyboardState.IsKeyDown(Keys.Escape) && !_previousKeyboardState.IsKeyDown(Keys.Escape))
                    {
                        _gameManager.ChangeGameState(GameManager.GameState.MainMenu);
                    }
                    break;
            }

            // Сохраняем предыдущее состояние клавиатуры
            _previousKeyboardState = keyboardState;
        }
        
        // Храним предыдущее состояние клавиатуры для детектирования нажатий
        private KeyboardState _previousKeyboardState;
    }
}
