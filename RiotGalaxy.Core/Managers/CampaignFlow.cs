using Microsoft.Xna.Framework;

namespace RiotGalaxy.Core.Managers
{
    /// <summary>
    /// Поток кампании: старт/продолжение/рестарт, прогон шагов миссии (брифинг → бой → босс →
    /// магазин), зачистка боя и банк кредитов, финал. Вынесено из GameManager.
    ///
    /// Зависимость от GameManager — явная ссылка (экраны/диалоги/магазин/игрок принадлежат ему);
    /// публичные методы кампании в GameManager остались фасадами — call-sites не меняются.
    /// </summary>
    public class CampaignFlow
    {
        private readonly GameManager _gm;
        private readonly MissionDirector _mission;
        private readonly LevelDirector _levels;

        private float _levelClearTimer; // «окно сбора звёзд» после зачистки боя

        public CampaignFlow(GameManager gm, MissionDirector mission, LevelDirector levels)
        {
            _gm = gm;
            _mission = mission;
            _levels = levels;
        }

        /// <summary>Начать кампанию с начала (меню/рестарт после Victory).</summary>
        public void StartCampaign()
        {
            try
            {
                Utils.SaveData.ClearCheckpoint(); // новая игра с начала — старый чекпоинт не нужен
                _gm.ResetPlayerAndScene();
                _mission.StartCampaign();
                RunNextStep();
            }
            catch (System.Exception ex)
            {
                Utils.Log.Error($"Error starting campaign: {ex.Message}");
            }
        }

        /// <summary>Продолжить с чекпоинта (миссия/волна); нет чекпоинта — с начала.</summary>
        public void ContinueCampaign()
        {
            try
            {
                _gm.ResetPlayerAndScene();
                if (Utils.SaveData.HasCheckpoint &&
                    _mission.ResumeAt(Utils.SaveData.CampaignMission, Utils.SaveData.CampaignStep))
                {
                    if (_gm.Player != null) _gm.Player.Score = Utils.SaveData.CampaignScore;
                }
                else
                {
                    _mission.StartCampaign();
                }
                RunNextStep();
            }
            catch (System.Exception ex)
            {
                Utils.Log.Error($"Error continuing campaign: {ex.Message}");
            }
        }

        /// <summary>Рестарт ТЕКУЩЕЙ миссии с первого шага (после гибели игрока).</summary>
        public void RestartMission()
        {
            try
            {
                _gm.ResetPlayerAndScene();
                _mission.RestartMission();
                RunNextStep();
            }
            catch (System.Exception ex)
            {
                Utils.Log.Error($"Error restarting mission: {ex.Message}");
            }
        }

        /// <summary>DEV: старт с первого шага миссии missionIndex.</summary>
        public void DevStartMission(int missionIndex)
        {
            try
            {
                _gm.ResetPlayerAndScene();
                if (!_mission.ResumeAt(missionIndex, 0))
                    _mission.StartCampaign();
                RunNextStep();
            }
            catch (System.Exception ex)
            {
                Utils.Log.Error($"Error dev-start mission {missionIndex}: {ex.Message}");
            }
        }

        /// <summary>DEV: старт сразу с шага-босса миссии (нет босса — с начала миссии).</summary>
        public void DevStartMissionAtBoss(int missionIndex)
        {
            try
            {
                _gm.ResetPlayerAndScene();
                if (!_mission.ResumeAtBoss(missionIndex) && !_mission.ResumeAt(missionIndex, 0))
                    _mission.StartCampaign();
                RunNextStep();
            }
            catch (System.Exception ex)
            {
                Utils.Log.Error($"Error dev-start boss {missionIndex}: {ex.Message}");
            }
        }

        /// <summary>
        /// Тик боя: «окно сбора звёзд» после зачистки → следующий шаг миссии.
        /// Вызывается из GameManager.UpdateGameplay (только в состоянии Playing).
        /// </summary>
        public void Update(float dt)
        {
            if (_levelClearTimer > 0f)
            {
                _levelClearTimer -= dt;
                if (_levelClearTimer <= 0f || !_gm.GameObjects.Exists(o => o is GameObjects.BonusStar))
                    OnBattleCleared();
            }
            else if (_levels.LevelComplete)
            {
                _levelClearTimer = Utils.BonusConfig.Current.LevelClearCollectSeconds;
                MessageLog.Add(Utils.Loc.T("battle.cleared_collect"), Color.Gold);
            }
        }

        /// <summary>Выполнить следующий шаг миссии; конец кампании → Victory.</summary>
        private void RunNextStep()
        {
            var step = _mission.Advance(out _);
            if (step == null)
            {
                FinishCampaign(); // кампания пройдена
                return;
            }

            // Биом (небо/звёзды) по текущей миссии — на каждом шаге, идемпотентно. Так работает и
            // для dev-прыжка/«Продолжить» (там миссия задаётся через ResumeAt, без флага «старт»).
            _gm.ApplyBiomeForCurrentMission();

            switch (step.Kind)
            {
                case StepKind.Briefing:
                    _gm.PlayDialogueThen(step.Arg, RunNextStep); // после брифинга — следующий шаг
                    break;
                case StepKind.Battle:
                case StepKind.Boss:
                    EnterBattle(step.Arg);
                    break;
                case StepKind.Shop:
                    BankCurrency();                 // зафиксировать заработок перед тратой
                    _gm.OpenShopThen(RunNextStep);
                    break;
            }
        }

        /// <summary>Загрузить и начать бой миссии (общий путь для battle/boss-шагов).</summary>
        private void EnterBattle(string battleName)
        {
            _gm.ClearNonPlayerObjects();
            _gm.Player?.ApplyUpgrades();             // покупки из магазина вступают в силу
            _levels.LoadBattle(battleName, _gm.ScreenWidth, _gm.ScreenHeight);
            _levelClearTimer = 0f;
            Barks.Reset();                            // барки пилота — с чистого листа на каждый бой
            Barks.Fire("battleStart");

            // Чекпоинт «последней волны» — чтобы «Продолжить» возобновляло именно этот бой.
            Utils.SaveData.SetCheckpoint(_mission.MissionIndex, _mission.StepIndex, _gm.Player?.Score ?? 0);

            if (_gm.CurrentGameState != GameManager.GameState.Playing)
                _gm.ChangeGameState(GameManager.GameState.Playing); // показать игровой экран
        }

        /// <summary>Кампания пройдена: зафиксировать счёт/рекорд и кредиты, экран победы.</summary>
        private void FinishCampaign()
        {
            if (_gm.Player != null)
                _gm.SetLastScore(_gm.Player.Score);
            BankCurrency();                        // кредиты уже забанкованы в shop-шаге; на всякий случай
            Utils.SaveData.ReportScore(_gm.LastScore);
            Utils.SaveData.ClearCheckpoint();      // «Продолжить» больше не нужно (Save внутри)
            _gm.ChangeGameState(GameManager.GameState.Victory);
        }

        /// <summary>Бой зачищен: начислить бонус, забанковать кредиты, следующий шаг.</summary>
        public void OnBattleCleared()
        {
            _levelClearTimer = 0f;
            if (_gm.Player != null)
            {
                var bc = Utils.BonusConfig.Current;
                int clearBonus = bc.LevelClearBonusBase + bc.LevelClearBonusPerLevel * _levels.CurrentBattle;
                _gm.Player.Currency += clearBonus;
                MessageLog.Add(Utils.Loc.F("battle.cleared_bonus", clearBonus), Color.Gold);
            }
            Barks.Fire("waveCleared");
            BankCurrency();
            RunNextStep();
        }

        /// <summary>Перевести заработанные кредиты игрока в профиль (для магазина/сейва).</summary>
        public void BankCurrency()
        {
            if (_gm.Player == null) return;
            Utils.SaveData.Currency += _gm.Player.Currency;
            _gm.Player.Currency = 0;
            Utils.SaveData.Save();
        }
    }
}
