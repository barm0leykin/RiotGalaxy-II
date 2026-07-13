using System;
using Microsoft.Xna.Framework;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>
    /// Общая игровая математика. Собрана из дублей по компонентам/объектам
    /// (EnemyBounceMovement/Bonus/Weapon/SortieMovement/FormationMovement).
    /// Конвенция углов курса: 0° = вверх, 90° = вправо, 180° = вниз.
    /// </summary>
    public static class MathUtil
    {
        /// <summary>Единичный вектор направления по углу в радианах (0 = вверх).</summary>
        public static Vector2 DirFromAngle(float radians)
            => new Vector2((float)Math.Sin(radians), -(float)Math.Cos(radians));

        /// <summary>Единичный вектор направления по углу в градусах (0 = вверх).</summary>
        public static Vector2 DirFromAngleDeg(float degrees)
            => DirFromAngle(MathHelper.ToRadians(degrees));

        /// <summary>
        /// Сдвинуть pos к target на шаг step. Возвращает true, когда цель достигнута
        /// (pos ставится точно в target) — общий «полёт к ячейке» формаций/вылетов.
        /// </summary>
        public static bool MoveTowards(ref Vector2 pos, Vector2 target, float step)
        {
            Vector2 to = target - pos;
            float dist = to.Length();
            if (dist <= step || dist < 0.001f)
            {
                pos = target;
                return true;
            }
            pos += to / dist * step;
            return false;
        }

        /// <summary>Плавный доворот current→target за шаг maxStep (градусы), кратчайшим путём.</summary>
        public static float ApproachAngleDeg(float current, float target, float maxStep)
        {
            // Кратчайшая разница в диапазоне (-180, 180] — иначе на границе ±180° доворот «длинным путём»
            float diff = Mod360(target - current + 180f) - 180f;
            if (Math.Abs(diff) <= maxStep)
                return Mod360(target);
            return Mod360(current + Math.Sign(diff) * maxStep);
        }

        /// <summary>Угол, приведённый к диапазону [0, 360).</summary>
        public static float Mod360(float a)
        {
            a %= 360f;
            return a < 0 ? a + 360f : a;
        }
    }
}
