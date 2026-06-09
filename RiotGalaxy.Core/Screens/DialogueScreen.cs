using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using RiotGalaxy.Core.Managers;
using RiotGalaxy.Core.Utils;

namespace RiotGalaxy.Core.Screens
{
    /// <summary>
    /// Экран диалога: реплики по очереди (имя говорящего + текст с переносом + опц. портрет)
    /// в нижней панели. Тап/Пробел/Enter — далее, Esc — пропустить весь диалог. По завершении
    /// GameManager переходит в заранее заданное состояние (см. GameManager.PlayDialogue/EndDialogue).
    /// </summary>
    public class DialogueScreen : Screen
    {
        private int _index;
        private const float TextScale = 1.3f;
        private readonly Dictionary<string, Texture2D> _portraits = new Dictionary<string, Texture2D>();

        private Dialogue D => GameManager.Instance.CurrentDialogue;

        public override void Update(GameTime gameTime)
        {
            base.Update(gameTime);

            var d = D;
            if (d == null) { GameManager.Instance.EndDialogue(); return; }

            if (KeyPressed(Keys.Escape)) { GameManager.Instance.EndDialogue(); return; } // пропустить всё

            if (MouseClicked() || KeyPressed(Keys.Space) || KeyPressed(Keys.Enter))
            {
                _index++;
                if (_index >= d.Lines.Count)
                    GameManager.Instance.EndDialogue();
            }
        }

        public override void Draw(SpriteBatch sb)
        {
            var d = D;
            if (d == null || Font == null) return;

            int i = MathHelper.Clamp(_index, 0, d.Lines.Count - 1);
            var line = d.Lines[i];
            var px = GameManager.Instance.SimpleTexture;

            // Нижняя панель диалога.
            var panel = new Rectangle(40, (int)(ScreenH * 0.60f), ScreenW - 80, (int)(ScreenH * 0.33f));
            if (px != null) sb.Draw(px, panel, new Color(10, 14, 28, 220));

            const int pad = 28;
            int textX = panel.X + pad;
            int textTop = panel.Y + pad;

            // Портрет (если задан и грузится) слева.
            var portrait = LoadPortrait(line.Portrait);
            if (portrait != null)
            {
                int size = panel.Height - pad * 2;
                sb.Draw(portrait, new Rectangle(panel.X + pad, panel.Y + pad, size, size), Color.White);
                textX = panel.X + pad + size + pad;
            }
            int maxTextW = panel.Right - pad - textX;

            // Имя говорящего.
            if (!string.IsNullOrEmpty(line.Speaker))
            {
                sb.DrawString(Font, line.Speaker, new Vector2(textX, textTop), Color.Gold,
                    0f, Vector2.Zero, ItemScale, SpriteEffects.None, 0f);
                textTop += (int)(Font.LineSpacing * ItemScale) + 8;
            }

            // Текст с переносом по словам.
            float lineH = Font.LineSpacing * TextScale;
            foreach (var wl in Wrap(line.Text ?? "", maxTextW, TextScale))
            {
                sb.DrawString(Font, wl, new Vector2(textX, textTop), Color.White,
                    0f, Vector2.Zero, TextScale, SpriteEffects.None, 0f);
                textTop += (int)lineH;
            }

            DrawCentered(sb, Utils.Loc.F("dialogue.hint", i + 1, d.Lines.Count),
                panel.Bottom + 12, Color.Gray, HintScale);
        }

        private Texture2D LoadPortrait(string asset)
        {
            if (string.IsNullOrEmpty(asset)) return null;
            if (_portraits.TryGetValue(asset, out var cached)) return cached;
            Texture2D t = null;
            try { t = GameManager.Instance.Content.Load<Texture2D>(asset); }
            catch { /* портрета нет — рисуем без него */ }
            _portraits[asset] = t;
            return t;
        }

        /// <summary>Разбить текст на строки по ширине maxWidth (перенос по словам).</summary>
        private List<string> Wrap(string text, float maxWidth, float scale)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(text)) return result;

            string cur = "";
            foreach (var w in text.Split(' '))
            {
                string test = cur.Length == 0 ? w : cur + " " + w;
                if (cur.Length > 0 && Font.MeasureString(test).X * scale > maxWidth)
                {
                    result.Add(cur);
                    cur = w;
                }
                else cur = test;
            }
            if (cur.Length > 0) result.Add(cur);
            return result;
        }
    }
}
