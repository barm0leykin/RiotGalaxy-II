using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// DEV-меню: быстрый прыжок на любую миссию кампании (для тестирования). Открывается из
    /// главного меню только в Debug-сборке (пункт «[DEV]…» под #if DEBUG); в релизе недоступно.
    /// ↑/↓ — выбор, Enter — играть с начала миссии, B — сразу к боссу миссии, Esc — назад.
    /// </summary>
    public class DevMenuScreen : Screen
    {
        private const float RowScale = 1.0f;
        private const int Visible = 32;            // показываем все миссии сразу (без прокрутки)
        private readonly List<(string id, string title)> _missions;
        private int _selected;
        private int _scroll;
        private Rectangle Panel => PanelRect(0.8f, 0.06f, 0.96f);

        public DevMenuScreen() => _missions = MissionDirector.Catalog();

        // Шаг строки подстраивается под число миссий, чтобы всё влезло между заголовком и подсказкой.
        private float RowStep => (ScreenH * 0.90f - ScreenH * 0.20f) / System.Math.Max(1, _missions.Count);
        private float RowY(int vis) => ScreenH * 0.20f + RowStep * (vis + 0.5f) - Font.MeasureString("Ay").Y * RowScale / 2f;
        private string Label(int i) => $"{i + 1}.  {_missions[i].id} — {_missions[i].title}";
        private Rectangle RowRect(int vis, int idx) => ListItemRect(Panel, Label(idx), RowY(vis), RowScale);

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            int n = _missions.Count;
            if (n == 0) { if (KeyPressed(Keys.Escape)) Back(); return; }

            for (int vis = 0; vis < Visible && _scroll + vis < n; vis++)
                if (RowRect(vis, _scroll + vis).Contains(MousePoint)) _selected = _scroll + vis;

            if (KeyPressed(Keys.Down) || KeyPressed(Keys.S)) _selected = (_selected + 1) % n;
            if (KeyPressed(Keys.Up) || KeyPressed(Keys.W)) _selected = (_selected - 1 + n) % n;
            if (_selected < _scroll) _scroll = _selected;
            if (_selected >= _scroll + Visible) _scroll = _selected - Visible + 1;

            if (MouseClicked())
                for (int vis = 0; vis < Visible && _scroll + vis < n; vis++)
                    if (RowRect(vis, _scroll + vis).Contains(MousePoint)) { Play(_scroll + vis); return; }

            if (KeyPressed(Keys.Enter) || KeyPressed(Keys.Space)) Play(_selected);
            if (KeyPressed(Keys.B)) PlayBoss(_selected); // B — сразу к боссу миссии
            if (KeyPressed(Keys.Escape)) Back();
        }

        private void Play(int i) => GameManager.Instance.DevStartMission(i);
        private void PlayBoss(int i) => GameManager.Instance.DevStartMissionAtBoss(i);
        private void Back() => GameManager.Instance.ChangeGameState(GameManager.GameState.MainMenu);

        public override void Draw(SpriteBatch spriteBatch)
        {
            var sb = spriteBatch;
            var p = Panel;
            string title = "[DEV] Выбор миссии";
            float titleY = ScreenH * 0.115f;

            DrawDimmer(sb, 180);
            DrawNeonPanel(sb, p, NeonGreen);

            GlowPass(sb, () =>
            {
                NeonPanelGlow(sb, p, NeonGreen);
                GlowTextCentered(sb, title, titleY, NeonGreen, TitleScale);
                for (int vis = 0; vis < Visible && _scroll + vis < _missions.Count; vis++)
                    if (_scroll + vis == _selected)
                        SelectionBarGlow(sb, ListItemRect(p, Label(_selected), RowY(vis), RowScale), NeonGreen);
            });

            DrawCentered(sb, title, titleY, Color.White, TitleScale);

            for (int vis = 0; vis < Visible && _scroll + vis < _missions.Count; vis++)
            {
                int idx = _scroll + vis;
                DrawListItem(sb, p, Label(idx), RowY(vis), idx == _selected, NeonGreen, RowScale);
            }

            DrawCentered(sb, "↑/↓ — выбор · Enter — с начала · B — сразу к боссу · Esc — назад",
                ScreenH * 0.955f, Scale(NeonDim, 0.8f), HintScale);
        }
    }
}
