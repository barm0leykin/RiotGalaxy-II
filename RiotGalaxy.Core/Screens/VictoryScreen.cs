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
            DrawCentered(spriteBatch, Utils.Loc.T("victory.title"), ScreenH * 0.30f, Color.Gold, TitleScale);
            DrawCentered(spriteBatch, Utils.Loc.F("result.score", score), ScreenH * 0.46f, Color.White, ItemScale);

            bool newRecord = score > 0 && score >= Utils.SaveData.HighScore;
            DrawCentered(spriteBatch, newRecord ? Utils.Loc.T("result.newrecord") : Utils.Loc.F("result.record", Utils.SaveData.HighScore),
                ScreenH * 0.56f, newRecord ? Color.Gold : Color.LightGray, ItemScale);

            DrawCentered(spriteBatch, Utils.Loc.T("result.hint"), ScreenH * 0.66f, Color.Gray, HintScale);
        }
    }
}
