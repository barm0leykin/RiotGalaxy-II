using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace RiotGalaxy.Core.Managers
{
    /// <summary>
    /// Реплики пилота в бою (barks): по триггеру показывает случайную фразу из barks.yaml через
    /// <see cref="MessageLog"/>, НЕ прерывая игру. Есть общий кулдаун (чтобы не спамили) и защита
    /// от повтора одной фразы подряд. Данные — <see cref="Utils.BarkConfig"/>.
    /// </summary>
    public static class Barks
    {
        private static readonly Random _rng = new Random();
        private static readonly Dictionary<string, int> _lastIdx = new Dictionary<string, int>();
        private static float _cooldown;                       // общий тайм-аут между репликами
        private const float DefaultGap = 3.5f;                // сек
        private static readonly Color BarkColor = new Color(150, 210, 255); // мягкий голубой — «пилот»

        /// <summary>Тик кулдауна (звать из игрового Update).</summary>
        public static void Update(float dt)
        {
            if (_cooldown > 0f) _cooldown -= dt;
        }

        /// <summary>Сбросить состояние (при старте боя/миссии).</summary>
        public static void Reset()
        {
            _cooldown = 0f;
            _lastIdx.Clear();
        }

        /// <summary>Показать реплику по триггеру, если кулдаун истёк и фразы для триггера есть.</summary>
        public static void Fire(string trigger, float gap = DefaultGap)
        {
            if (_cooldown > 0f) return;
            var list = Utils.BarkConfig.Get(trigger);
            if (list == null) return;

            int idx = _rng.Next(list.Count);
            if (list.Count > 1 && _lastIdx.TryGetValue(trigger, out int last) && idx == last)
                idx = (idx + 1) % list.Count; // не повторяем ту же фразу подряд
            _lastIdx[trigger] = idx;

            MessageLog.Add(list[idx], BarkColor);
            _cooldown = gap;
        }
    }
}
