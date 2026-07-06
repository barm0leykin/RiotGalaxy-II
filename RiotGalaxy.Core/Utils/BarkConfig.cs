using System.Collections.Generic;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>
    /// Реплики пилота в бою (barks) из Content/Config/barks.yaml: карта триггер → список фраз.
    /// Показ и кулдаун — в <see cref="Managers.Barks"/>. Игра работает и без файла (просто без реплик).
    /// </summary>
    public static class BarkConfig
    {
        private static Dictionary<string, List<string>> _byTrigger = new Dictionary<string, List<string>>();

        public static void Load()
        {
            var data = Yaml.LoadAsset<Dictionary<string, List<string>>>(Yaml.ConfigAsset("barks.yaml"));
            _byTrigger = data ?? new Dictionary<string, List<string>>();
            Log.Debug($"Barks loaded: {_byTrigger.Count} triggers");
        }

        /// <summary>Список фраз для триггера (или null, если нет).</summary>
        public static List<string> Get(string trigger)
        {
            if (trigger != null && _byTrigger.TryGetValue(trigger, out var list) && list != null && list.Count > 0)
                return list;
            return null;
        }
    }
}
