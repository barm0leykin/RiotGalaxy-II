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
    /// Все миссии показываются сразу (шаг строки подстраивается — прокрутка не нужна).
    /// </summary>
    public class DevMenuScreen : Screen
    {
        private const float RowScale = 1.0f;
        private readonly List<(string id, string title)> _missions;
        private int _selected;
        private Rectangle Panel => PanelRect(0.8f, 0.06f, 0.96f);

        public DevMenuScreen() => _missions = MissionDirector.Catalog();

        // Шаг строки подстраивается под число миссий, чтобы всё влезло между заголовком и подсказкой.
        private float RowStep => (ScreenH * 0.90f - ScreenH * 0.20f) / System.Math.Max(1, _missions.Count);
        private float RowY(int i) => ScreenH * 0.20f + RowStep * (i + 0.5f) - Font.MeasureString("Ay").Y * RowScale / 2f;
        private string Label(int i) => $"{i + 1}.  {_missions[i].id} — {_missions[i].title}";
        private Rectangle RowRect(int i) => ListItemRect(Panel, Label(i), RowY(i), RowScale);

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            int n = _missions.Count;
            if (n == 0) { if (KeyPressed(Keys.Escape)) Back(); return; }

            for (int i = 0; i < n; i++)
                if (RowRect(i).Contains(MousePoint)) _selected = i;

            if (KeyPressed(Keys.Down) || KeyPressed(Keys.S)) _selected = (_selected + 1) % n;
            if (KeyPressed(Keys.Up) || KeyPressed(Keys.W)) _selected = (_selected - 1 + n) % n;

            if (MouseClicked())
                for (int i = 0; i < n; i++)
                    if (RowRect(i).Contains(MousePoint)) { Play(i); return; }

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
                if (_selected >= 0 && _selected < _missions.Count)
                    SelectionBarGlow(sb, RowRect(_selected), NeonGreen);
            });

            DrawCentered(sb, title, titleY, Color.White, TitleScale);

            for (int i = 0; i < _missions.Count; i++)
                DrawListItem(sb, p, Label(i), RowY(i), i == _selected, NeonGreen, RowScale);

            DrawCentered(sb, "↑/↓ — выбор · Enter — с начала · B — сразу к боссу · Esc — назад",
                ScreenH * 0.955f, Scale(NeonDim, 0.8f), HintScale);
        }
    }
}
