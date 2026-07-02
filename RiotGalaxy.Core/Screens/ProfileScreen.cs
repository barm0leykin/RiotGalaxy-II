using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiotGalaxy.Core.Managers;
using RiotGalaxy.Core.Utils;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Выбор профиля игрока (слоты 1..SaveData.ProfileCount). Показывается после заставки.
    /// Enter/тап по слоту — играть этим профилем; R или кнопка «Сбросить» — стереть прогресс
    /// выбранного слота (с подтверждением); Esc — продолжить с текущим профилем.
    /// </summary>
    public class ProfileScreen : Screen
    {
        private int _slot;                 // выбранный слот (0..N-1)
        private bool _confirmReset;        // показываем запрос подтверждения сброса
        private (bool exists, int high, int currency, int level)[] _summ;

        public ProfileScreen() => Refresh();

        private void Refresh()
        {
            _summ = new (bool, int, int, int)[SaveData.ProfileCount];
            for (int i = 0; i < SaveData.ProfileCount; i++)
                _summ[i] = SaveData.Peek(i + 1);
        }

        private float SlotY(int i) => ScreenH * 0.32f + i * ScreenH * 0.13f;
        private float ResetY => ScreenH * 0.78f;
        private Rectangle SlotRect(int i) => CenteredItemRect(SlotLabel(i), SlotY(i), ItemScale);
        private Rectangle ResetRect => CenteredItemRect(Loc.F("profile.reset", _slot + 1), ResetY, ItemScale);

        private string SlotLabel(int i)
        {
            string name = Loc.F("profile.slot", i + 1);
            string info = _summ[i].exists
                ? Loc.F("profile.summary", _summ[i].high, _summ[i].currency)
                : Loc.T("profile.empty");
            return $"{name} — {info}";
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            // Запрос подтверждения сброса перехватывает ввод.
            if (_confirmReset)
            {
                if (KeyPressed(Keys.Enter) || KeyPressed(Keys.Space))
                {
                    SaveData.ResetProfile(_slot + 1);
                    Refresh();
                    _confirmReset = false;
                }
                else if (KeyPressed(Keys.Escape))
                {
                    _confirmReset = false;
                }
                return;
            }

            // Наведение мышью/тачем на слот.
            for (int i = 0; i < SaveData.ProfileCount; i++)
                if (SlotRect(i).Contains(MousePoint)) _slot = i;

            if (KeyPressed(Keys.Down) || KeyPressed(Keys.S)) _slot = (_slot + 1) % SaveData.ProfileCount;
            if (KeyPressed(Keys.Up) || KeyPressed(Keys.W)) _slot = (_slot - 1 + SaveData.ProfileCount) % SaveData.ProfileCount;

            // Сброс: клавиша R или кнопка «Сбросить».
            if (KeyPressed(Keys.R)) { _confirmReset = true; return; }

            if (MouseClicked())
            {
                if (ResetRect.Contains(MousePoint)) { _confirmReset = true; return; }
                for (int i = 0; i < SaveData.ProfileCount; i++)
                    if (SlotRect(i).Contains(MousePoint)) { Play(i); return; }
            }

            if (KeyPressed(Keys.Enter) || KeyPressed(Keys.Space)) Play(_slot);
            if (KeyPressed(Keys.Escape)) GameManager.Instance.ChangeGameState(GameManager.GameState.MainMenu);
        }

        /// <summary>Выбрать слот и перейти в меню с его прогрессом.</summary>
        private void Play(int i)
        {
            _slot = i;
            SaveData.SelectProfile(i + 1);
            GameSettings.LastProfile = i + 1;
            GameSettings.Save();
            GameManager.Instance.ChangeGameState(GameManager.GameState.MainMenu);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var sb = spriteBatch;
            var p = PanelRect(0.7f, 0.08f, 0.95f);
            string title = Loc.T("profile.title");
            float titleY = ScreenH * 0.15f;
            bool resetHover = !_confirmReset && ResetRect.Contains(MousePoint);

            DrawDimmer(sb, 175);
            DrawNeonPanel(sb, p, NeonCyan);

            GlowPass(sb, () =>
            {
                NeonPanelGlow(sb, p, NeonCyan);
                GlowTextCentered(sb, title, titleY, NeonMag, TitleScale);
                if (!_confirmReset)
                    SelectionBarGlow(sb, ListItemRect(p, SlotLabel(_slot), SlotY(_slot), ItemScale), NeonCyan);
                if (resetHover)
                    SelectionBarGlow(sb, ListItemRect(p, Loc.F("profile.reset", _slot + 1), ResetY, ItemScale), NeonMag);
            });

            DrawCentered(sb, title, titleY, Color.White, TitleScale);

            for (int i = 0; i < SaveData.ProfileCount; i++)
                DrawListItem(sb, p, SlotLabel(i), SlotY(i), i == _slot && !_confirmReset, NeonCyan);

            DrawListItem(sb, p, Loc.F("profile.reset", _slot + 1), ResetY, resetHover, NeonMag);

            DrawCentered(sb, Loc.T("profile.hint"), ScreenH * 0.92f, Scale(NeonDim, 0.8f), HintScale);

            // Оверлей подтверждения сброса.
            if (_confirmReset)
            {
                DrawDimmer(sb, 200);
                GlowPass(sb, () => GlowTextCentered(sb, Loc.F("profile.reset_confirm", _slot + 1), ScreenH * 0.5f, new Color(255, 90, 90), ItemScale));
                DrawCentered(sb, Loc.F("profile.reset_confirm", _slot + 1), ScreenH * 0.5f, new Color(255, 170, 170), ItemScale);
            }
        }
    }
}
