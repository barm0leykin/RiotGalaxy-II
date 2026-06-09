using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Экран между уровнями: номер уровня, описание, статистика. Продолжить — в игру.
    /// </summary>
    public class NextLevelScreen : Screen
    {
        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            if (KeyPressed(Keys.Space) || KeyPressed(Keys.Enter) || MouseClicked())
                GameManager.Instance.ChangeGameState(GameManager.GameState.Playing);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var gm = GameManager.Instance;
            DrawCentered(spriteBatch, Utils.Loc.F("nextlevel.title", gm.CurrentLevel, gm.TotalLevels), ScreenH * 0.28f, Color.Orange, TitleScale);

            if (!string.IsNullOrWhiteSpace(gm.CurrentLevelDescription))
                DrawCentered(spriteBatch, gm.CurrentLevelDescription, ScreenH * 0.45f, Color.White, ItemScale);

            if (gm.Player != null)
                DrawCentered(spriteBatch, Utils.Loc.F("nextlevel.score", gm.Player.Score), ScreenH * 0.56f, Color.Yellow, ItemScale);

            DrawCentered(spriteBatch, Utils.Loc.T("nextlevel.hint"), ScreenH * 0.8f, Color.Gray, HintScale);
        }
    }
}
