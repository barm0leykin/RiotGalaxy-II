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
        // Пункты берутся из локализации (Loc загружается до показа меню).
        private static string[] Items => new[]
        {
            Utils.Loc.T("menu.start"), Utils.Loc.T("menu.shop"), Utils.Loc.T("menu.settings"), Utils.Loc.T("menu.exit")
        };
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
                Activate(Items.Length - 1); // выход (последний пункт)
        }

        private void Activate(int index)
        {
            switch (index)
            {
                case 0:
                    // Начать кампанию: миссия 1 ведёт через брифинги/бои (см. MissionDirector).
                    GameManager.Instance.StartCampaign();
                    break;
                case 1:
                    GameManager.Instance.OpenShop(GameManager.GameState.MainMenu);
                    break;
                case 2:
                    GameManager.Instance.ChangeGameState(GameManager.GameState.Settings);
                    break;
                case 3:
                    Game1.Instance.Exit();
                    break;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawDimmer(spriteBatch);
            DrawPanel(spriteBatch, PanelRect(0.62f, 0.12f, 0.95f));

            DrawCentered(spriteBatch, Utils.Loc.T("menu.title"), ScreenH * 0.18f, Color.Orange, TitleScale);

            if (Utils.SaveData.HighScore > 0)
                DrawCentered(spriteBatch, Utils.Loc.F("menu.record", Utils.SaveData.HighScore), ScreenH * 0.29f, Color.LightGray, HintScale);
            if (Utils.SaveData.Currency > 0)
                DrawCentered(spriteBatch, Utils.Loc.F("menu.credits", Utils.SaveData.Currency), ScreenH * 0.34f, Color.Gold, HintScale);

            for (int i = 0; i < Items.Length; i++)
            {
                bool active = (i == _hover) || (i == _selected);
                DrawMenuItem(spriteBatch, Items[i], StartY + i * SpacingY, active);
            }

            DrawCentered(spriteBatch, Utils.Loc.T("menu.hint"), ScreenH * 0.9f, Color.Gray, HintScale);
        }
    }
}
