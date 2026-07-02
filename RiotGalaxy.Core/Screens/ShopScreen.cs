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
    /// Магазин (неон): секции «Апгрейды»/«Оружие», строки в две колонки (подпись + уровень слева,
    /// цена/статус справа), подсветка выбора плашкой. Высота строк подстраивается, чтобы ВСЁ
    /// влезало на один экран без прокрутки. Покупка за SaveData.Currency, сохраняется в профиль.
    /// </summary>
    public class ShopScreen : Screen
    {
        private int _sel;   // индекс среди выбираемых (товары по порядку, затем «Назад»)

        private enum Kind { Header, Item }
        private class Entry
        {
            public Kind Kind;
            public string Left, Right;
            public Color Accent;      // для заголовка секции
            public bool Max;
            public int Cost;
            public Action Buy;
        }

        // Полный список записей (заголовки + товары).
        private List<Entry> BuildEntries()
        {
            var e = new List<Entry>();
            e.Add(new Entry { Kind = Kind.Header, Left = "Апгрейды", Accent = NeonCyan });
            foreach (var u in UpgradeConfig.All)
            {
                int lvl = SaveData.GetUpgradeLevel(u.Id);
                bool max = lvl >= u.MaxLevel;
                int cost = u.CostForNext(lvl);
                e.Add(new Entry
                {
                    Kind = Kind.Item,
                    Left = $"{u.Name}   ур. {lvl}/{u.MaxLevel}",
                    Right = max ? Loc.T("shop.max") : cost.ToString(),
                    Max = max, Cost = cost,
                    Buy = () =>
                    {
                        SaveData.Currency -= cost;
                        SaveData.SetUpgradeLevel(u.Id, lvl + 1);
                        SaveData.Save();
                        MessageLog.Add($"{u.Name}: ур. {lvl + 1}", Color.Lime);
                    },
                });
            }

            e.Add(new Entry { Kind = Kind.Header, Left = "Оружие", Accent = NeonMag });
            foreach (var w in WeaponConfig.All)
            {
                int lvl = SaveData.GetWeaponLevel(w.Id);
                bool owned = lvl >= 1;
                bool max = lvl >= w.MaxLevel;
                int cost = owned ? w.CostForLevel(lvl) : w.UnlockCost;
                e.Add(new Entry
                {
                    Kind = Kind.Item,
                    Left = owned ? $"{w.Name}   ур. {lvl}/{w.MaxLevel}" : $"{w.Name}   ●",
                    Right = !owned ? $"Открыть · {w.UnlockCost}"
                          : max ? Loc.T("shop.max") : cost.ToString(),
                    Max = max, Cost = cost,
                    Buy = () =>
                    {
                        SaveData.Currency -= cost;
                        SaveData.SetWeaponLevel(w.Id, lvl + 1);
                        SaveData.Save();
                        MessageLog.Add(owned ? $"{w.Name}: ур. {lvl + 1}" : $"Открыто: {w.Name}", Color.Lime);
                    },
                });
            }
            return e;
        }

        // Геометрия: контент между шапкой и подсказкой; строки равномерно, всё влезает.
        private Rectangle Panel => PanelRect(0.66f, 0.05f, 0.96f);
        private float AreaTop => ScreenH * 0.195f;
        private float AreaBot => ScreenH * 0.905f;

        // Раскладка: прямоугольник каждой записи + «Назад». lines = записи + 1 (Назад).
        private (List<Rectangle> rects, Rectangle backRect, int lines) Layout(int entryCount)
        {
            int lines = entryCount + 1;
            float step = (AreaBot - AreaTop) / Math.Max(1, lines);
            var p = Panel;
            int h = (int)Math.Min(step * 0.86f, 64f);
            var rects = new List<Rectangle>(entryCount);
            for (int i = 0; i < entryCount; i++)
            {
                int cy = (int)(AreaTop + step * (i + 0.5f));
                rects.Add(new Rectangle(p.X + 18, cy - h / 2, p.Width - 36, h));
            }
            int by = (int)(AreaTop + step * (entryCount + 0.5f));
            var backRect = new Rectangle(p.X + 18, by - h / 2, p.Width - 36, h);
            return (rects, backRect, lines);
        }

        // Индексы записей-товаров (выбираемых) в общем списке.
        private static List<int> ItemIndices(List<Entry> e)
        {
            var idx = new List<int>();
            for (int i = 0; i < e.Count; i++) if (e[i].Kind == Kind.Item) idx.Add(i);
            return idx;
        }

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);
            var entries = BuildEntries();
            var items = ItemIndices(entries);
            int back = items.Count;                 // «Назад» — последний выбираемый
            var (rects, backRect, _) = Layout(entries.Count);

            // Наведение мышью
            for (int s = 0; s < items.Count; s++)
                if (rects[items[s]].Contains(MousePoint)) _sel = s;
            if (backRect.Contains(MousePoint)) _sel = back;

            if (KeyPressed(Keys.Down) || KeyPressed(Keys.S)) _sel = Math.Min(back, _sel + 1);
            if (KeyPressed(Keys.Up) || KeyPressed(Keys.W)) _sel = Math.Max(0, _sel - 1);

            if (MouseClicked())
            {
                if (backRect.Contains(MousePoint)) { Back(); return; }
                for (int s = 0; s < items.Count; s++)
                    if (rects[items[s]].Contains(MousePoint)) { TryBuy(entries[items[s]]); return; }
            }

            if (KeyPressed(Keys.Enter) || KeyPressed(Keys.Space))
            {
                if (_sel == back) { Back(); return; }
                if (_sel >= 0 && _sel < items.Count) TryBuy(entries[items[_sel]]);
            }
            if (KeyPressed(Keys.Escape)) Back();
        }

        private void Back() => GameManager.Instance.CloseShop();

        private void TryBuy(Entry r)
        {
            if (r == null || r.Max || SaveData.Currency < r.Cost) return;
            r.Buy();
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            var sb = spriteBatch;
            var entries = BuildEntries();
            var items = ItemIndices(entries);
            int back = items.Count;
            var (rects, backRect, _) = Layout(entries.Count);
            var p = Panel;

            int selEntry = (_sel >= 0 && _sel < items.Count) ? items[_sel] : -1;
            bool backSel = _sel == back;

            DrawDimmer(sb, 180);
            DrawNeonPanel(sb, p, NeonMag);

            string title = Loc.T("shop.title");
            float titleY = ScreenH * 0.075f;
            string credits = Loc.F("shop.credits", SaveData.Currency);
            float credY = ScreenH * 0.135f;
            FillRect(sb, new Rectangle(p.X + 40, (int)(ScreenH * 0.178f), p.Width - 80, 1), WithA(NeonMag, 70));

            // ── Свечение ──
            GlowPass(sb, () =>
            {
                NeonPanelGlow(sb, p, NeonMag);
                GlowTextCentered(sb, title, titleY, NeonMag, TitleScale);
                GlowTextCentered(sb, credits, credY, NeonGold, ItemScale);
                if (selEntry >= 0) SelectionBarGlow(sb, rects[selEntry], NeonCyan);
                if (backSel) SelectionBarGlow(sb, backRect, NeonCyan);
            });

            // ── Чёткий слой ──
            DrawCentered(sb, title, titleY, Color.White, TitleScale);
            DrawCentered(sb, credits, credY, NeonGold, ItemScale);

            for (int i = 0; i < entries.Count; i++)
            {
                var e = entries[i];
                var r = rects[i];
                if (e.Kind == Kind.Header)
                {
                    DrawSectionHeader(sb, e.Left, r, e.Accent, HintScale * 1.15f);
                    continue;
                }
                bool sel = i == selEntry;
                bool affordable = !e.Max && SaveData.Currency >= e.Cost;
                if (sel) DrawSelectionBar(sb, r, NeonCyan);

                Color lc = e.Max ? NeonOff : sel ? Color.White : NeonDim;
                Color rc = e.Max ? NeonOff
                         : affordable ? NeonGreen
                         : new Color(210, 120, 120);
                DrawRow(sb, r, e.Left, e.Right, lc, rc, RowTextScale);
            }

            // «Назад»
            if (backSel) DrawSelectionBar(sb, backRect, NeonCyan);
            DrawCentered(sb, Loc.T("shop.back"),
                backRect.Y + (backRect.Height - Font.MeasureString(Loc.T("shop.back")).Y * ItemScale) / 2f,
                backSel ? Color.White : NeonDim, ItemScale);

            DrawCentered(sb, Loc.T("shop.hint"), ScreenH * 0.955f, Scale(NeonDim, 0.8f), HintScale);
        }

        private const float RowTextScale = 1.05f;
    }
}
