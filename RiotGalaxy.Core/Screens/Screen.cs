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

        /// <summary>Тёмная панель с рамкой-акцентом — рамка вокруг контента меню.</summary>
        protected void DrawPanel(SpriteBatch sb, Rectangle r)
        {
            var px = GameManager.Instance.SimpleTexture;
            if (px == null) return;
            sb.Draw(px, r, new Color(12, 18, 38, 215));        // тёмная заливка
            var border = new Color(90, 130, 210, 210);          // синеватая рамка-акцент
            const int t = 2;
            sb.Draw(px, new Rectangle(r.X, r.Y, r.Width, t), border);
            sb.Draw(px, new Rectangle(r.X, r.Bottom - t, r.Width, t), border);
            sb.Draw(px, new Rectangle(r.X, r.Y, t, r.Height), border);
            sb.Draw(px, new Rectangle(r.Right - t, r.Y, t, r.Height), border);
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

        /// <summary>Текст пункта меню: выбранный — жёлтый и в маркерах «» », прочие — белые.</summary>
        protected void DrawMenuItem(SpriteBatch sb, string text, float y, bool selected, float scale = 0f)
        {
            if (scale <= 0f) scale = ItemScale;
            string label = selected ? "» " + text + " «" : text;
            DrawCentered(sb, label, y, selected ? Color.Yellow : Color.White, scale);
        }
    }
}
