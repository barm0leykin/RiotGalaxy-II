using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Экран боя. Тонкая обёртка: вся боевая логика и отрисовка остаются в GameManager
    /// (UpdateGameplay/DrawGameplay), здесь — только маршрутизация через ScreenSystem.
    /// Ввод и переходы (Esc/P → пауза) обрабатывает Game1.HandleGameplayKeys.
    /// </summary>
    public class GameplayScreen : Screen
    {
        public override void Update(GameTime gameTime) => GameManager.Instance.UpdateGameplay(gameTime);

        public override void Draw(SpriteBatch spriteBatch) => GameManager.Instance.DrawGameplay();
    }
}
