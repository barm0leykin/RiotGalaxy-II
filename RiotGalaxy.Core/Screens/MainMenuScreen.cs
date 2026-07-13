using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiotGalaxy.Core;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Главное меню (неон): «Продолжить» (если есть чекпоинт) / «Начать игру» / «Магазин» /
    /// «Настройки» / «Сменить профиль» / «Выход». Управление мышью и клавиатурой.
    /// Список пунктов динамический (Продолжить появляется при наличии сохранённой позиции).
    /// </summary>
    public class MainMenuScreen : Screen
    {
        private int _selected;

        private List<(string label, Action act)> _items = new List<(string, Action)>();

        private Rectangle Panel => PanelRect(0.52f, 0.07f, 0.95f);
        private float ListTop => ScreenH * 0.40f;
        private float ListBot => ScreenH * 0.88f;
        private float RowH => (ListBot - ListTop) / Math.Max(1, _items.Count);
        private Rectangle RowRect(int i)
        {
            var p = Panel;
            int h = (int)Math.Min(RowH * 0.84f, MinTouchHeight);
            int cy = (int)(ListTop + RowH * (i + 0.5f));
            return new Rectangle(p.X + 16, cy - h / 2, p.Width - 32, h);
        }

        private void BuildItems()
        {
            var gm = GameManager.Instance;
            _items = new List<(string, Action)>();

            if (Utils.SaveData.HasCheckpoint)
                _items.Add((Utils.Loc.T("menu.continue"), () => gm.ContinueCampaign()));
            _items.Add((Utils.Loc.T("menu.start"), () => gm.StartCampaign()));
            _items.Add((Utils.Loc.T("menu.shop"), () => gm.OpenShop(GameManager.GameState.MainMenu)));
            _items.Add((Utils.Loc.T("menu.settings"), () => gm.ChangeGameState(GameManager.GameState.Settings)));
            _items.Add((Utils.Loc.T("menu.profile"), () => gm.ChangeGameState(GameManager.GameState.Profile)));
#if DEBUG
            // Только в Debug-сборке: быстрый выбор миссии для тестирования (в релизе скрыто).
            _items.Add(("[DEV] Выбрать миссию", () => gm.ChangeGameState(GameManager.GameState.DevMenu)));
#endif
            _items.Add((Utils.Loc.T("menu.exit"), () => Game1.Instance.Exit()));

            if (_selected >= _items.Count) _selected = _items.Count - 1;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            BuildItems();
            int n = _items.Count;

            UpdateListNav(n, ref _selected, RowRect,
                activate: i => _items[i].act(),
                back: () => _items[n - 1].act()); // последний пункт — «Выход»
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (_items.Count == 0) BuildItems();
            var sb = spriteBatch;
            var p = Panel;

            string title = Utils.Loc.T("menu.title");
            float titleY = ScreenH * 0.115f;
            float profY = ScreenH * 0.235f;
            float statY = ScreenH * 0.285f;

            DrawDimmer(sb, 175);
            DrawNeonPanel(sb, p, NeonCyan);

            // Разделитель под шапкой
            FillRect(sb, new Rectangle(p.X + 40, (int)(ScreenH * 0.335f), p.Width - 80, 1), WithA(NeonCyan, 70));

            // ── Свечение (аддитивный проход) ──
            GlowPass(sb, () =>
            {
                NeonPanelGlow(sb, p, NeonCyan);
                GlowTextCentered(sb, title, titleY, NeonMag, TitleScale);
                if (_selected >= 0 && _selected < _items.Count)
                    SelectionBarGlow(sb, RowRect(_selected), NeonCyan);
            });

            // ── Чёткий слой ──
            DrawCentered(sb, title, titleY, Color.White, TitleScale);

            DrawCentered(sb, Utils.Loc.F("menu.profile_label", Utils.SaveData.CurrentProfile), profY, NeonCyan, HintScale);
            string stats = "";
            if (Utils.SaveData.HighScore > 0) stats += Utils.Loc.F("menu.record", Utils.SaveData.HighScore);
            if (Utils.SaveData.Currency > 0) stats += (stats.Length > 0 ? "    " : "") + Utils.Loc.F("menu.credits", Utils.SaveData.Currency);
            if (stats.Length > 0) DrawCentered(sb, stats, statY, NeonGold, HintScale);

            for (int i = 0; i < _items.Count; i++)
            {
                bool active = i == _selected;
                var r = RowRect(i);
                if (active) DrawSelectionBar(sb, r, NeonCyan);
                DrawCentered(sb, _items[i].label, r.Y + (r.Height - Font.MeasureString(_items[i].label).Y * ItemScale) / 2f,
                    active ? Color.White : NeonDim, ItemScale);
            }

            DrawCentered(sb, Utils.Loc.T("menu.hint"), ScreenH * 0.955f, Scale(NeonDim, 0.8f), HintScale);
        }
    }
}
