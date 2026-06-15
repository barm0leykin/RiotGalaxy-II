using System;
using System.Threading.Tasks;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using RiotGalaxy.Core.Commands;
using RiotGalaxy.Core.GameObjects;
using RiotGalaxy.Core.Managers;

namespace RiotGalaxy.Core.Interface
{
    /// <summary>
    /// Класс GUI кнопки
    /// Адаптация MyButton из CocosSharp для MonoGame
    /// </summary>
    public class MyButton
    {
        protected ICommand cmd;
        public Texture2D sprite;
        protected int delay = 1000;
        bool blocked = false;
        public string name;
        
        public Vector2 Position { get; set; }
        public int Width { get; set; }
        public int Height { get; set; }
        public bool Visible { get; set; } = true;

        public MyButton(Vector2 pos)
        {
            Position = pos;
            cmd = new NoCommand();
            Width = 64;
            Height = 64;
        }

        public virtual void Delete()
        {
            // Очистка ресурсов
            sprite = null;
        }

        public async void Press()
        {
            if (!blocked)
            {
                cmd.Execute();
                blocked = true;
                await Task.Delay(delay);
                blocked = false;
            }            
        }

        public bool CheckCollision(Vector2 touchPoint)
        {
            Rectangle rect = GetRect();
            return touchPoint.X >= rect.X && touchPoint.X <= rect.X + rect.Width &&
                   touchPoint.Y >= rect.Y && touchPoint.Y <= rect.Y + rect.Height;
        }

        public virtual Rectangle GetRect()
        {
            // В CocosSharp точка привязки по центру, здесь также для совместимости
            return new Rectangle(
                (int)(Position.X - Width / 2), 
                (int)(Position.Y - Height / 2), 
                Width, 
                Height
            );
        }

        public virtual void Draw(SpriteBatch spriteBatch, Texture2D defaultTexture)
        {
            if (!Visible) return;
            
            Texture2D textureToDraw = sprite ?? defaultTexture;
            if (textureToDraw != null)
            {
                // Рисуем по тому же прямоугольнику, что и проверка клика (GetRect)
                spriteBatch.Draw(textureToDraw, GetRect(), Color.White);
            }
        }
    }

    /// <summary>
    /// Кнопка активного навыка: активирует навык по id, рисует затемнение-оверлей пропорционально
    /// оставшемуся кулдауну (заполняется снизу вверх). Иконку (sprite) задаёт создатель кнопки.
    /// </summary>
    public class ButtonSkill : MyButton
    {
        private readonly string _id;

        public ButtonSkill(Vector2 pos, string skillId) : base(pos)
        {
            _id = skillId;
            name = "skill_" + skillId;
            cmd = new CommandUseSkill(skillId);
        }

        public override void Draw(SpriteBatch spriteBatch, Texture2D defaultTexture)
        {
            base.Draw(spriteBatch, defaultTexture);

            var player = GameManager.Instance.Player;
            if (player == null || defaultTexture == null) return;

            float frac = player.SkillCooldownFraction(_id); // 1 → только что использован, 0 → готов
            if (frac <= 0f) return;

            // Затемнение, "вытекающее" сверху вниз по мере перезарядки.
            Rectangle r = GetRect();
            int h = (int)(r.Height * frac);
            spriteBatch.Draw(defaultTexture, new Rectangle(r.X, r.Y, r.Width, h), new Color(0, 0, 0, 170));
        }
    }

    /// <summary>
    /// Кнопка уничтожения всех врагов
    /// </summary>
    public class ButtonKillAll : MyButton
    {
        public ButtonKillAll(Vector2 pos) : base(pos)
        {
            name = "btn_killall";
            Width = 64;
            Height = 64;
            
            cmd = new CommandKillAll();
        }
    }

    /// <summary>
    /// Кнопка победы
    /// </summary>
    public class ButtonWin : MyButton
    {
        public ButtonWin(Vector2 pos) : base(pos)
        {
            name = "btn_win";
            Width = 64;
            Height = 64;
            
            cmd = new CommandWin();
        }
    }

    /// <summary>
    /// Кнопка паузы
    /// </summary>
    public class ButtonPause : MyButton
    {
        public ButtonPause(Vector2 pos) : base(pos)
        {
            name = "btn_pause";
            Width = 64;
            Height = 64;
            
            cmd = new CommandSwitchPause();
        }
    }

    /// <summary>
    /// Кнопка паузы на игроке
    /// </summary>
    public class ButtonPlayerPause : MyButton
    {
        GameObject owner;
        
        public ButtonPlayerPause(Vector2 pos, GameObject obj) : base(pos)
        {            
            name = "btn_player_pause";
            Width = 64;
            Height = 64;
            
            delay = 500;
            owner = obj;
            cmd = new CommandPauseWeaponMenu();
        }
        
        public override Rectangle GetRect()
        {
            // тк кнопка движется вместе с объектом, необходимо суммировать координаты владельца
            return new Rectangle(
                (int)(Position.X + owner.Position.X - Width / 2), 
                (int)(Position.Y + owner.Position.Y - Height / 2), 
                Width, 
                Height
            );
        }
    }

    /// <summary>
    /// Кнопка выбора оружия: пушка
    /// </summary>
    public class ButtonCannon : MyButton
    {
        public ButtonCannon(Vector2 pos) : base(pos)
        {
            name = "btn_cannon";
            Width = 64;
            Height = 64;
            
            cmd = new CommandChWeaponCannon();
        }
    }

    /// <summary>
    /// Кнопка выбора оружия: пулемет
    /// </summary>
    public class ButtonMinigun : MyButton
    {
        public ButtonMinigun(Vector2 pos) : base(pos)
        {
            name = "btn_minigun";
            Width = 64;
            Height = 64;
            
            cmd = new CommandChWeaponMinigun();
        }
    }

    /// <summary>
    /// Кнопка выбора оружия: лазер
    /// </summary>
    public class ButtonLaser : MyButton
    {
        public ButtonLaser(Vector2 pos) : base(pos)
        {
            name = "btn_laser";
            Width = 64;
            Height = 64;
            
            cmd = new CommandChWeaponLaser();
        }
    }

    /// <summary>Кнопка выбора оружия «разлёт» (доступно после покупки в магазине).</summary>
    public class ButtonSpread : MyButton
    {
        public ButtonSpread(Vector2 pos) : base(pos)
        {
            name = "btn_spread";
            Width = 64;
            Height = 64;
            cmd = new CommandChWeaponSpread();
        }
    }

    /// <summary>Кнопка смены оружия по id (см. WeaponConfig). Иконку задаёт создатель кнопки.</summary>
    public class ButtonChWeapon : MyButton
    {
        public ButtonChWeapon(Vector2 pos, string weaponId) : base(pos)
        {
            name = "btn_weapon_" + weaponId;
            cmd = new CommandChWeapon(weaponId);
        }
    }

    /// <summary>
    /// Кнопка улучшения здоровья
    /// </summary>
    public class ButtonHpUp : MyButton
    {
        public ButtonHpUp(Vector2 pos) : base(pos)
        {
            name = "btn_hp_up";
            Width = 64;
            Height = 64;

            cmd = new CommandHpUp();
        }
    }

    /// <summary>
    /// Тестовая кнопка: следующий уровень
    /// </summary>
    public class ButtonNextLevel : MyButton
    {
        public ButtonNextLevel(Vector2 pos) : base(pos)
        {
            name = "btn_next_level";
            Width = 64;
            Height = 64;

            cmd = new CommandNextLevel();
        }
    }
}