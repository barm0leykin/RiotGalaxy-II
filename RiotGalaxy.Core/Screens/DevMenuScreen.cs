using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// DEV-меню: быстрый прыжок на любую миссию кампании или СРАЗУ на её босс-волну.
    /// Открывается из главного меню только в Debug-сборке (пункт «[DEV]…» под #if DEBUG).
    /// ↑/↓ — выбор, Enter — запустить пункт (миссия с начала / босс), B — босс выбранной миссии,
    /// Esc — назад. Все пункты показываются сразу (шаг строки подстраивается — прокрутки нет).
    /// </summary>
    public class DevMenuScreen : Screen
    {
        private const float RowScale = 0.9f;
        private readonly List<(int missionIndex, bool boss, string label)> _items = new();
        private int _selected;
        private Rectangle Panel => PanelRect(0.8f, 0.05f, 0.965f);

        public DevMenuScreen()
        {
            var missions = MissionDirector.Catalog();
            for (int i = 0; i < missions.Count; i++)
            {
                _items.Add((i, false, $"{i + 1}.  {missions[i].id} — {missions[i].title}"));
                _items.Add((i, true, $"      → {missions[i].id} — сразу к боссу"));
            }
        }

        // Шаг строки подстраивается под число пунктов, чтобы всё влезло между заголовком и подсказкой.
        private float RowStep => (ScreenH * 0.915f - ScreenH * 0.165f) / System.Math.Max(1, _items.Count);
        private float RowY(int i) => ScreenH * 0.165f + RowStep * (i + 0.5f) - Font.MeasureString("Ay").Y * RowScale / 2f;
        private Rectangle RowRect(int i) => ListItemRect(Panel, _items[i].label, RowY(i), RowScale);

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            UpdateListNav(_items.Count, ref _selected, RowRect, activate: Activate, back: Back);
            if (KeyPressed(Keys.B)) // B — босс миссии выбранного пункта
                GameManager.Instance.DevStartMissionAtBoss(_items[_selected].missionIndex);
        }

        private void Activate(int i)
        {
            var (missionIndex, boss, _) = _items[i];
            if (boss) GameManager.Instance.DevStartMissionAtBoss(missionIndex);
            else GameManager.Instance.DevStartMission(missionIndex);
        }

        private void Back() => GameManager.Instance.ChangeGameState(GameManager.GameState.MainMenu);

        public override void Draw(SpriteBatch spriteBatch)
        {
            var sb = spriteBatch;
            var p = Panel;
            string title = "[DEV] Выбор миссии";
            float titleY = ScreenH * 0.085f;

            DrawDimmer(sb, 180);
            DrawNeonPanel(sb, p, NeonGreen);

            GlowPass(sb, () =>
            {
                NeonPanelGlow(sb, p, NeonGreen);
                GlowTextCentered(sb, title, titleY, NeonGreen, TitleScale);
                if (_selected >= 0 && _selected < _items.Count)
                    SelectionBarGlow(sb, RowRect(_selected), NeonGreen);
            });

            DrawCentered(sb, title, titleY, Color.White, TitleScale);

            for (int i = 0; i < _items.Count; i++)
            {
                bool sel = i == _selected;
                var r = RowRect(i);
                if (sel) DrawSelectionBar(sb, r, NeonGreen);
                // Босс-пункты — приглушённее и с magenta-акцентом при выборе.
                Color c = sel ? Color.White : _items[i].boss ? Scale(NeonDim, 0.75f) : NeonDim;
                DrawCentered(sb, _items[i].label, RowY(i), c, RowScale);
            }

            DrawCentered(sb, "↑/↓ — выбор · Enter — запустить · B — босс миссии · Esc — назад",
                ScreenH * 0.955f, Scale(NeonDim, 0.8f), HintScale);
        }
    }
}
