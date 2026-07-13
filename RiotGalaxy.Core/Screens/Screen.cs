using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Microsoft.Xna.Framework.Input.Touch;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Базовый класс экрана меню (заставка, главное меню, настройки).
    /// Сам читает ввод (клавиатура/мышь) с защитой от ложного срабатывания на первом кадре.
    /// </summary>
    public abstract class Screen
    {
        protected KeyboardState Kb, PrevKb;
        protected MouseState Ms, PrevMs;

        // Кросс-платформенный указатель (мышь на desktop, палец на Android).
        // Позиция — в ВИРТУАЛЬНЫХ координатах (1280x768).
        private bool _ptrDown, _ptrPrevDown;
        private Point _ptrPos;

        protected int ScreenW => GameManager.Instance.ScreenWidth;
        protected int ScreenH => GameManager.Instance.ScreenHeight;
        protected SpriteFont Font => GameManager.Instance.Font;

        protected Screen()
        {
            // Захватываем стартовое состояние, чтобы клавиша/клик, которыми нас открыли,
            // не «прокликались» на первом же кадре.
            Kb = PrevKb = Keyboard.GetState();
            Ms = PrevMs = Mouse.GetState();
#if ANDROID
            _ptrDown = _ptrPrevDown = TouchPanel.GetState().Count > 0;
#else
            _ptrDown = _ptrPrevDown = Ms.LeftButton == ButtonState.Pressed;
#endif
        }

        public virtual void Update(GameTime gameTime)
        {
            PrevKb = Kb;
            Kb = Keyboard.GetState();
            PrevMs = Ms;
            Ms = Mouse.GetState();

            // Кросс-платформенный указатель
            _ptrPrevDown = _ptrDown;
#if ANDROID
            var touches = TouchPanel.GetState();
            if (touches.Count > 0)
            {
                _ptrDown = true;
                _ptrPos = GameManager.Instance.ScreenToVirtual(touches[0].Position).ToPoint();
            }
            else
            {
                _ptrDown = false; // позицию сохраняем последней
            }
#else
            _ptrDown = Ms.LeftButton == ButtonState.Pressed;
            _ptrPos = GameManager.Instance.ScreenToVirtual(new Vector2(Ms.X, Ms.Y)).ToPoint();
#endif
        }

        public abstract void Draw(SpriteBatch spriteBatch);

        protected bool KeyPressed(Keys key) => Kb.IsKeyDown(key) && PrevKb.IsKeyUp(key);

        /// <summary>Клик = момент нажатия (edge) указателя (ЛКМ или касание), как и у клавиш —
        /// чтобы клик/тап, которым открыли экран, не «прокликивал» его на отпускании.</summary>
        protected bool MouseClicked() => _ptrDown && !_ptrPrevDown;

        /// <summary>Позиция указателя в виртуальных координатах (1280x768).</summary>
        protected Point MousePoint => _ptrPos;

        // Базовые масштабы UI (шрифт мелкий — 14pt; на телефоне нужно крупнее и с запасом под палец).
        protected const float TitleScale = 2.2f;    // заголовки экранов
        protected const float ItemScale = 1.6f;     // кликабельные пункты меню
        protected const float HintScale = 1.1f;     // подсказки внизу
        protected const float MinTouchHeight = 84f; // мин. высота тач-зоны (вирт. пиксели)

        /// <summary>Текст по центру по горизонтали (y — верх текста).</summary>
        protected void DrawCentered(SpriteBatch sb, string text, float y, Color color, float scale = 1f)
        {
            if (Font == null) return;
            Vector2 size = Font.MeasureString(text) * scale;
            sb.DrawString(Font, text, new Vector2(ScreenW / 2f - size.X / 2f, y),
                color, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
        }

        /// <summary>
        /// Тач-зона для горизонтально-центрированного пункта меню: ширина по тексту с большим
        /// запасом, высота — не меньше MinTouchHeight, полоса центрируется на тексте. Совпадает
        /// с позицией DrawCentered (тот же y — верх текста), чтобы отрисовка и попадание не расходились.
        /// </summary>
        protected Rectangle CenteredItemRect(string text, float y, float scale)
        {
            Vector2 size = (Font != null ? Font.MeasureString(text) : new Vector2(160, 30)) * scale;
            float w = size.X + 120f;
            float h = MathHelper.Max(size.Y + 48f, MinTouchHeight);
            float x = ScreenW / 2f - w / 2f;
            float top = y + size.Y / 2f - h / 2f; // центрируем полосу на тексте
            return new Rectangle((int)x, (int)top, (int)w, (int)h);
        }

        // ── Оформление меню (без ассетов, через SimpleTexture) ───────────────

        /// <summary>Полупрозрачное затемнение всего экрана — фон/звёзды не мешают читать меню.</summary>
        protected void DrawDimmer(SpriteBatch sb, int alpha = 150)
        {
            var px = GameManager.Instance.SimpleTexture;
            if (px != null) sb.Draw(px, new Rectangle(0, 0, ScreenW, ScreenH), new Color(0, 0, 0, alpha));
        }

        /// <summary>Центрированная панель в долях экрана (0..1).</summary>
        protected Rectangle PanelRect(float widthFrac, float topFrac, float bottomFrac)
        {
            int w = (int)(ScreenW * widthFrac);
            int x = (ScreenW - w) / 2;
            int top = (int)(ScreenH * topFrac);
            int bottom = (int)(ScreenH * bottomFrac);
            return new Rectangle(x, top, w, bottom - top);
        }

        // ══════════════════════════════════════════════════════════════════
        //  Неоновый UI-кит: тёмные панели, аддитивное свечение, плашки-выбор,
        //  двухколоночные строки, секции. Свечение — отдельным аддитивным
        //  проходом (GlowPass), поверх которого рисуется чёткий текст.
        // ══════════════════════════════════════════════════════════════════

        // Палитра
        protected static readonly Color NeonBg    = new Color(8, 12, 28, 236);   // фон панели
        protected static readonly Color NeonEdge  = new Color(12, 18, 40, 255);
        protected static readonly Color NeonCyan  = new Color(90, 220, 255);
        protected static readonly Color NeonMag   = new Color(255, 95, 205);
        protected static readonly Color NeonGold  = new Color(255, 205, 90);
        protected static readonly Color NeonGreen = new Color(120, 255, 170);
        protected static readonly Color NeonDim   = new Color(150, 172, 205);
        protected static readonly Color NeonOff   = new Color(96, 104, 126);     // недоступно/макс

        private Texture2D Px => GameManager.Instance.SimpleTexture;
        private Texture2D GlowTex => GameManager.Instance.GlowTexture;

        protected static Color Scale(Color c, float f) =>
            new Color((byte)(c.R * f), (byte)(c.G * f), (byte)(c.B * f), c.A);
        // ВАЖНО: батч рисует в premultiplied-alpha (стандарт MonoGame). Чтобы полупрозрачная
        // ЦВЕТНАЯ заливка реально была прозрачной (а не «полный цвет при любой альфе»), RGB нужно
        // домножить на альфу — это и делает FromNonPremultiplied.
        protected static Color WithA(Color c, int a) => Color.FromNonPremultiplied(c.R, c.G, c.B, a);

        protected void FillRect(SpriteBatch sb, Rectangle r, Color c) { if (Px != null) sb.Draw(Px, r, c); }
        protected void BorderRect(SpriteBatch sb, Rectangle r, Color c, int t = 2)
        {
            if (Px == null) return;
            sb.Draw(Px, new Rectangle(r.X, r.Y, r.Width, t), c);
            sb.Draw(Px, new Rectangle(r.X, r.Bottom - t, r.Width, t), c);
            sb.Draw(Px, new Rectangle(r.X, r.Y, t, r.Height), c);
            sb.Draw(Px, new Rectangle(r.Right - t, r.Y, t, r.Height), c);
        }

        /// <summary>Аддитивный проход: переоткрываем UI-батч в Additive, рисуем свечение,
        /// возвращаемся к AlphaBlend. Всё свечение экрана — одним вызовом, ДО чёткого текста.</summary>
        protected void GlowPass(SpriteBatch sb, System.Action draw)
        {
            if (GlowTex == null) return;
            sb.End();
            GameManager.Instance.BeginUiBatch(sb, BlendState.Additive);
            draw();
            sb.End();
            GameManager.Instance.BeginUiBatch(sb, null); // назад к AlphaBlend
        }

        /// <summary>Мягкое пятно свечения (внутри GlowPass). Тон — цвет, яркость — его величина.</summary>
        protected void Glow(SpriteBatch sb, Rectangle r, Color c) { if (GlowTex != null) sb.Draw(GlowTex, r, c); }

        /// <summary>Тёмная неон-панель: заливка + внешняя акцентная рамка + тонкая внутренняя линия.</summary>
        protected void DrawNeonPanel(SpriteBatch sb, Rectangle r, Color accent)
        {
            FillRect(sb, r, NeonBg);
            BorderRect(sb, r, WithA(accent, 235), 2);
            BorderRect(sb, new Rectangle(r.X + 5, r.Y + 5, r.Width - 10, r.Height - 10), WithA(accent, 55), 1);
        }
        /// <summary>Свечение вдоль рамки панели (внутри GlowPass).</summary>
        protected void NeonPanelGlow(SpriteBatch sb, Rectangle r, Color accent)
        {
            const int g = 30; var c = Scale(accent, 0.35f);
            Glow(sb, new Rectangle(r.X - g, r.Y - g, r.Width + 2 * g, g * 2), c);
            Glow(sb, new Rectangle(r.X - g, r.Bottom - g, r.Width + 2 * g, g * 2), c);
            Glow(sb, new Rectangle(r.X - g, r.Y - g, g * 2, r.Height + 2 * g), c);
            Glow(sb, new Rectangle(r.Right - g, r.Y - g, g * 2, r.Height + 2 * g), c);
        }

        /// <summary>Плашка выбранного пункта: деликатная заливка + яркая левая кромка + тонкие
        /// линии сверху/снизу для «рамки». Держим спокойной, чтобы текст поверх читался.</summary>
        protected void DrawSelectionBar(SpriteBatch sb, Rectangle r, Color accent)
        {
            FillRect(sb, r, WithA(accent, 20));                                       // деликатная заливка
            FillRect(sb, new Rectangle(r.X, r.Y, 3, r.Height), accent);              // яркая левая кромка
            FillRect(sb, new Rectangle(r.X, r.Y, r.Width, 1), WithA(accent, 70));    // верхняя хайрлайн
            FillRect(sb, new Rectangle(r.X, r.Bottom - 1, r.Width, 1), WithA(accent, 70)); // нижняя
        }
        /// <summary>Свечение плашки выбора (внутри GlowPass): лёгкий ореол только у левой кромки.</summary>
        protected void SelectionBarGlow(SpriteBatch sb, Rectangle r, Color accent)
        {
            Glow(sb, new Rectangle(r.X - 14, r.Y - 3, 26, r.Height + 6), Scale(accent, 0.3f));
        }

        /// <summary>Свечение центрированного текста (внутри GlowPass): мягкий ореол за строкой
        /// (radial-glow, без смещённых копий глифов — не двоит и не «мылит» пиксельный шрифт).</summary>
        protected void GlowTextCentered(SpriteBatch sb, string text, float y, Color c, float scale)
        {
            if (Font == null || string.IsNullOrEmpty(text)) return;
            var size = Font.MeasureString(text) * scale;
            TextHalo(sb, ScreenW / 2f, y + size.Y / 2f, size, c);
        }
        /// <summary>Мягкий ореол за текстом: цепочка перекрывающихся radial-пятен вдоль строки,
        /// равномерно подсвечивает надпись, не касаясь чёткости самих глифов.</summary>
        private void TextHalo(SpriteBatch sb, float cx, float cy, Vector2 size, Color c)
        {
            var col = Scale(c, 0.4f);
            float d = size.Y * 1.7f;                 // диаметр пятна ~ высота строки
            int n = System.Math.Max(1, (int)System.Math.Round(size.X / (size.Y * 0.9f)));
            float left = cx - size.X / 2f;
            for (int i = 0; i < n; i++)
            {
                float bx = left + size.X * (i + 0.5f) / n;
                Glow(sb, new Rectangle((int)(bx - d / 2f), (int)(cy - d / 2f), (int)d, (int)d), col);
            }
        }

        /// <summary>Строка «две колонки»: подпись слева, значение/цена справа (по краям прямоугольника).</summary>
        protected void DrawRow(SpriteBatch sb, Rectangle r, string left, string right, Color lc, Color rc, float scale, float pad = 18f)
        {
            if (Font == null) return;
            float th = Font.MeasureString("Ay").Y * scale;
            float ty = r.Y + (r.Height - th) / 2f;
            if (!string.IsNullOrEmpty(left))
                sb.DrawString(Font, left, new Vector2(r.X + pad, ty), lc, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            if (!string.IsNullOrEmpty(right))
            {
                float rw = Font.MeasureString(right).X * scale;
                sb.DrawString(Font, right, new Vector2(r.Right - pad - rw, ty), rc, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        /// <summary>Заголовок секции: подпись слева + акцентное подчёркивание.</summary>
        protected void DrawSectionHeader(SpriteBatch sb, string text, Rectangle r, Color accent, float scale)
        {
            if (Font != null)
                sb.DrawString(Font, text, new Vector2(r.X + 18, r.Y), accent, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            FillRect(sb, new Rectangle(r.X + 18, r.Bottom - 4, r.Width - 36, 2), WithA(accent, 130));
        }

        /// <summary>Прямоугольник плашки на всю ширину панели для центрированного пункта (topY — верх текста).</summary>
        protected Rectangle ListItemRect(Rectangle panel, string label, float topY, float scale)
        {
            float th = Font != null ? Font.MeasureString(label).Y * scale : 30f;
            int h = (int)System.Math.Max(MinTouchHeight * 0.72f, th + 16f);
            int cy = (int)(topY + th / 2f);
            return new Rectangle(panel.X + 16, cy - h / 2, panel.Width - 32, h);
        }

        /// <summary>Пункт меню-списка (неон): при выборе — плашка на всю ширину панели + белый текст,
        /// иначе приглушённый. Свечение плашки рисуется отдельно в GlowPass (см. ListItemRect).</summary>
        protected void DrawListItem(SpriteBatch sb, Rectangle panel, string label, float topY, bool selected,
            Color accent, float scale = 0f)
        {
            if (scale <= 0f) scale = ItemScale;
            if (selected) DrawSelectionBar(sb, ListItemRect(panel, label, topY, scale), accent);
            DrawCentered(sb, label, topY, selected ? Color.White : NeonDim, scale);
        }
    }
}
