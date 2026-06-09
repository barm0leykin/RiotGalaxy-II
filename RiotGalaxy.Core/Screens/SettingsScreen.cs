using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiotGalaxy.Core.Managers;
using RiotGalaxy.Core.Utils;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Экран настроек: громкость звуковых эффектов с сохранением в файл.
    /// Управление: стрелки влево/вправо или кнопки [-]/[+]; «Назад»/Esc — сохранить и в меню.
    /// </summary>
    public class SettingsScreen : Screen
    {
        private const float Step = 0.1f;
        private const float BtnScale = 2.4f; // символы [-]/[+] крупно

        // Крупные квадратные тач-зоны по бокам от значения громкости.
        private Rectangle MinusRect => new Rectangle((int)(ScreenW / 2f - 240), (int)(ScreenH * 0.44f), 120, 100);
        private Rectangle PlusRect => new Rectangle((int)(ScreenW / 2f + 120), (int)(ScreenH * 0.44f), 120, 100);
        private Rectangle BackRect => CenteredItemRect(Utils.Loc.T("settings.back"), ScreenH * 0.7f, ItemScale);

        private void ChangeVolume(float delta)
        {
            var audio = AudioManager.Instance;
            audio.EffectsVolume = MathHelper.Clamp(audio.EffectsVolume + delta, 0f, 1f);
        }

        private void Back()
        {
            GameSettings.Save();
            GameManager.Instance.ChangeGameState(GameManager.GameState.MainMenu);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            if (KeyPressed(Keys.Left) || KeyPressed(Keys.A))
                ChangeVolume(-Step);
            if (KeyPressed(Keys.Right) || KeyPressed(Keys.D))
                ChangeVolume(Step);

            if (MouseClicked())
            {
                if (MinusRect.Contains(MousePoint)) ChangeVolume(-Step);
                else if (PlusRect.Contains(MousePoint)) ChangeVolume(Step);
                else if (BackRect.Contains(MousePoint)) Back();
            }

            if (KeyPressed(Keys.Escape) || KeyPressed(Keys.Enter))
                Back();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawCentered(spriteBatch, Utils.Loc.T("settings.title"), ScreenH * 0.20f, Color.Orange, TitleScale);

            int percent = (int)System.Math.Round(AudioManager.Instance.EffectsVolume * 100);
            DrawCentered(spriteBatch, Utils.Loc.F("settings.volume", percent), ScreenH * 0.32f, Color.White, ItemScale);

            bool minusHover = MinusRect.Contains(MousePoint);
            bool plusHover = PlusRect.Contains(MousePoint);
            DrawButton(spriteBatch, "[-]", MinusRect, minusHover);
            DrawButton(spriteBatch, "[+]", PlusRect, plusHover);

            bool backHover = BackRect.Contains(MousePoint);
            DrawCentered(spriteBatch, Utils.Loc.T("settings.back"), ScreenH * 0.7f, backHover ? Color.Yellow : Color.White, ItemScale);

            DrawCentered(spriteBatch, Utils.Loc.T("settings.hint"), ScreenH * 0.9f, Color.Gray, HintScale);
        }

        /// <summary>Крупный символ-кнопка по центру тач-зоны (подсветка цветом при наведении).</summary>
        private void DrawButton(SpriteBatch sb, string symbol, Rectangle rect, bool hover)
        {
            if (Font == null) return;
            Vector2 sz = Font.MeasureString(symbol) * BtnScale;
            var pos = new Vector2(rect.Center.X - sz.X / 2f, rect.Center.Y - sz.Y / 2f);
            sb.DrawString(Font, symbol, pos, hover ? Color.Yellow : Color.White,
                0f, Vector2.Zero, BtnScale, SpriteEffects.None, 0f);
        }
    }
}
