using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiotGalaxy.Core.Managers;
using RiotGalaxy.Core.Utils;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Экран настроек: громкость эффектов/музыки и язык интерфейса (сохраняются в settings.yaml).
    /// Управление: ↑/↓ — выбор пункта, ←/→ — менять значение выбранного, Enter — активировать,
    /// Esc — сохранить и назад. Мышь/тач: наведение + клик ([-]/[+] у строк громкости, язык, «Назад»).
    /// </summary>
    public class SettingsScreen : Screen
    {
        private const float Step = 0.1f;
        private const float BtnScale = 2.4f;
        private const int Volume = 0, Music = 1, Language = 2, Back = 3;
        private int _selected = Volume;

        // Позиции строк (доли высоты). У строк громкости под подписью — тач-кнопки [-]/[+].
        private float VolumeY => ScreenH * 0.225f;
        private float MusicY => ScreenH * 0.435f;
        private float LangY => ScreenH * 0.655f;
        private float BackY => ScreenH * 0.775f;

        private Rectangle MinusRect(float labelY) =>
            new Rectangle((int)(ScreenW / 2f - 240), (int)(labelY + ScreenH * 0.062f), 120, 90);
        private Rectangle PlusRect(float labelY) =>
            new Rectangle((int)(ScreenW / 2f + 120), (int)(labelY + ScreenH * 0.062f), 120, 90);

        // Hit-зона — по той же локализованной строке, что и отрисовка (иначе зоны расходятся).
        private Rectangle VolumeRect => CenteredItemRect(VolumeLabel, VolumeY, ItemScale);
        private Rectangle MusicRect => CenteredItemRect(MusicLabel, MusicY, ItemScale);
        private Rectangle LangRect => CenteredItemRect(LangLabel, LangY, ItemScale);
        private Rectangle BackRect => CenteredItemRect(Loc.T("settings.back"), BackY, ItemScale);

        private static string VolumeLabel =>
            Loc.F("settings.volume", (int)System.Math.Round(AudioManager.Instance.EffectsVolume * 100));
        private static string MusicLabel =>
            Loc.F("settings.music", (int)System.Math.Round(AudioManager.Instance.MusicVolume * 100));
        private static string LangLabel => Loc.F("settings.language", Loc.T("lang." + GameSettings.Language));

        private void ChangeVolume(int row, float delta)
        {
            var audio = AudioManager.Instance;
            if (row == Volume)
            {
                audio.EffectsVolume = MathHelper.Clamp(audio.EffectsVolume + delta, 0f, 1f);
                audio.Play("ui.select"); // сразу слышно новую громкость
            }
            else
            {
                audio.MusicVolume = MathHelper.Clamp(audio.MusicVolume + delta, 0f, 1f);
            }
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

        // Прямоугольники пунктов для навигации.
        private Rectangle ItemRect(int i) =>
            i == Volume ? VolumeRect : i == Music ? MusicRect : i == Language ? LangRect : BackRect;

        private float RowY(int row) => row == Volume ? VolumeY : MusicY;

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Кнопки [-]/[+] у строк громкости — поверх общей навигации (и hover, и клик).
            for (int row = Volume; row <= Music; row++)
            {
                var minus = MinusRect(RowY(row));
                var plus = PlusRect(RowY(row));
                if (minus.Contains(MousePoint) || plus.Contains(MousePoint))
                {
                    _selected = row;
                    if (MouseClicked())
                    {
                        ChangeVolume(row, minus.Contains(MousePoint) ? -Step : Step);
                        return;
                    }
                }
            }

            // Изменение значения выбранного пункта стрелками ←/→.
            bool left = KeyPressed(Keys.Left) || KeyPressed(Keys.A);
            bool right = KeyPressed(Keys.Right) || KeyPressed(Keys.D);
            if (_selected == Volume || _selected == Music)
            {
                if (left) ChangeVolume(_selected, -Step);
                if (right) ChangeVolume(_selected, Step);
            }
            else if (_selected == Language && (left || right))
            {
                ToggleLanguage();
            }

            UpdateListNav(4, ref _selected, ItemRect,
                activate: i =>
                {
                    if (i == Language) ToggleLanguage();
                    else if (i == Back) Exit();
                    // громкости активации по Enter не имеют — регулируются ←/→ и кнопками
                },
                back: Exit);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var sb = spriteBatch;
            var p = PanelRect(0.62f, 0.09f, 0.9f);
            string title = Loc.T("settings.title");
            float titleY = ScreenH * 0.115f;
            string volLabel = VolumeLabel;
            string musLabel = MusicLabel;

            DrawDimmer(sb, 175);
            DrawNeonPanel(sb, p, NeonCyan);

            GlowPass(sb, () =>
            {
                NeonPanelGlow(sb, p, NeonCyan);
                GlowTextCentered(sb, title, titleY, NeonMag, TitleScale);
                if (_selected == Volume) SelectionBarGlow(sb, ListItemRect(p, volLabel, VolumeY, ItemScale), NeonCyan);
                if (_selected == Music) SelectionBarGlow(sb, ListItemRect(p, musLabel, MusicY, ItemScale), NeonCyan);
                if (_selected == Language) SelectionBarGlow(sb, ListItemRect(p, LangLabel, LangY, ItemScale), NeonCyan);
                if (_selected == Back) SelectionBarGlow(sb, ListItemRect(p, Loc.T("settings.back"), BackY, ItemScale), NeonCyan);
            });

            DrawCentered(sb, title, titleY, Color.White, TitleScale);

            // Громкости (значение + крупные кнопки [-]/[+]); подсветка строки при выборе.
            DrawListItem(sb, p, volLabel, VolumeY, _selected == Volume, NeonCyan);
            DrawVolumeButtons(sb, VolumeY);
            DrawListItem(sb, p, musLabel, MusicY, _selected == Music, NeonCyan);
            DrawVolumeButtons(sb, MusicY);

            DrawListItem(sb, p, LangLabel, LangY, _selected == Language, NeonCyan);
            DrawListItem(sb, p, Loc.T("settings.back"), BackY, _selected == Back, NeonCyan);

            DrawCentered(sb, Loc.T("settings.hint"), ScreenH * 0.94f, Scale(NeonDim, 0.8f), HintScale);
        }

        private void DrawVolumeButtons(SpriteBatch sb, float labelY)
        {
            var minus = MinusRect(labelY);
            var plus = PlusRect(labelY);
            DrawButton(sb, "[-]", minus, minus.Contains(MousePoint));
            DrawButton(sb, "[+]", plus, plus.Contains(MousePoint));
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
