using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiotGalaxy.Core;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Главное меню: заголовок + кнопки «Начать игру / Настройки / Выход».
    /// Управление мышью (наведение + клик) и клавиатурой (стрелки + Enter, Space/Esc).
    /// </summary>
    public class MainMenuScreen : Screen
    {
        private static readonly string[] Items = { "Начать игру", "Настройки", "Выход" };
        private const float SpacingY = 100f; // крупные пункты + запас под палец
        private int _selected;
        private int _hover = -1;

        private float StartY => ScreenH * 0.42f;

        private Rectangle ItemRect(int i) => CenteredItemRect(Items[i], StartY + i * SpacingY, ItemScale);

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Наведение мышью
            _hover = -1;
            for (int i = 0; i < Items.Length; i++)
            {
                if (ItemRect(i).Contains(MousePoint))
                {
                    _hover = i;
                    _selected = i;
                    break;
                }
            }

            // Навигация клавиатурой
            if (KeyPressed(Keys.Down) || KeyPressed(Keys.S))
                _selected = (_selected + 1) % Items.Length;
            if (KeyPressed(Keys.Up) || KeyPressed(Keys.W))
                _selected = (_selected - 1 + Items.Length) % Items.Length;

            // Активация: клик по пункту под мышью, Enter по выбранному
            if (_hover >= 0 && MouseClicked())
                Activate(_hover);
            else if (KeyPressed(Keys.Enter))
                Activate(_selected);

            // Горячие клавиши
            if (KeyPressed(Keys.Space))
                Activate(0); // начать игру
            if (KeyPressed(Keys.Escape))
                Activate(2); // выход
        }

        private void Activate(int index)
        {
            switch (index)
            {
                case 0:
                    GameManager.Instance.ChangeGameState(GameManager.GameState.Playing);
                    break;
                case 1:
                    GameManager.Instance.ChangeGameState(GameManager.GameState.Settings);
                    break;
                case 2:
                    Game1.Instance.Exit();
                    break;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawCentered(spriteBatch, "-= Galaxy Riot! =-", ScreenH * 0.20f, Color.Orange, TitleScale);

            for (int i = 0; i < Items.Length; i++)
            {
                bool active = (i == _hover) || (i == _selected);
                Color color = active ? Color.Yellow : Color.White;
                DrawCentered(spriteBatch, Items[i], StartY + i * SpacingY, color, ItemScale);
            }

            DrawCentered(spriteBatch, "Тап по пункту · стрелки + Enter · Esc — выход",
                ScreenH * 0.9f, Color.Gray, HintScale);
        }
    }
}
