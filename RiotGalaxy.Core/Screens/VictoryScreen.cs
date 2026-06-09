using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Экран победы. Переходы (Пробел — заново, Esc — в меню) — Game1.HandleGameplayKeys.
    /// </summary>
    public class VictoryScreen : Screen
    {
        public override void Update(GameTime gameTime) { }

        public override void Draw(SpriteBatch spriteBatch)
        {
            int score = GameManager.Instance.LastScore;
            DrawCentered(spriteBatch, "ПОБЕДА!", ScreenH / 2f - 40, Color.Gold);
            DrawCentered(spriteBatch, $"Очки: {score}", ScreenH / 2f + 10, Color.White);
            DrawCentered(spriteBatch, "Пробел — заново, Esc — в меню", ScreenH / 2f + 60, Color.Gray);
        }
    }
}
