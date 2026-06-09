using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Экран поражения. Тап/Пробел — заново; Esc/«Назад» — в меню (клавиши — Game1.HandleGameplayKeys).
    /// </summary>
    public class GameOverScreen : Screen
    {
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            // Тап/Enter — рестарт (на телефоне нет Space/Esc).
            if (MouseClicked() || KeyPressed(Keys.Enter))
                GameManager.Instance.ChangeGameState(GameManager.GameState.Playing);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            int score = GameManager.Instance.LastScore;
            DrawCentered(spriteBatch, "GAME OVER", ScreenH * 0.34f, Color.Red, TitleScale);
            DrawCentered(spriteBatch, $"Очки: {score}", ScreenH * 0.5f, Color.White, ItemScale);
            DrawCentered(spriteBatch, "Тап / Пробел — заново · Esc — в меню", ScreenH * 0.6f, Color.Gray, HintScale);
        }
    }
}
