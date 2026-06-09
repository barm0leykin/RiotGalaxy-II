using System.Collections.Generic;
using RiotGalaxy.Core.GameObjects;

namespace RiotGalaxy.Core.Managers
{
    /// <summary>
    /// Обработка столкновений игровых объектов. Вынесено из GameManager.
    ///
    /// Каждая пара проверяется ОДИН раз (O(n²/2)). Обработчики несимметричны
    /// (снаряд-враг, враг-игрок, снаряд-игрок, бонус-игрок), поэтому для пары
    /// вызываются оба порядка — какой подойдёт, тот и сработает.
    /// </summary>
    public class CollisionSystem
    {
        /// <summary>Проверить и разрешить столкновения всех пар в списке.</summary>
        public void ResolveAll(List<GameObject> objects)
        {
            int count = objects.Count;
            for (int i = 0; i < count; i++)
            {
                var a = objects[i];
                if (a == null || !a.IsAlive) continue;

                for (int z = i + 1; z < count; z++)
                {
                    var b = objects[z];
                    if (b == null || !b.IsAlive) continue;
                    if (!a.Intersects(b)) continue;

                    Resolve(a, b);
                    Resolve(b, a); // обработчики несимметричны; Resolve сам проверяет IsAlive
                }
            }
        }

        /// <summary>Разрешить столкновение упорядоченной пары (a — «активный», b — «цель»).</summary>
        private void Resolve(GameObject a, GameObject b)
        {
            // Мёртвые объекты больше не наносят и не получают урон
            if (!a.IsAlive || !b.IsAlive)
                return;

            // Снаряд игрока попал во врага
            if (a is Shell shell && b is Enemy enemy && shell.PlayerSide)
                ShellHitsEnemy(shell, enemy);
            // Враг столкнулся с кораблём игрока (таран)
            else if (a is Enemy en && b is PlayerShip ship)
                EnemyHitsPlayer(en, ship);
            // Вражеский снаряд попал в игрока
            else if (a is Shell sh && b is PlayerShip ps && !sh.PlayerSide)
                ShellHitsPlayer(sh, ps);
            // Игрок подобрал бонус
            else if (a is Bonus bonus && b is PlayerShip player)
            {
                bonus.Apply(player);
                bonus.IsAlive = false;
            }
        }

        /// <summary>Уничтожить всех врагов на экране (бонус NukeBomb).</summary>
        public void KillAllEnemies(List<GameObject> objects)
        {
            foreach (var obj in objects)
                if (obj is Enemy enemy)
                    enemy.TakeDamage(enemy.Hp);

            GameManager.Instance.Shake(Utils.EffectsConfig.NukeShake.Magnitude,
                                       Utils.EffectsConfig.NukeShake.Duration);
        }

        private void ShellHitsEnemy(Shell shell, Enemy enemy)
        {
            // Искра в точке попадания (если враг выживет — это hit-feedback; если умрёт,
            // ProcessObjectRemoval добавит полноценный взрыв сверху).
            if (enemy.Hp > shell.Damage)
                GameManager.Instance.Particles.HitSpark(shell.Position, enemy.ExplosionColor);

            enemy.TakeDamage(shell.Damage);
            if (!shell.IsPiercing)
                shell.IsAlive = false; // обычный снаряд исчезает; лазер летит насквозь
        }

        private void EnemyHitsPlayer(Enemy enemy, PlayerShip player)
        {
            player.TakeDamage(enemy.Damage);
            enemy.TakeDamage(enemy.Hp); // враг уничтожается при таране
        }

        private void ShellHitsPlayer(Shell shell, PlayerShip player)
        {
            player.TakeDamage(shell.Damage);
            shell.IsAlive = false;
        }
    }
}
