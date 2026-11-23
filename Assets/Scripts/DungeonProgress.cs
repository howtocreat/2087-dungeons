using System;

namespace Dungeons2087
{
    public sealed partial class DungeonGame
    {
        private enum ContractKind { Mining, Robot, Combat }

        private static readonly (string Name, string Task, ContractKind Kind, int Goal, int Silver, int Gold,
            int Diamond, int Capsules, int Bottles, string Reward)[] Contracts =
        {
            ("矿石储备", "采集任意矿物", ContractKind.Mining, 8, 16, 0, 0, 1, 0, "16 银 · 食物胶囊 ×1"),
            ("矿区增援", "购置采矿机器人", ContractKind.Robot, 1, 0, 5, 0, 0, 1, "5 金 · 血瓶 ×1"),
            ("隔离门外", "首次清理战区", ContractKind.Combat, 1, 0, 0, 2, 0, 1, "2 钻石 · 血瓶 ×1"),
            ("例行巡逻", "再清理一次战区", ContractKind.Combat, 1, 0, 2, 0, 1, 0, "2 金 · 食物胶囊 ×1")
        };

        private int contractIndex;
        private int contractProgress;

        public bool InContracts { get; private set; }
        public string ContractObjective => Contracts[contractIndex].Name;
        public string ContractStatus => $"{Contracts[contractIndex].Task}　{contractProgress}/{Contracts[contractIndex].Goal}";
        public string ContractReward => Contracts[contractIndex].Reward;
        public bool ContractReady => contractProgress >= Contracts[contractIndex].Goal;
        public int ContractsCompleted { get; private set; }
        public int CombatVictories { get; private set; }

        public DungeonSnapshot CaptureSnapshot()
        {
            Area safeArea = Area switch
            {
                Area.Infirmary or Area.City or Area.Mine or Area.Market => Area,
                Area.Robot or Area.Bank or Area.Armory or Area.Food => Area.Market,
                Area.Combat => Area.City,
                _ => throw new InvalidOperationException("当前没有可保存的探索进度。")
            };
            return new DungeonSnapshot
            {
                Area = safeArea.ToString(),
                Tutorial = (int)(tutorial == Tutorial.CloseBag ? Tutorial.OpenBag : tutorial),
                StoryIndex = storyIndex,
                RobotName = RobotName,
                HasRobot = HasRobot,
                Silver = Silver,
                Gold = Gold,
                Diamond = Diamond,
                Capsules = Capsules,
                BloodBottles = BloodBottles,
                Health = Health,
                Food = Food,
                MiningLevel = MiningLevel,
                RobotLevel = RobotLevel,
                WeaponLevel = WeaponLevel,
                ArmorLevel = ArmorLevel,
                MiningCount = Math.Min(MiningCount, MiningMax),
                MiningRemaining = IsMining ? MiningRemaining : 0,
                BatchRolls = IsMining ? batchRolls : 0,
                HaulSilver = IsMining ? haul[(int)Ore.Silver] : 0,
                HaulGold = IsMining ? haul[(int)Ore.Gold] : 0,
                HaulDiamond = IsMining ? haul[(int)Ore.Diamond] : 0,
                RollElapsed = IsMining ? rollElapsed : 0f,
                IsMining = IsMining,
                ContractIndex = contractIndex,
                ContractProgress = contractProgress,
                ContractsCompleted = ContractsCompleted,
                CombatVictories = CombatVictories
            };
        }

        public void RestoreSnapshot(DungeonSnapshot snapshot)
        {
            if (snapshot == null) throw new ArgumentNullException(nameof(snapshot));

            Area location = snapshot.Area switch
            {
                nameof(Area.Infirmary) => Area.Infirmary,
                nameof(Area.City) => Area.City,
                nameof(Area.Mine) => Area.Mine,
                nameof(Area.Market) => Area.Market,
                _ => throw new ArgumentException("存档区域无效。", nameof(snapshot))
            };
            if (snapshot.ContractIndex < 0 || snapshot.ContractIndex >= Contracts.Length
                || snapshot.Tutorial < 0 || snapshot.Tutorial > (int)Tutorial.CloseBag
                || snapshot.StoryIndex < -1 || snapshot.StoryIndex >= Opening.Length
                || (snapshot.StoryIndex >= 0 && (location != Area.Infirmary || snapshot.Tutorial != (int)Tutorial.MeetReception)))
                throw new ArgumentException("存档剧情或委托进度无效。", nameof(snapshot));

            Reset();
            Enter(location);
            tutorial = (Tutorial)snapshot.Tutorial;
            if (tutorial == Tutorial.CloseBag) tutorial = Tutorial.OpenBag;
            storyIndex = snapshot.StoryIndex;
            RobotName = snapshot.RobotName;
            HasRobot = snapshot.HasRobot;
            Silver = snapshot.Silver;
            Gold = snapshot.Gold;
            Diamond = snapshot.Diamond;
            Capsules = snapshot.Capsules;
            BloodBottles = snapshot.BloodBottles;
            Health = snapshot.Health;
            Food = snapshot.Food;
            MiningLevel = snapshot.MiningLevel;
            RobotLevel = snapshot.RobotLevel;
            WeaponLevel = snapshot.WeaponLevel;
            ArmorLevel = snapshot.ArmorLevel;
            MiningCount = snapshot.MiningCount;
            MiningRemaining = snapshot.MiningRemaining;
            batchRolls = snapshot.BatchRolls;
            haul[(int)Ore.Silver] = snapshot.HaulSilver;
            haul[(int)Ore.Gold] = snapshot.HaulGold;
            haul[(int)Ore.Diamond] = snapshot.HaulDiamond;
            rollElapsed = snapshot.RollElapsed;
            IsMining = snapshot.IsMining;
            contractIndex = snapshot.ContractIndex;
            contractProgress = snapshot.ContractProgress;
            ContractsCompleted = snapshot.ContractsCompleted;
            CombatVictories = snapshot.CombatVictories;
            mildHungerArmed = Food > 80f;
            severeHungerArmed = Food > 20f;
            if (HasDialogue)
            {
                var line = Opening[storyIndex];
                Say(line.Speaker, line.Text);
            }
            Publish();
        }

        public void ReturnToTitle()
        {
            Reset();
            Publish();
        }

        private static string NormalizeRobotName(string input)
        {
            string name = input.Trim();
            if (name.Length > 14) name = name[..(char.IsHighSurrogate(name[13]) ? 13 : 14)];
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsHighSurrogate(name[i]) && i + 1 < name.Length && char.IsLowSurrogate(name[i + 1]))
                {
                    i++;
                    continue;
                }
                if (char.IsControl(name[i]) || char.IsSurrogate(name[i])) return "拾矿者";
            }
            return string.IsNullOrWhiteSpace(name) ? "拾矿者" : name;
        }

        private static int AddCounter(int value, int change) => (int)Math.Min(int.MaxValue, (long)value + change);

        private void ResetContracts()
        {
            contractIndex = contractProgress = ContractsCompleted = CombatVictories = 0;
            InContracts = false;
        }

        private void TrackContract(ContractKind kind)
        {
            if (Contracts[contractIndex].Kind == kind && !ContractReady) contractProgress++;
        }

        private void ClaimContract()
        {
            if (!ContractReady) return;

            var contract = Contracts[contractIndex];
            Silver = AddCounter(Silver, contract.Silver);
            Gold = AddCounter(Gold, contract.Gold);
            Diamond = AddCounter(Diamond, contract.Diamond);
            Capsules = AddCounter(Capsules, contract.Capsules);
            BloodBottles = AddCounter(BloodBottles, contract.Bottles);
            ContractsCompleted = AddCounter(ContractsCompleted, 1);
            contractIndex = Math.Min(contractIndex + 1, Contracts.Length - 1);
            contractProgress = ((contractIndex == 1 && HasRobot) || (contractIndex == 2 && CombatVictories > 0)) ? 1 : 0;
            Notice($"委托完成　{contract.Reward}");
            Effect("victory");
        }
    }
}
