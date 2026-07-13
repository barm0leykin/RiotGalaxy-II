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
        // Hit-зона — по той же локализованной строке, что и отрисовка (иначе зоны расходятся).
        private Rectangle VolumeRect => CenteredItemRect(VolumeLabel, VolumeY, ItemScale);
        private static string VolumeLabel =>
            Loc.F("settings.volume", (int)System.Math.Round(AudioManager.Instance.EffectsVolume * 100));
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

        // Прямоугольники пунктов для навигации (0=громкость, 1=язык, 2=назад).
        private Rectangle ItemRect(int i) => i == Volume ? VolumeRect : i == Language ? LangRect : BackRect;

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Кнопки [-]/[+] громкости — поверх общей навигации (и hover, и клик).
            if (MinusRect.Contains(MousePoint) || PlusRect.Contains(MousePoint))
            {
                _selected = Volume;
                if (MouseClicked())
                {
                    ChangeVolume(MinusRect.Contains(MousePoint) ? -Step : Step);
                    return;
                }
            }

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

            UpdateListNav(3, ref _selected, ItemRect,
                activate: i =>
                {
                    if (i == Language) ToggleLanguage();
                    else if (i == Back) Exit();
                    // Volume активации по Enter не имеет — регулируется ←/→ и кнопками
                },
                back: Exit);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var sb = spriteBatch;
            var p = PanelRect(0.62f, 0.12f, 0.88f);
            string title = Loc.T("settings.title");
            float titleY = ScreenH * 0.165f;
            string volLabel = VolumeLabel;

            DrawDimmer(sb, 175);
            DrawNeonPanel(sb, p, NeonCyan);

            GlowPass(sb, () =>
            {
                NeonPanelGlow(sb, p, NeonCyan);
                GlowTextCentered(sb, title, titleY, NeonMag, TitleScale);
                if (_selected == Volume) SelectionBarGlow(sb, ListItemRect(p, volLabel, VolumeY, ItemScale), NeonCyan);
                if (_selected == Language) SelectionBarGlow(sb, ListItemRect(p, LangLabel, LangY, ItemScale), NeonCyan);
                if (_selected == Back) SelectionBarGlow(sb, ListItemRect(p, Loc.T("settings.back"), BackY, ItemScale), NeonCyan);
            });

            DrawCentered(sb, title, titleY, Color.White, TitleScale);

            // Громкость (значение + крупные кнопки [-]/[+]); подсветка строки при выборе.
            DrawListItem(sb, p, volLabel, VolumeY, _selected == Volume, NeonCyan);
            DrawButton(sb, "[-]", MinusRect, MinusRect.Contains(MousePoint));
            DrawButton(sb, "[+]", PlusRect, PlusRect.Contains(MousePoint));

            DrawListItem(sb, p, LangLabel, LangY, _selected == Language, NeonCyan);
            DrawListItem(sb, p, Loc.T("settings.back"), BackY, _selected == Back, NeonCyan);

            DrawCentered(sb, Loc.T("settings.hint"), ScreenH * 0.92f, Scale(NeonDim, 0.8f), HintScale);
        }

        /// <summary>Крупный символ-кнопка по центру тач-зоны (подсветка цветом при наведении).</summary>
        private void DrawButton(SpriteBatch sb, string symbol, Rectangle rect, bool hover)
        {
            if (Font == null) return;
            Vector2 sz = Font.MeasureString(symbol) * BtnScale;
            var pos = new Vector2(rect.Center.X - sz.X / 2f, rect.Center.Y - sz.Y / 2f);
            sb.DrawString(Font, symbol, pos, hover ? NeonCyan : Color.White,
                0f, Vector2.Zero, BtnScale, SpriteEffects.None, 0f);
        }
    }
}
