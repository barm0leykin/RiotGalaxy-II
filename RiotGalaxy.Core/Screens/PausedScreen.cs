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

            // Замороженная игра под паузой
            gm.DrawGameplay();

            // Полупрозрачное затемнение
            if (gm.SimpleTexture != null)
                spriteBatch.Draw(gm.SimpleTexture,
                    new Rectangle(0, 0, ScreenW, ScreenH), new Color(0, 0, 0, 150));

            // Меню паузы
            DrawCentered(spriteBatch, "ПАУЗА", ScreenH / 2f - 40, Color.White);
            DrawCentered(spriteBatch, "Esc / P — продолжить", ScreenH / 2f + 20, Color.Yellow);
            DrawCentered(spriteBatch, "Q — выход в меню", ScreenH / 2f + 56, Color.Gray);
        }
    }
}
