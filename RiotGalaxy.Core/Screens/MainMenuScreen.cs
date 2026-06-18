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
    /// Главное меню: «Продолжить» (если есть чекпоинт) / «Начать игру» / «Магазин» /
    /// «Настройки» / «Сменить профиль» / «Выход». Управление мышью и клавиатурой.
    /// Список пунктов динамический (Продолжить появляется при наличии сохранённой позиции).
    /// </summary>
    public class MainMenuScreen : Screen
    {
        private const float SpacingY = 84f; // крупные пункты + запас под палец
        private int _selected;
        private int _hover = -1;

        private List<(string label, Action act)> _items = new List<(string, Action)>();

        private float StartY => ScreenH * 0.36f;
        private Rectangle ItemRect(int i) => CenteredItemRect(_items[i].label, StartY + i * SpacingY, ItemScale);

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
            _items.Add((Utils.Loc.T("menu.exit"), () => Game1.Instance.Exit()));

            if (_selected >= _items.Count) _selected = _items.Count - 1;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            BuildItems();
            int n = _items.Count;

            // Наведение мышью
            _hover = -1;
            for (int i = 0; i < n; i++)
                if (ItemRect(i).Contains(MousePoint)) { _hover = i; _selected = i; break; }

            // Навигация клавиатурой
            if (KeyPressed(Keys.Down) || KeyPressed(Keys.S)) _selected = (_selected + 1) % n;
            if (KeyPressed(Keys.Up) || KeyPressed(Keys.W)) _selected = (_selected - 1 + n) % n;

            // Активация
            if (_hover >= 0 && MouseClicked())
                _items[_hover].act();
            else if (KeyPressed(Keys.Enter) || KeyPressed(Keys.Space))
                _items[_selected].act();
            else if (KeyPressed(Keys.Escape))
                _items[n - 1].act(); // последний пункт — «Выход»
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (_items.Count == 0) BuildItems();

            DrawDimmer(spriteBatch);
            DrawPanel(spriteBatch, PanelRect(0.62f, 0.10f, 0.97f));

            DrawCentered(spriteBatch, Utils.Loc.T("menu.title"), ScreenH * 0.16f, Color.Orange, TitleScale);

            DrawCentered(spriteBatch, Utils.Loc.F("menu.profile_label", Utils.SaveData.CurrentProfile), ScreenH * 0.25f, Color.Cyan, HintScale);
            if (Utils.SaveData.HighScore > 0)
                DrawCentered(spriteBatch, Utils.Loc.F("menu.record", Utils.SaveData.HighScore), ScreenH * 0.295f, Color.LightGray, HintScale);
            if (Utils.SaveData.Currency > 0)
                DrawCentered(spriteBatch, Utils.Loc.F("menu.credits", Utils.SaveData.Currency), ScreenH * 0.33f, Color.Gold, HintScale);

            for (int i = 0; i < _items.Count; i++)
            {
                bool active = (i == _hover) || (i == _selected);
                DrawMenuItem(spriteBatch, _items[i].label, StartY + i * SpacingY, active);
            }

            DrawCentered(spriteBatch, Utils.Loc.T("menu.hint"), ScreenH * 0.95f, Color.Gray, HintScale);
        }
    }
}
