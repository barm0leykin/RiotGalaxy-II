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
                GameManager.Instance.RestartMission(); // рестарт с начала текущей миссии
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var sb = spriteBatch;
            int score = GameManager.Instance.LastScore;
            bool newRecord = score > 0 && score >= Utils.SaveData.HighScore;
            var accent = new Color(255, 80, 90); // красный неон поражения
            var p = PanelRect(0.5f, 0.24f, 0.72f);
            string title = Utils.Loc.T("gameover.title");
            float titleY = ScreenH * 0.30f;

            DrawDimmer(sb, 180);
            DrawNeonPanel(sb, p, accent);

            GlowPass(sb, () =>
            {
                NeonPanelGlow(sb, p, accent);
                GlowTextCentered(sb, title, titleY, accent, TitleScale);
                if (newRecord) GlowTextCentered(sb, Utils.Loc.T("result.newrecord"), ScreenH * 0.56f, NeonGold, ItemScale);
            });

            DrawCentered(sb, title, titleY, Color.White, TitleScale);
            DrawCentered(sb, Utils.Loc.F("result.score", score), ScreenH * 0.46f, Color.White, ItemScale);
            DrawCentered(sb, newRecord ? Utils.Loc.T("result.newrecord") : Utils.Loc.F("result.record", Utils.SaveData.HighScore),
                ScreenH * 0.56f, newRecord ? NeonGold : NeonDim, ItemScale);
            DrawCentered(sb, Utils.Loc.T("result.hint"), ScreenH * 0.66f, Scale(NeonDim, 0.8f), HintScale);
        }
    }
}
