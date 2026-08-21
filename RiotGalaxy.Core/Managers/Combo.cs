using Microsoft.Xna.Framework;

namespace RiotGalaxy.Core.Managers
{
    /// <summary>
    /// Комбо-множитель ОЧКОВ за серию убийств. Серия живёт, пока между убийствами проходит
    /// меньше <c>window</c> секунд; полученный урон её обрывает (<c>resetOnDamage</c>).
    /// Пороги и множители — <c>combo</c> в bonuses.yaml (<see cref="Utils.BonusConfig.ComboDef"/>).
    /// Кредиты множитель не затрагивает — только счёт, идущий в рекорд.
    /// </summary>
    public static class Combo
    {
        private static float _timer;   // сколько секунд серия ещё жива

        /// <summary>Текущая длина серии убийств.</summary>
        public static int Kills { get; private set; }

        /// <summary>Текущий множитель очков (1 — комбо не набрано).</summary>
        public static int Multiplier { get; private set; } = 1;

        /// <summary>Остаток окна серии, 1→0 (для индикатора в HUD).</summary>
        public static float Fraction
        {
            get
            {
                float w = Utils.BonusConfig.Current.Combo.Window;
                return w > 0f ? MathHelper.Clamp(_timer / w, 0f, 1f) : 0f;
            }
        }

        /// <summary>Тик окна серии (звать из игрового Update).</summary>
        public static void Update(float dt)
        {
            if (Kills == 0) return;
            _timer -= dt;
            if (_timer <= 0f)
                Reset();
        }

        /// <summary>
        /// Учесть убийство: продлить серию и вернуть множитель, на который надо умножить очки.
        /// При переходе на новый порог показывает сообщение игроку.
        /// </summary>
        public static int RegisterKill()
        {
            var cfg = Utils.BonusConfig.Current.Combo;
            Kills++;
            _timer = cfg.Window;

            int mult = 1;
            if (cfg.Tiers != null)
                foreach (var t in cfg.Tiers)
                    if (Kills >= t.Kills && t.Mult > mult)
                        mult = t.Mult;

            if (mult > Multiplier)
                MessageLog.Add(Utils.Loc.F("combo.up", mult), new Color(255, 200, 90));
            Multiplier = mult;
            return Multiplier;
        }

        /// <summary>Урон по игроку обрывает серию (если так задано в конфиге).</summary>
        public static void OnPlayerDamaged()
        {
            if (Utils.BonusConfig.Current.Combo.ResetOnDamage)
                Reset();
        }

        /// <summary>Сбросить серию (старт боя, обрыв по времени, урон).</summary>
        public static void Reset()
        {
            Kills = 0;
            Multiplier = 1;
            _timer = 0f;
        }
    }
}
