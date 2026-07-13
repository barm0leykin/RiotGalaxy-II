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
            UpdateListNav(ItemKeys.Length, ref _selected, ItemRect, activate: Activate);
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
            var sb = spriteBatch;
            var gm = GameManager.Instance;
            var p = PanelRect(0.7f, 0.10f, 0.95f);
            string title = Loc.F("nextlevel.title", gm.CurrentLevel, gm.TotalLevels);
            float titleY = ScreenH * 0.135f;

            DrawDimmer(sb, 178);
            DrawNeonPanel(sb, p, NeonCyan);

            GlowPass(sb, () =>
            {
                NeonPanelGlow(sb, p, NeonCyan);
                GlowTextCentered(sb, title, titleY, NeonMag, TitleScale);
                SelectionBarGlow(sb, ListItemRect(p, Loc.T(ItemKeys[_selected]), ItemY(_selected), ItemScale), NeonCyan);
            });

            DrawCentered(sb, title, titleY, Color.White, TitleScale);

            if (!string.IsNullOrWhiteSpace(gm.CurrentLevelDescription))
                DrawCentered(sb, gm.CurrentLevelDescription, ScreenH * 0.30f, Color.White, ItemScale);

            if (gm.Player != null)
                DrawCentered(sb, Loc.F("nextlevel.score", gm.Player.Score), ScreenH * 0.40f, NeonCyan, ItemScale);

            DrawCentered(sb, Loc.F("nextlevel.credits", SaveData.Currency), ScreenH * 0.48f, NeonGold, ItemScale);

            for (int i = 0; i < ItemKeys.Length; i++)
                DrawListItem(sb, p, Loc.T(ItemKeys[i]), ItemY(i), i == _selected, NeonCyan);

            DrawCentered(sb, Loc.T("nextlevel.hint"), ScreenH * 0.93f, Scale(NeonDim, 0.8f), HintScale);
        }
    }
}
