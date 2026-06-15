using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiotGalaxy.Core.Managers;
using RiotGalaxy.Core.Utils;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Экран настроек: громкость эффектов и язык интерфейса (сохраняются в settings.yaml).
    /// Управление: ↑/↓ — выбор пункта, ←/→ — менять значение выбранного, Enter — активировать,
    /// Esc — сохранить и назад. Мышь/тач: наведение + клик ([-]/[+], язык, «Назад»).
    /// </summary>
    public class SettingsScreen : Screen
    {
        private const float Step = 0.1f;
        private const float BtnScale = 2.4f;
        private const int Volume = 0, Language = 1, Back = 2;
        private int _selected = Volume;

        // Позиции строк (доли высоты).
        private float VolumeY => ScreenH * 0.28f;
        private float LangY => ScreenH * 0.56f;
        private float BackY => ScreenH * 0.70f;

        private Rectangle MinusRect => new Rectangle((int)(ScreenW / 2f - 240), (int)(ScreenH * 0.38f), 120, 100);
        private Rectangle PlusRect => new Rectangle((int)(ScreenW / 2f + 120), (int)(ScreenH * 0.38f), 120, 100);
        private Rectangle VolumeRect => CenteredItemRect("Громкость", VolumeY, ItemScale);
        private Rectangle LangRect => CenteredItemRect(LangLabel, LangY, ItemScale);
        private Rectangle BackRect => CenteredItemRect(Loc.T("settings.back"), BackY, ItemScale);

        private static string LangLabel => Loc.F("settings.language", Loc.T("lang." + GameSettings.Language));

        private void ChangeVolume(float delta)
        {
            var audio = AudioManager.Instance;
            audio.EffectsVolume = MathHelper.Clamp(audio.EffectsVolume + delta, 0f, 1f);
        }

        private void ToggleLanguage()
        {
            GameSettings.Language = GameSettings.Language == "en" ? "ru" : "en";
            Loc.Load(GameSettings.Language);
            GameSettings.Save();
        }

        private void Exit()
        {
            GameSettings.Save();
            GameManager.Instance.ChangeGameState(GameManager.GameState.MainMenu);
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Наведение мышью/тачем задаёт выбранный пункт.
            if (VolumeRect.Contains(MousePoint) || MinusRect.Contains(MousePoint) || PlusRect.Contains(MousePoint))
                _selected = Volume;
            else if (LangRect.Contains(MousePoint)) _selected = Language;
            else if (BackRect.Contains(MousePoint)) _selected = Back;

            // Навигация курсором.
            if (KeyPressed(Keys.Down) || KeyPressed(Keys.S)) _selected = (_selected + 1) % 3;
            if (KeyPressed(Keys.Up) || KeyPressed(Keys.W)) _selected = (_selected + 2) % 3;

            // Изменение значения выбранного пункта стрелками ←/→.
            bool left = KeyPressed(Keys.Left) || KeyPressed(Keys.A);
            bool right = KeyPressed(Keys.Right) || KeyPressed(Keys.D);
            if (_selected == Volume)
            {
                if (left) ChangeVolume(-Step);
                if (right) ChangeVolume(Step);
            }
            else if (_selected == Language && (left || right))
            {
                ToggleLanguage();
            }

            // Активация выбранного (Enter/Пробел).
            if (KeyPressed(Keys.Enter) || KeyPressed(Keys.Space))
            {
                if (_selected == Language) ToggleLanguage();
                else if (_selected == Back) Exit();
            }

            // Клик мышью/тачем.
            if (MouseClicked())
            {
                if (MinusRect.Contains(MousePoint)) ChangeVolume(-Step);
                else if (PlusRect.Contains(MousePoint)) ChangeVolume(Step);
                else if (LangRect.Contains(MousePoint)) ToggleLanguage();
                else if (BackRect.Contains(MousePoint)) Exit();
            }

            if (KeyPressed(Keys.Escape)) Exit();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawDimmer(spriteBatch);
            DrawPanel(spriteBatch, PanelRect(0.62f, 0.12f, 0.88f));

            DrawCentered(spriteBatch, Loc.T("settings.title"), ScreenH * 0.17f, Color.Orange, TitleScale);

            // Громкость (значение + крупные кнопки [-]/[+]); подсветка строки при выборе.
            int percent = (int)System.Math.Round(AudioManager.Instance.EffectsVolume * 100);
            DrawMenuItem(spriteBatch, Loc.F("settings.volume", percent), VolumeY, _selected == Volume);
            DrawButton(spriteBatch, "[-]", MinusRect, MinusRect.Contains(MousePoint));
            DrawButton(spriteBatch, "[+]", PlusRect, PlusRect.Contains(MousePoint));

            DrawMenuItem(spriteBatch, LangLabel, LangY, _selected == Language);
            DrawMenuItem(spriteBatch, Loc.T("settings.back"), BackY, _selected == Back);

            DrawCentered(spriteBatch, Loc.T("settings.hint"), ScreenH * 0.92f, Color.Gray, HintScale);
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
