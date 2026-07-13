using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using RiotGalaxy.Core.GameObjects;

namespace RiotGalaxy.Core.Managers
{
    /// <summary>
    /// Спавн бонусов при смерти врага: звёзды-кредиты (номинал = reward врага, дробится на
    /// несколько звёзд) + авторский бонус из YAML уровня (drop:). Вынесено из GameManager.
    /// </summary>
    public static class BonusSpawner
    {
        private static readonly Random _rnd = new Random();

        // Пул случайных «фоновых» баффов (используется, только если buffDropChance > 0 в bonuses.yaml).
        private static readonly BonusType[] _buffTypes = { BonusType.POWER, BonusType.RAPID, BonusType.SPEED };

        /// <summary>Звёзды (кредиты) + опциональный случайный бафф в точке смерти врага.</summary>
        public static void SpawnStars(Vector2 pos, int starCredits, List<GameObject> objects)
        {
            var bc = Utils.BonusConfig.Current;
            int count = Math.Max(1, (int)Math.Round(starCredits / (float)Math.Max(1, bc.StarValue)));
            count = Math.Min(count, Math.Max(1, bc.MaxStarsPerKill));
            int baseVal = starCredits / count;
            int rem = starCredits - baseVal * count;
            for (int i = 0; i < count; i++)
            {
                int val = baseVal + (i < rem ? 1 : 0);
                var off = new Vector2((float)(_rnd.NextDouble() * 2 - 1) * 26f,
                                      (float)(_rnd.NextDouble() * 2 - 1) * 26f);
                objects.Add(new BonusStar(pos + off, Math.Max(1, val)));
            }

            // Опциональный «фоновый» случайный бафф — по умолчанию выключен (buffDropChance: 0).
            if (bc.BuffDropChance > 0 && _rnd.Next(100) < bc.BuffDropChance)
                objects.Add(new Bonus(_buffTypes[_rnd.Next(_buffTypes.Length)], pos));
        }

        /// <summary>Авторский бонус врага (drop: из YAML уровня) с шансом DropChance%.</summary>
        public static void SpawnAuthoredDrop(Enemy enemy, List<GameObject> objects)
        {
            if (enemy == null || string.IsNullOrEmpty(enemy.DropBonus))
                return;
            if (enemy.DropChance < 100 && _rnd.Next(100) >= enemy.DropChance)
                return;
            if (Bonus.TryParseType(enemy.DropBonus, out var bt))
                objects.Add(new Bonus(bt, enemy.Position));
        }
    }
}
