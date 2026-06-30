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
    /// ↑/↓ — выбор, Enter — играть с начала миссии, Esc — назад.
    /// </summary>
    public class DevMenuScreen : Screen
    {
        private const float RowScale = 1.05f;
        private const int Visible = 12;            // строк в окне (миссий пока меньше)
        private readonly List<(string id, string title)> _missions;
        private int _selected;
        private int _scroll;

        public DevMenuScreen() => _missions = MissionDirector.Catalog();

        private float RowY(int vis) => ScreenH * 0.22f + vis * ScreenH * 0.06f;
        private string Label(int i) => $"{i + 1}. {_missions[i].id} — {_missions[i].title}";
        private Rectangle RowRect(int vis, int idx) => CenteredItemRect(Label(idx), RowY(vis), RowScale);

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
            if (KeyPressed(Keys.Escape)) Back();
        }

        private void Play(int i) => GameManager.Instance.DevStartMission(i);
        private void Back() => GameManager.Instance.ChangeGameState(GameManager.GameState.MainMenu);

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawDimmer(spriteBatch);
            DrawPanel(spriteBatch, PanelRect(0.8f, 0.08f, 0.96f));

            DrawCentered(spriteBatch, "[DEV] Выбор миссии", ScreenH * 0.14f, Color.Lime, TitleScale);

            for (int vis = 0; vis < Visible && _scroll + vis < _missions.Count; vis++)
            {
                int idx = _scroll + vis;
                DrawMenuItem(spriteBatch, Label(idx), RowY(vis), idx == _selected);
            }

            DrawCentered(spriteBatch, "↑/↓ — выбор · Enter — играть с начала миссии · Esc — назад",
                ScreenH * 0.93f, Color.Gray, HintScale);
        }
    }
}
