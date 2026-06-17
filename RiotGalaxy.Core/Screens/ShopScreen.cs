using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiotGalaxy.Core.Managers;
using RiotGalaxy.Core.Utils;
using RiotGalaxy.Core.Weapons;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Магазин: единый прокручиваемый список — сначала апгрейды (UpgradeConfig), затем оружие
    /// (WeaponConfig: разблокировка и индивидуальные уровни). Покупка за SaveData.Currency,
    /// сохраняется в профиль; применяется к кораблю при старте/продолжении.
    /// </summary>
    public class ShopScreen : Screen
    {
        private const float RowScale = 1.15f;
        private const int Visible = 7;     // строк в окне прокрутки
        private int _selected;
        private int _scroll;

        // Описание строки магазина (пересобирается каждый кадр из актуального состояния).
        private struct Row { public string Label; public bool Max; public int Cost; public Action Buy; }

        private float RowY(int vis) => ScreenH * 0.22f + vis * ScreenH * 0.085f;
        private float BackY => ScreenH * 0.90f;
        private Rectangle BackRect => CenteredItemRect(Loc.T("shop.back"), BackY, ItemScale);

        private List<Row> BuildRows()
        {
            var rows = new List<Row>();

            // Апгрейды (статы).
            foreach (var u in UpgradeConfig.All)
            {
                int lvl = SaveData.GetUpgradeLevel(u.Id);
                bool max = lvl >= u.MaxLevel;
                int cost = u.CostForNext(lvl);
                string label = max ? $"{u.Name}  ·  {Loc.T("shop.max")}"
                                   : Loc.F("shop.row", u.Name, lvl, u.MaxLevel, cost.ToString());
                rows.Add(new Row
                {
                    Label = label, Max = max, Cost = cost,
                    Buy = () =>
                    {
                        SaveData.Currency -= cost;
                        SaveData.SetUpgradeLevel(u.Id, lvl + 1);
                        SaveData.Save();
                        MessageLog.Add($"{u.Name}: ур. {lvl + 1}", Color.Lime);
                    },
                });
            }

            // Оружие (разблокировка + уровни).
            foreach (var w in WeaponConfig.All)
            {
                int lvl = SaveData.GetWeaponLevel(w.Id);
                bool owned = lvl >= 1;
                bool max = lvl >= w.MaxLevel;
                int cost = w.CostForLevel(lvl);
                string label = !owned ? Loc.F("shop.unlock", w.Name, w.UnlockCost)
                             : max ? $"{w.Name}  ·  {Loc.T("shop.max")}"
                             : Loc.F("shop.row", w.Name, lvl, w.MaxLevel, cost.ToString());
                rows.Add(new Row
                {
                    Label = label, Max = max, Cost = cost,
                    Buy = () =>
                    {
                        SaveData.Currency -= cost;
                        SaveData.SetWeaponLevel(w.Id, lvl + 1);
                        SaveData.Save();
                        MessageLog.Add(owned ? $"{w.Name}: ур. {lvl + 1}" : $"Открыто: {w.Name}", Color.Lime);
                    },
                });
            }
            return rows;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            var rows = BuildRows();
            int n = rows.Count;
            int back = n; // «Назад» — отдельный навигируемый пункт сразу после строк

            // Наведение мышью: на видимую строку или на «Назад».
            for (int vis = 0; vis < Visible && _scroll + vis < n; vis++)
                if (CenteredItemRect(rows[_scroll + vis].Label, RowY(vis), RowScale).Contains(MousePoint))
                    _selected = _scroll + vis;
            if (BackRect.Contains(MousePoint)) _selected = back;

            if (KeyPressed(Keys.Down) || KeyPressed(Keys.S)) _selected = Math.Min(back, _selected + 1);
            if (KeyPressed(Keys.Up) || KeyPressed(Keys.W)) _selected = Math.Max(0, _selected - 1);

            // Держим выбранную строку в окне прокрутки (для «Назад» прокрутку не трогаем).
            if (_selected < n)
            {
                if (_selected < _scroll) _scroll = _selected;
                if (_selected >= _scroll + Visible) _scroll = _selected - Visible + 1;
            }

            if (MouseClicked())
            {
                if (BackRect.Contains(MousePoint)) { Back(); return; }
                for (int vis = 0; vis < Visible && _scroll + vis < n; vis++)
                    if (CenteredItemRect(rows[_scroll + vis].Label, RowY(vis), RowScale).Contains(MousePoint))
                    { TryBuy(rows, _scroll + vis); return; }
            }

            // Enter/Space: «Назад» — выход, иначе покупка выбранной строки.
            if (KeyPressed(Keys.Enter) || KeyPressed(Keys.Space))
            {
                if (_selected == back) { Back(); return; }
                TryBuy(rows, _selected);
            }
            if (KeyPressed(Keys.Escape)) Back();
        }

        private void Back() => GameManager.Instance.CloseShop();

        private void TryBuy(List<Row> rows, int i)
        {
            if (i < 0 || i >= rows.Count) return;
            var r = rows[i];
            if (r.Max || SaveData.Currency < r.Cost) return; // максимум или не хватает кредитов
            r.Buy();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            DrawDimmer(spriteBatch);
            DrawPanel(spriteBatch, PanelRect(0.72f, 0.04f, 0.99f));

            DrawCentered(spriteBatch, Loc.T("shop.title"), ScreenH * 0.07f, Color.Orange, TitleScale);
            DrawCentered(spriteBatch, Loc.F("shop.credits", SaveData.Currency), ScreenH * 0.14f, Color.Gold, ItemScale);

            var rows = BuildRows();
            for (int vis = 0; vis < Visible && _scroll + vis < rows.Count; vis++)
            {
                int idx = _scroll + vis;
                var r = rows[idx];
                bool selected = idx == _selected;
                bool affordable = !r.Max && SaveData.Currency >= r.Cost;
                Color color = r.Max ? Color.Gray
                            : selected ? Color.Yellow
                            : affordable ? Color.White
                            : new Color(170, 120, 120);
                DrawCentered(spriteBatch, r.Label, RowY(vis), color, RowScale);
            }

            // Индикаторы прокрутки.
            if (_scroll > 0)
                DrawCentered(spriteBatch, "^", ScreenH * 0.195f, Color.Gray, HintScale);
            if (_scroll + Visible < rows.Count)
                DrawCentered(spriteBatch, "v", ScreenH * 0.83f, Color.Gray, HintScale);

            bool backSelected = _selected == rows.Count || BackRect.Contains(MousePoint);
            DrawMenuItem(spriteBatch, Loc.T("shop.back"), BackY, backSelected);
            DrawCentered(spriteBatch, Loc.T("shop.hint"), ScreenH * 0.95f, Color.Gray, HintScale);
        }
    }
}
