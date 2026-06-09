using System.Collections.Generic;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>
    /// Локализация UI-строк. Строки вынесены из кода в Content/Locale/&lt;lang&gt;.yaml
    /// (плоская карта ключ→текст). Эталонная локаль — ru. Доступ по ключу: Loc.T("menu.start").
    ///
    /// Если ключа/файла нет — возвращается сам ключ (видно, что строка не локализована, без падений).
    /// </summary>
    public static class Loc
    {
        private static Dictionary<string, string> _strings = new Dictionary<string, string>();
        public static string Lang { get; private set; } = "ru";

        public static void Load(string lang = "ru")
        {
            Lang = lang;
            var data = Yaml.LoadAsset<Dictionary<string, string>>("Content/Locale/" + lang + ".yaml");
            _strings = data ?? new Dictionary<string, string>();
        }

        /// <summary>Строка по ключу (или сам ключ, если не найдена).</summary>
        public static string T(string key)
            => _strings.TryGetValue(key, out var v) ? v : key;

        /// <summary>Строка-шаблон по ключу с подстановкой аргументов (string.Format).</summary>
        public static string F(string key, params object[] args)
            => string.Format(T(key), args);
    }
}
