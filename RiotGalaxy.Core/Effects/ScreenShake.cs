using System;
using Microsoft.Xna.Framework;

namespace RiotGalaxy.Core.Effects
{
    /// <summary>
    /// Тряска экрана (взрывы/босс/нюк/урон): случайное смещение виртуального кадра
    /// с линейным затуханием. Вынесено из GameManager; смещение читает letterbox-матрица.
    /// </summary>
    public class ScreenShake
    {
        private float _time;
        private float _duration;
        private float _magnitude;
        private readonly Random _rng = new Random();

        /// <summary>Текущее смещение кадра (виртуальные пиксели).</summary>
        public Vector2 Offset { get; private set; } = Vector2.Zero;

        /// <summary>Запустить тряску. Слабая тряска не перебивает более сильную активную.</summary>
        /// <param name="magnitude">амплитуда в виртуальных пикселях</param>
        /// <param name="duration">длительность, сек</param>
        public void Start(float magnitude, float duration = 0.3f)
        {
            if (magnitude <= 0f) return;
            if (_time <= 0f || magnitude >= _magnitude)
            {
                _magnitude = magnitude;
                _duration = duration;
                _time = duration;
            }
        }

        /// <summary>Затухание и пересчёт случайного смещения кадра (звать каждый кадр).</summary>
        public void Update(float dt)
        {
            if (_time <= 0f)
            {
                Offset = Vector2.Zero;
                return;
            }
            _time -= dt;
            float k = Math.Max(0f, _time / _duration); // 1 → 0, линейное затухание
            float mag = _magnitude * k;
            Offset = new Vector2(
                (float)(_rng.NextDouble() * 2.0 - 1.0) * mag,
                (float)(_rng.NextDouble() * 2.0 - 1.0) * mag);
        }
    }
}
