using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Commands
{
    /// <summary>
    /// Команды живых кнопок боевого HUD (см. GameManager.CreateDebugButtons/CreateSkillButtons).
    /// Адаптировано из CocosSharp Command.cs; мёртвые команды старого UI удалены.
    /// </summary>

    public class CommandKillAll : ICommand
    {
        public void Execute()
        {
            var gameObjects = GameManager.Instance.GameObjects;

            foreach (var obj in gameObjects)
            {
                if (obj != null && obj.GetType().Name.Contains("Enemy"))
                {
                    obj.IsAlive = false;
                }
            }
            MessageLog.Add("Уничтожить всех", Microsoft.Xna.Framework.Color.Orange);
        }
    }

    public class CommandHpUp : ICommand
    {
        public void Execute()
        {
            var player = GameManager.Instance.Player;
            if (player != null)
            {
                player.Health = player.MaxHealth;
                MessageLog.Add("Полное лечение", Microsoft.Xna.Framework.Color.Lime);
            }
        }
    }
}
