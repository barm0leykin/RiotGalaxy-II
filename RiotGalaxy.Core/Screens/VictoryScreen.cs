using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Экран победы. Тап/Пробел — заново; Esc/«Назад» — в меню (клавиши — Game1.HandleGameplayKeys).
    /// </summary>
    public class VictoryScreen : Screen
    {
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (MouseClicked() || KeyPressed(Keys.Enter))
                GameManager.Instance.ChangeGameState(GameManager.GameState.Playing);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            int score = GameManager.Instance.LastScore;
            DrawCentered(spriteBatch, "ПОБЕДА!", ScreenH * 0.34f, Color.Gold, TitleScale);
            DrawCentered(spriteBatch, $"Очки: {score}", ScreenH * 0.5f, Color.White, ItemScale);
            DrawCentered(spriteBatch, "Тап / Пробел — заново · Esc — в меню", ScreenH * 0.6f, Color.Gray, HintScale);
        }
    }
}
