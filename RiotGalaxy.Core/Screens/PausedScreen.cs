using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Пауза: под полупрозрачным затемнением видна замороженная игра.
    /// Игра не обновляется (Update пуст). Выход из паузы (Esc/P/Q) — Game1.HandleGameplayKeys.
    /// </summary>
    public class PausedScreen : Screen
    {
        public override void Update(GameTime gameTime) { /* игра на паузе — не обновляем */ }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var gm = GameManager.Instance;

            var sb = spriteBatch;

            // Замороженная игра под паузой
            gm.DrawGameplay();

            // Полупрозрачное затемнение
            if (gm.SimpleTexture != null)
                sb.Draw(gm.SimpleTexture, new Rectangle(0, 0, ScreenW, ScreenH), new Color(0, 0, 0, 150));

            // Компактная неон-панель по центру
            var p = PanelRect(0.44f, 0.30f, 0.68f);
            string title = Utils.Loc.T("paused.title");
            float titleY = ScreenH * 0.345f;
            DrawNeonPanel(sb, p, NeonCyan);

            GlowPass(sb, () =>
            {
                NeonPanelGlow(sb, p, NeonCyan);
                GlowTextCentered(sb, title, titleY, NeonMag, TitleScale);
            });

            DrawCentered(sb, title, titleY, Color.White, TitleScale);
            DrawCentered(sb, Utils.Loc.T("paused.resume"), ScreenH * 0.50f, NeonCyan, ItemScale);
            DrawCentered(sb, Utils.Loc.T("paused.menu"), ScreenH * 0.58f, Scale(NeonDim, 0.9f), ItemScale);
        }
    }
}
