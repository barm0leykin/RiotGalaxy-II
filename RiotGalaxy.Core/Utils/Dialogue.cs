using System.Collections.Generic;

namespace RiotGalaxy.Core.Utils
{
    /// <summary>Одна реплика диалога: кто говорит, текст и (опц.) спрайт-портрет.</summary>
    public class DialogueLine
    {
        public string Speaker { get; set; }
        public string Text { get; set; }
        public string Portrait { get; set; } // ассет, напр. "Images/portrait_commander" (опционально)
        public string Tag { get; set; }      // метка реплики (напр. intro/phase2/phase3/defeat у боссов)
    }

    /// <summary>
    /// Диалог — последовательность реплик из Content/Dialogues/&lt;name&gt;.yaml.
    /// Используется DialogueScreen (брифинги/сюжет). Грузится через TitleContainer (кросс-платформенно).
    /// </summary>
    public class Dialogue
    {
        public List<DialogueLine> Lines { get; set; }

        /// <summary>Загрузить диалог по имени; null, если файла нет или он пуст.</summary>
        public static Dialogue Load(string name)
        {
            var d = Yaml.LoadAsset<Dialogue>("Content/Dialogues/" + name + ".yaml");
            return (d?.Lines != null && d.Lines.Count > 0) ? d : null;
        }
    }
}
