using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Commands
{
    /// <summary>
    /// Команды для управления оружием (адаптировано из CocosSharp CommandsWeapon.cs).
    /// </summary>

    /// <summary>Сменить оружие игрока по id (см. WeaponConfig).</summary>
    public class CommandChWeapon : ICommand
    {
        private readonly string _id;
        public CommandChWeapon(string id) { _id = id; }
        public void Execute() => GameManager.Instance.Player?.ChangeWeapon(_id);
    }

    /// <summary>Тестовая команда: перейти к следующему уровню.</summary>
    public class CommandNextLevel : ICommand
    {
        public void Execute()
        {
            MessageLog.Add(Utils.Loc.T("cmd.nextlevel"), Microsoft.Xna.Framework.Color.Yellow);
            GameManager.Instance.DebugNextLevel();
        }
    }

    /// <summary>Активировать активный навык игрока по id (см. SkillsConfig / PlayerShip.UseSkill).</summary>
    public class CommandUseSkill : ICommand
    {
        private readonly string _id;
        public CommandUseSkill(string id) { _id = id; }
        public void Execute() => GameManager.Instance.Player?.UseSkill(_id);
    }
}
