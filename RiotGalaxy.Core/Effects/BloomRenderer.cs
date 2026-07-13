using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace RiotGalaxy.Core.Effects
{
    /// <summary>
    /// Bloom post-process (только десктоп): сцена рендерится в offscreen-таргет, затем
    /// extract ярких зон (порог) → гаусс H/V в половинном разрешении → композиция в back buffer.
    /// Параметры — options.yaml (Utils.GameOptions.Bloom*). Шейдер — Content/Effects/Bloom.fx
    /// (на Android не собирается → эффект null → BeginScene возвращает false, рендер прямой).
    /// Вынесено из GameManager.
    /// </summary>
    public class BloomRenderer : IDisposable
    {
        private readonly Effect _effect;              // null → bloom недоступен
        private RenderTarget2D _sceneRT;              // сцена в полном размере вьюпорта
        private RenderTarget2D _bloomA, _bloomB;      // буферы свечения (половинное разрешение)
        private int _rtW, _rtH;                        // текущий размер таргетов
        private readonly Vector2[] _blurOffsets = new Vector2[15];
        private readonly float[] _blurWeights = new float[15];

        public BloomRenderer(Effect bloomEffect) => _effect = bloomEffect;

        /// <summary>
        /// Начать кадр: если bloom доступен и включён — перенаправить рендер сцены в offscreen-таргет.
        /// Возвращает true, если после сцены нужно вызвать EndScene.
        /// </summary>
        public bool BeginScene(GraphicsDevice device)
        {
            int w = device.Viewport.Width, h = device.Viewport.Height;
            if (_effect == null || !Utils.GameOptions.BloomEnabled || w <= 0 || h <= 0)
                return false;
            EnsureTargets(device, w, h);
            device.SetRenderTarget(_sceneRT);
            return true;
        }

        /// <summary>Пост-обработка: extract ярких зон → блюр H/V → сцена + свечение в back buffer.</summary>
        public void EndScene(GraphicsDevice device, SpriteBatch sb)
        {
            int vpW = _rtW, vpH = _rtH;
            var full = new Rectangle(0, 0, vpW, vpH);
            var bloomRect = new Rectangle(0, 0, _bloomA.Width, _bloomA.Height);

            // 1) Extract: яркие зоны сцены → _bloomA (половинное разрешение).
            device.SetRenderTarget(_bloomA);
            device.Clear(Color.Transparent);
            _effect.CurrentTechnique = _effect.Techniques["Extract"];
            _effect.Parameters["Threshold"].SetValue(Utils.GameOptions.BloomThreshold);
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, null, null, _effect);
            sb.Draw(_sceneRT, bloomRect, Color.White);
            sb.End();

            // 2) Гаусс по горизонтали: _bloomA → _bloomB.
            SetBlurParameters(1f / _bloomA.Width, 0f);
            device.SetRenderTarget(_bloomB);
            device.Clear(Color.Transparent);
            _effect.CurrentTechnique = _effect.Techniques["Blur"];
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, null, null, _effect);
            sb.Draw(_bloomA, bloomRect, Color.White);
            sb.End();

            // 3) Гаусс по вертикали: _bloomB → _bloomA.
            SetBlurParameters(0f, 1f / _bloomA.Height);
            device.SetRenderTarget(_bloomA);
            device.Clear(Color.Transparent);
            sb.Begin(SpriteSortMode.Immediate, BlendState.Opaque, SamplerState.LinearClamp, null, null, _effect);
            sb.Draw(_bloomB, bloomRect, Color.White);
            sb.End();

            // 4) Композиция в back buffer: сцена + аддитивно свечение.
            device.SetRenderTarget(null);
            device.Clear(Color.Black);
            sb.Begin(SpriteSortMode.Deferred, BlendState.Opaque, SamplerState.LinearClamp, null, null);
            sb.Draw(_sceneRT, full, Color.White);
            sb.End();
            sb.Begin(SpriteSortMode.Deferred, BlendState.Additive, SamplerState.LinearClamp, null, null);
            sb.Draw(_bloomA, full, Color.White * Utils.GameOptions.BloomIntensity);
            sb.End();
        }

        /// <summary>Пересоздать таргеты при смене размера вьюпорта.</summary>
        private void EnsureTargets(GraphicsDevice device, int w, int h)
        {
            if (_sceneRT != null && _rtW == w && _rtH == h) return;
            _sceneRT?.Dispose(); _bloomA?.Dispose(); _bloomB?.Dispose();
            _rtW = w; _rtH = h;
            _sceneRT = new RenderTarget2D(device, w, h, false, SurfaceFormat.Color, DepthFormat.None);
            int bw = Math.Max(1, w / 2), bh = Math.Max(1, h / 2); // свечение — в половинном разрешении
            _bloomA = new RenderTarget2D(device, bw, bh, false, SurfaceFormat.Color, DepthFormat.None);
            _bloomB = new RenderTarget2D(device, bw, bh, false, SurfaceFormat.Color, DepthFormat.None);
        }

        /// <summary>Гауссовы веса/смещения для одного направления (dx,dy — размер тексела по оси).</summary>
        private void SetBlurParameters(float dx, float dy)
        {
            float b = Utils.GameOptions.BloomBlurAmount;
            int n = _blurOffsets.Length;
            _blurWeights[0] = Gauss(0);
            _blurOffsets[0] = Vector2.Zero;
            float total = _blurWeights[0];
            for (int i = 0; i < n / 2; i++)
            {
                float w = Gauss(i + 1);
                _blurWeights[i * 2 + 1] = w;
                _blurWeights[i * 2 + 2] = w;
                total += w * 2;
                // сдвиг между парой текселей — для «бесплатной» билинейной выборки двух за раз
                float off = i * 2 + 1.5f;
                var delta = new Vector2(dx, dy) * off;
                _blurOffsets[i * 2 + 1] = delta;
                _blurOffsets[i * 2 + 2] = -delta;
            }
            for (int i = 0; i < n; i++) _blurWeights[i] /= total; // нормируем
            _effect.Parameters["SampleOffsets"].SetValue(_blurOffsets);
            _effect.Parameters["SampleWeights"].SetValue(_blurWeights);

            float Gauss(float x) => (float)(Math.Exp(-(x * x) / (2 * b * b)) / Math.Sqrt(2 * Math.PI * b * b));
        }

        public void Dispose()
        {
            _sceneRT?.Dispose();
            _bloomA?.Dispose();
            _bloomB?.Dispose();
        }
    }
}
