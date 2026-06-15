using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiotGalaxy.Core.Managers;
using RiotGalaxy.Core.Utils;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Экран между уровнями: номер/описание уровня, очки, накопленные кредиты.
    /// Действия: «Магазин» (потратить кредиты), «Продолжить» (в бой), «В главное меню» (выход из забега).
    /// Управление: мышь/тач (наведение+клик) и клавиатура (↑/↓ — выбор, Enter — подтвердить).
    /// </summary>
    public class NextLevelScreen : Screen
    {
        private const int Shop = 0, Cont = 1, Menu = 2;
        private static readonly string[] ItemKeys = { "nextlevel.shop", "nextlevel.continue", "nextlevel.menu" };
        private int _selected = Cont; // по умолчанию — «Продолжить»

        private float ItemY(int i) => ScreenH * 0.60f + i * ScreenH * 0.10f;
        private Rectangle ItemRect(int i) => CenteredItemRect(Loc.T(ItemKeys[i]), ItemY(i), ItemScale);

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Наведение мышью/тачем.
            for (int i = 0; i < ItemKeys.Length; i++)
                if (ItemRect(i).Contains(MousePoint)) _selected = i;

            if (KeyPressed(Keys.Down) || KeyPressed(Keys.S)) _selected = (_selected + 1) % ItemKeys.Length;
            if (KeyPressed(Keys.Up) || KeyPressed(Keys.W)) _selected = (_selected - 1 + ItemKeys.Length) % ItemKeys.Length;

            if (MouseClicked())
                for (int i = 0; i < ItemKeys.Length; i++)
                    if (ItemRect(i).Contains(MousePoint)) { Activate(i); return; }

            if (KeyPressed(Keys.Enter) || KeyPressed(Keys.Space)) Activate(_selected);
        }

        private void Activate(int index)
        {
            switch (index)
            {
                case Shop: GameManager.Instance.OpenShop(GameManager.GameState.NextLevel); break;
                case Cont: GameManager.Instance.ChangeGameState(GameManager.GameState.Playing); break;
                case Menu: GameManager.Instance.ChangeGameState(GameManager.GameState.MainMenu); break;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var gm = GameManager.Instance;

            DrawDimmer(spriteBatch);
            DrawPanel(spriteBatch, PanelRect(0.7f, 0.10f, 0.95f));

            DrawCentered(spriteBatch, Loc.F("nextlevel.title", gm.CurrentLevel, gm.TotalLevels), ScreenH * 0.14f, Color.Orange, TitleScale);

            if (!string.IsNullOrWhiteSpace(gm.CurrentLevelDescription))
                DrawCentered(spriteBatch, gm.CurrentLevelDescription, ScreenH * 0.30f, Color.White, ItemScale);

            if (gm.Player != null)
                DrawCentered(spriteBatch, Loc.F("nextlevel.score", gm.Player.Score), ScreenH * 0.40f, Color.Yellow, ItemScale);

            DrawCentered(spriteBatch, Loc.F("nextlevel.credits", SaveData.Currency), ScreenH * 0.48f, Color.Gold, ItemScale);

            for (int i = 0; i < ItemKeys.Length; i++)
                DrawMenuItem(spriteBatch, Loc.T(ItemKeys[i]), ItemY(i), i == _selected);

            DrawCentered(spriteBatch, Loc.T("nextlevel.hint"), ScreenH * 0.93f, Color.Gray, HintScale);
        }
    }
}
