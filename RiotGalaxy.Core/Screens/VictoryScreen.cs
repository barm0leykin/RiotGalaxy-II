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
                GameManager.Instance.StartCampaign(); // сыграть кампанию заново
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var sb = spriteBatch;
            int score = GameManager.Instance.LastScore;
            bool newRecord = score > 0 && score >= Utils.SaveData.HighScore;
            var p = PanelRect(0.5f, 0.24f, 0.72f);
            string title = Utils.Loc.T("victory.title");
            float titleY = ScreenH * 0.30f;

            DrawDimmer(sb, 180);
            DrawNeonPanel(sb, p, NeonGold);

            GlowPass(sb, () =>
            {
                NeonPanelGlow(sb, p, NeonGold);
                GlowTextCentered(sb, title, titleY, NeonGold, TitleScale);
            });

            DrawCentered(sb, title, titleY, Color.White, TitleScale);
            DrawCentered(sb, Utils.Loc.F("result.score", score), ScreenH * 0.46f, Color.White, ItemScale);
            DrawCentered(sb, newRecord ? Utils.Loc.T("result.newrecord") : Utils.Loc.F("result.record", Utils.SaveData.HighScore),
                ScreenH * 0.56f, newRecord ? NeonGold : NeonDim, ItemScale);
            DrawCentered(sb, Utils.Loc.T("result.hint"), ScreenH * 0.66f, Scale(NeonDim, 0.8f), HintScale);
        }
    }
}
