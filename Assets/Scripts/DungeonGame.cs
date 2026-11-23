using System;
using System.Collections.Generic;

namespace Dungeons2087
{
    public enum Area { Title, Infirmary, City, Mine, Market, Robot, Bank, Armory, Food, Combat, Dead }

    public sealed class GameAction
    {
        public GameAction(string id, string key, string label, string detail)
        {
            Id = id;
            Key = key;
            Label = label;
            Detail = detail;
        }

        public string Id { get; }
        public string Key { get; }
        public string Label { get; }
        public string Detail { get; }
    }

    public sealed partial class DungeonGame
    {
        private enum Tutorial { None, MeetReception, EnterMine, MineSilver, OpenBag, CloseBag }
        private enum InputMode { None, RobotName }
        private enum Ore { Silver, Gold, Diamond }

        private static readonly (string Speaker, string Text)[] Opening =
        {
            ("小帅", "慢点起来。巡逻队在地表的检修隧道里发现了你。"),
            ("你", "我记得警报响了……上面还有人吗？"),
            ("小帅", "撤下来的人都在这座地下城。隔离门外还有感染者，巡逻队暂时守着入口。"),
            ("小帅", "先去矿区试着采块矿。熟悉这里后，去主城区的委托终端看看。")
        };

        private readonly Func<int, int, int> roll;
        private readonly List<GameAction> actions = new List<GameAction>();
        private readonly int[] haul = new int[3];
        private Tutorial tutorial;
        private InputMode inputMode;
        private int storyIndex = -1;
        private bool choosingMining;
        private bool mildHungerArmed = true;
        private bool severeHungerArmed = true;
        private int batchRolls;
        private float rollElapsed;

        public DungeonGame() : this(null) { }

        public DungeonGame(Func<int, int, int> inclusiveRoll)
        {
            var random = new Random();
            roll = inclusiveRoll ?? ((minimum, maximum) => random.Next(minimum, maximum + 1));
            Reset();
            Publish();
        }

        public Area Area { get; private set; }
        public string Speaker { get; private set; } = "系统";
        public string Message { get; private set; } = "选择新的旅程。";
        public string RobotName { get; private set; } = "拾矿者";
        public string LastEffect { get; private set; } = "";
        public int EffectSerial { get; private set; }
        public string ShortNotice { get; private set; } = "";
        public int NoticeSerial { get; private set; }
        public bool InBag { get; private set; }
        public bool IsMining { get; private set; }
        public bool IsFighting { get; private set; }
        public bool IsRealTimeCombat => IsFighting;
        public bool HasDialogue => storyIndex >= 0;
        public bool IsChoosingMining => choosingMining;
        public bool HasRobot { get; private set; }
        public int Silver { get; private set; }
        public int Gold { get; private set; }
        public int Diamond { get; private set; }
        public int Capsules { get; private set; }
        public int BloodBottles { get; private set; }
        public int Health { get; private set; }
        public int MiningLevel { get; private set; }
        public int RobotLevel { get; private set; }
        public int WeaponLevel { get; private set; }
        public int ArmorLevel { get; private set; }
        public int MiningCount { get; private set; }
        public int MiningRemaining { get; private set; }
        public float Food { get; private set; }
        public int MiningMax => 10 * RobotLevel;
        public float MiningProgress => batchRolls == 0 ? 0f
            : Math.Min(1f, (batchRolls - MiningRemaining + rollElapsed / RobotSeconds) / batchRolls);
        public IReadOnlyList<GameAction> Actions => actions;
        public event Action Changed;

        public string Title => InBag ? "背包" : InContracts ? "避难所委托" : Area switch
        {
            Area.Title => "2087 地下城",
            Area.Infirmary => "医务室",
            Area.City => "主城区",
            Area.Mine => "矿区",
            Area.Market => "集市",
            Area.Robot => "机器人系统",
            Area.Bank => "银行",
            Area.Armory => "武器店",
            Area.Food => "食品店",
            Area.Combat => "战区",
            Area.Dead => "游戏结束",
            _ => "地下城"
        };

        public string Subtitle => InBag ? "随身物资" : InContracts ? ContractObjective : Area switch
        {
            Area.Title => "地下生存 · 俯视角冒险",
            Area.Infirmary => "地下城医疗站",
            Area.City => "地下避难所",
            Area.Mine => "手动采矿 · 自动采矿",
            Area.Market => "地下城贸易站",
            Area.Robot => "购置与升级",
            Area.Bank => "矿物兑换",
            Area.Armory => "枪械与护甲升级",
            Area.Food => "生存补给",
            Area.Combat => IsFighting ? "遭遇感染者" : "巡逻部队前线",
            Area.Dead => "生存记录终止",
            _ => ""
        };

        public string Hint
        {
            get
            {
                if (IsFighting)
                    return "鼠标瞄准射击，空格闪避。";
                if (tutorial == Tutorial.MeetReception && !HasDialogue)
                    return "走到小帅身边，按 E 交谈。";
                if (tutorial == Tutorial.EnterMine)
                    return "走到左侧矿区入口，按 E 进入。";
                if (tutorial == Tutorial.MineSilver)
                    return "靠近发光矿脉，按 E 采集。";
                if (tutorial == Tutorial.OpenBag)
                    return "按 B 打开背包。";
                if (IsMining)
                    return $"{RobotName} 正在工作 · 剩余 {MiningRemaining} 次采集";
                if (choosingMining && !InBag)
                    return $"选择挖矿次数 1–{MiningMax} · 当前 {MiningCount} 次";
                if (InBag)
                    return "食物胶囊回复 10 点饱食度，血瓶回复 10 点生命。";
                if (InContracts)
                    return ContractStatus;
                if (Area == Area.Robot)
                    return MiningRates;
                if (Area == Area.Armory)
                    return $"AK-47 攻击力 {AttackPower} · 护甲防御力 {ArmorLevel - 1}";
                if (Area == Area.Bank)
                    return "每次成功兑换消耗 1 点饱食度。";
                return "";
            }
        }

        public string InputPrompt => inputMode switch
        {
            InputMode.RobotName => "为机器人命名",
            _ => ""
        };

        public string InputDefault => inputMode == InputMode.RobotName ? RobotName : "";

        public int AttackPower => 7 + 2 * (WeaponLevel - 1);
        public int EnemyDamage => 6 - (ArmorLevel - 1);
        public const int CombatFoodRequirement = 60;
        private const int RobotPrice = 35;
        private const int MaxRobotLevel = 8;
        private int ManualUpgradeCost => MiningLevel + 1;
        private int RobotUpgradeCost => RobotLevel + 3;
        private double GoldThreshold => (0.1 + (RobotLevel - 1) * 0.01 + (MiningLevel - 1) * 0.02) * 10000;
        private double DiamondThreshold => (0.01 + (RobotLevel - 1) * 0.001 + (MiningLevel - 1) * 0.002) * 10000;
        private float RobotSeconds => (11 - RobotLevel) / 10f;
        private string MiningRates => $"银 {(10000 - GoldThreshold) / 100f:0.##}% · 金 {(GoldThreshold - DiamondThreshold) / 100f:0.##}% · 钻石 {DiamondThreshold / 100f:0.##}% · {RobotSeconds:0.0} 秒/块";

        public void Execute(string id)
        {
            if (id == "hbzs" && Area == Area.City && !InBag && !HasDialogue && tutorial == Tutorial.None)
            {
                EnableEasterEgg();
                Notice(Message);
                Publish();
                return;
            }

            if (!actions.Exists(action => action.Id == id))
                return;

            switch (id)
            {
                case "new": StartTutorial(); break;
                case "skip": Reset(); Enter(Area.City); break;
                case "restart": Reset(); break;
                case "next": NextStory(); break;
                case "back": Back(); break;
                case "mine-area": EnterMine(); break;
                case "mine": Mine(); break;
                case "bag": OpenBag(); break;
                case "contract": InContracts = true; break;
                case "claim-contract": ClaimContract(); break;
                case "market": Enter(Area.Market); break;
                case "robot": Enter(Area.Robot); break;
                case "bank": Enter(Area.Bank); break;
                case "armory": Enter(Area.Armory); break;
                case "food-shop": Enter(Area.Food); break;
                case "combat": EnterCombat(); break;
                case "buy-robot": BuyRobot(); break;
                case "upgrade-mining": UpgradeMining(false); break;
                case "upgrade-robot": UpgradeMining(true); break;
                case "upgrade-weapon": UpgradeEquipment(false); break;
                case "upgrade-armor": UpgradeEquipment(true); break;
                case "buy-capsule": BuyCapsule(); break;
                case "buy-bottle": BuyBottle(); break;
                case "eat": Consume(false); break;
                case "heal": Consume(true); break;
                case "silver-gold": Exchange(Ore.Silver, Ore.Gold, 10, 1); break;
                case "gold-silver": Exchange(Ore.Gold, Ore.Silver, 1, 10); break;
                case "gold-diamond": Exchange(Ore.Gold, Ore.Diamond, 10, 1); break;
                case "diamond-gold": Exchange(Ore.Diamond, Ore.Gold, 1, 10); break;
                case "silver-diamond": Exchange(Ore.Silver, Ore.Diamond, 100, 1); break;
                case "diamond-silver": Exchange(Ore.Diamond, Ore.Silver, 1, 100); break;
                case "robot-mine": OpenRobotMining(); break;
                case "mining-less": AdjustMining(-1); break;
                case "mining-more": AdjustMining(1); break;
                case "mining-less-ten": AdjustMining(-10); break;
                case "mining-more-ten": AdjustMining(10); break;
                case "mining-start": StartRobotMining(); break;
                default: return;
            }

            if (id is "mine" or "buy-capsule" or "buy-bottle" or "eat" or "heal"
                or "silver-gold" or "gold-silver" or "gold-diamond" or "diamond-gold" or "silver-diamond"
                or "diamond-silver" or "upgrade-mining" or "upgrade-robot" or "upgrade-weapon" or "upgrade-armor"
                || id == "combat" && !IsFighting
                || id == "robot-mine" && !choosingMining
                || id == "buy-robot" && inputMode == InputMode.None
                || id == "mining-start")
                Notice(Message);
            Publish();
        }

        public void Submit(string text)
        {
            text ??= "";
            switch (inputMode)
            {
                case InputMode.RobotName:
                    RobotName = NormalizeRobotName(text);
                    FinishRobotPurchase();
                    break;
                case InputMode.None:
                    Execute(text.ToLowerInvariant());
                    return;
            }
            Publish();
        }

        public void Tick(float seconds)
        {
            if (!IsMining || !(seconds > 0f) || float.IsInfinity(seconds))
                return;

            rollElapsed += seconds;
            if (rollElapsed + 0.000001f < RobotSeconds) return;

            do
            {
                rollElapsed = Math.Max(0f, rollElapsed - RobotSeconds);
                HandleRobotRoll();
            }
            while (IsMining && rollElapsed + 0.000001f >= RobotSeconds);
            Publish();
        }

        private void Reset()
        {
            Area = Area.Title;
            storyIndex = -1;
            tutorial = Tutorial.None;
            inputMode = InputMode.None;
            InBag = IsMining = IsFighting = HasRobot = false;
            choosingMining = false;
            ResetContracts();
            mildHungerArmed = severeHungerArmed = true;
            Silver = Gold = Diamond = Capsules = BloodBottles = 0;
            MiningLevel = RobotLevel = WeaponLevel = ArmorLevel = 1;
            MiningRemaining = batchRolls = 0;
            Array.Clear(haul, 0, haul.Length);
            rollElapsed = 0f;
            MiningCount = 5;
            Health = 20;
            Food = 100f;
            RobotName = "拾矿者";
            LastEffect = "";
            ShortNotice = "";
            Say("系统", "进入地下避难所。");
        }

        private void Enter(Area destination)
        {
            Area = destination;
            switch (destination)
            {
                case Area.City: Say("系统", "返回主城区。"); break;
                case Area.Mine: Say("矿工队长", "每次手动采集消耗 1 饱食度。"); break;
                case Area.Market: Say("集市老板", "走近摊位交易。"); break;
                case Area.Robot:
                    Say("机器人系统", HasRobot ? $"{RobotName} 已就绪。" : $"采矿机器人 · {RobotPrice} 银");
                    break;
                case Area.Bank: Say("银行行长", "这里可以兑换银、金与钻石。"); break;
                case Area.Armory: Say("武器店职员", "升级 AK-47 可以提高伤害，升级护甲可以减少受伤。"); break;
                case Area.Food: Say("食品店老板", "食物胶囊 5 银，血瓶 2 钻。买到的补给可以在背包里使用。"); break;
            }
        }

        private void StartTutorial()
        {
            Reset();
            Area = Area.Infirmary;
            tutorial = Tutorial.MeetReception;
        }

        private void NextStory()
        {
            storyIndex++;
            if (storyIndex < Opening.Length)
            {
                var line = Opening[storyIndex];
                Say(line.Speaker, line.Text);
                return;
            }

            storyIndex = -1;
            Enter(Area.City);
            tutorial = Tutorial.EnterMine;
        }

        private void EnterMine()
        {
            Enter(Area.Mine);
            if (tutorial == Tutorial.EnterMine) tutorial = Tutorial.MineSilver;
            else CheckVitals();
        }

        private void Mine()
        {
            if (tutorial == Tutorial.MineSilver)
            {
                AwardOre(Ore.Silver);
                tutorial = Tutorial.OpenBag;
                Say("矿工队长", "银 +1");
                return;
            }

            var ore = RollOre();
            AwardOre(ore);
            Food -= 1f;
            Say("矿工队长", $"{OreName(ore)} +1");
            CheckVitals();
        }

        private void OpenBag()
        {
            InBag = true;
            if (tutorial == Tutorial.OpenBag)
            {
                tutorial = Tutorial.CloseBag;
                Say("小帅", "收起背包后就可以自由探索。");
            }
            else Say("系统", $"银 {Silver} · 金 {Gold} · 钻石 {Diamond}");
        }

        private void Back()
        {
            if (inputMode == InputMode.RobotName)
            {
                FinishRobotPurchase();
                return;
            }
            if (InBag)
            {
                InBag = false;
                if (tutorial == Tutorial.CloseBag)
                {
                    tutorial = Tutorial.None;
                    Notice("教程完成　可到主城区查看巡逻委托");
                }
                else
                {
                    Say("系统", "已收起背包。");
                    if (Area == Area.City || Area == Area.Mine) CheckVitals();
                }
                return;
            }
            if (InContracts)
            {
                InContracts = false;
                return;
            }
            if (choosingMining)
            {
                choosingMining = false;
                Say("矿工队长", "已关闭机器人采矿设置。");
                return;
            }
            if (Area == Area.Mine || Area == Area.Market)
            {
                Enter(Area.City);
                CheckVitals();
                return;
            }
            Enter(Area.Market);
        }

        private void BuyRobot()
        {
            if (HasRobot || Silver < RobotPrice)
            {
                Say("机器人系统", HasRobot ? "已拥有采矿机器人" : $"需要 {RobotPrice} 银");
                return;
            }
            Silver -= RobotPrice;
            HasRobot = true;
            TrackContract(ContractKind.Robot);
            inputMode = InputMode.RobotName;
            Say("机器人系统", "购买成功！给你的机器人起个名字吧。");
        }

        private void FinishRobotPurchase()
        {
            inputMode = InputMode.None;
            Say("机器人系统", $"{RobotName} 已就绪");
            Notice(Message);
        }

        private void UpgradeMining(bool robot)
        {
            var level = robot ? RobotLevel : MiningLevel;
            var maximum = robot ? MaxRobotLevel : 10;
            var cost = robot ? RobotUpgradeCost : ManualUpgradeCost;
            var label = robot ? "机器人采矿" : "基础挖矿";
            if (level >= maximum)
            {
                Say("机器人系统", $"{label}已升至满级 Lv.{maximum}。");
                return;
            }
            if (Gold < cost)
            {
                Say("机器人系统", $"升级{label}需要 {cost} 金。");
                return;
            }
            Gold -= cost;
            if (robot) RobotLevel++;
            else MiningLevel++;
            Say("机器人系统", $"{label}提升至 Lv.{level + 1}。");
        }

        private void UpgradeEquipment(bool armor)
        {
            var level = armor ? ArmorLevel : WeaponLevel;
            var maximum = armor ? 5 : 10;
            var label = armor ? "护甲" : "枪械";
            if (level >= maximum)
            {
                Say("武器店职员", $"{label}已升至满级 Lv.{maximum}。");
                return;
            }
            if (Diamond < level)
            {
                Say("武器店职员", $"升级{label}需要 {level} 钻。");
                return;
            }
            Diamond -= level;
            if (armor) ArmorLevel++;
            else WeaponLevel++;
            Say("武器店职员", $"{label}提升至 Lv.{level + 1}。");
        }

        private void BuyCapsule()
        {
            const int price = 5;
            if (Capsules == int.MaxValue)
            {
                Say("食品店老板", "背包里的食物胶囊已满。");
                return;
            }
            if (Silver < price)
            {
                Say("食品店老板", $"购买食物胶囊需要 {price} 银。");
                return;
            }
            Silver -= price;
            Capsules++;
            Say("食品店老板", "食物胶囊 +1");
        }

        private void BuyBottle()
        {
            if (BloodBottles == int.MaxValue)
            {
                Say("食品店老板", "背包里的血瓶已满。");
                return;
            }
            if (Diamond < 2)
            {
                Say("食品店老板", "购买血瓶需要 2 钻。");
                return;
            }
            Diamond -= 2;
            BloodBottles++;
            Say("食品店老板", "血瓶 +1");
        }

        private void Consume(bool heal)
        {
            if (heal ? BloodBottles == 0 : Capsules == 0)
            {
                Say("系统", heal ? "背包里没有血瓶。" : "背包里没有食物胶囊。");
                return;
            }
            if (heal)
            {
                BloodBottles--;
                Health = Math.Min(20, Health + 10);
                Say("系统", $"已使用血瓶。生命 {Health}/20。");
            }
            else
            {
                Capsules--;
                Food = Math.Min(100f, Food + 10f);
                Say("系统", $"已食用胶囊。饱食度 {Food:0.##}/100。");
            }
            Effect("food");
        }

        private void Exchange(Ore from, Ore to, int cost, int reward)
        {
            if (Currency(from) < cost)
            {
                Say("银行行长", $"货币不足，需要 {cost} {OreName(from)}。");
                return;
            }
            if (Currency(to) > int.MaxValue - reward)
            {
                Say("银行行长", $"{OreName(to)}的数量已达到上限。");
                return;
            }
            AddCurrency(from, -cost);
            AddCurrency(to, reward);
            Food -= 1f;
            Say("银行行长", $"{OreName(from)} −{cost}　{OreName(to)} +{reward}");
            CheckVitals();
        }

        private void OpenRobotMining()
        {
            if (!HasRobot)
            {
                Say("矿工队长", "请先到集市购买机器人。");
                return;
            }
            if (IsMining)
            {
                Say("机器人系统", "机器人正在工作");
                return;
            }
            choosingMining = true;
            Say("机器人系统", $"设置 {RobotName} 的采矿次数。");
        }

        private void AdjustMining(int delta) => MiningCount = Math.Max(1, Math.Min(MiningMax, MiningCount + delta));

        private void StartRobotMining()
        {
            if (Food <= (MiningCount + 1) * 0.5f)
            {
                Say("机器人系统", $"饱食度无法支持 {MiningCount} 次挖矿，请减少次数或补充食物。");
                return;
            }
            Array.Clear(haul, 0, haul.Length);
            // The original converts the requested count to N+1 before the first roll.
            batchRolls = MiningCount + 1;
            MiningRemaining = batchRolls;
            rollElapsed = 0f;
            choosingMining = false;
            IsMining = true;
            Say("机器人系统", $"{RobotName} 已开始工作。");
        }

        private void HandleRobotRoll()
        {
            var ore = RollOre();
            // The source grants 0.5 before the batch and charges it on the first roll.
            // Start charging on the second roll so the visible meter stays within its limit.
            if (MiningRemaining < batchRolls) Food -= 0.5f;
            MiningRemaining--;
            if (MiningRemaining == 0)
            {
                IsMining = false;
                rollElapsed = 0f;
                haul[(int)ore]++;
                Notice($"机器人已完成　{HaulSummary()}");
            }
            else haul[(int)ore]++;
            AwardOre(ore);
            if (!CheckVitals()) Notice(Message);
        }

        private string HaulSummary() => $"银 {haul[(int)Ore.Silver]} · 金 {haul[(int)Ore.Gold]} · 钻石 {haul[(int)Ore.Diamond]}";

        private Ore RollOre()
        {
            var sample = roll(1, 10000);
            if (sample <= DiamondThreshold) return Ore.Diamond;
            return sample <= GoldThreshold ? Ore.Gold : Ore.Silver;
        }

        private void AwardOre(Ore ore)
        {
            AddCurrency(ore, 1);
            TrackContract(ContractKind.Mining);
            Effect(ore switch
            {
                Ore.Gold => "mine-gold",
                Ore.Diamond => "mine-diamond",
                _ => "mine-silver"
            });
        }

        private static string OreName(Ore ore) => ore switch
        {
            Ore.Gold => "金",
            Ore.Diamond => "钻石",
            _ => "银"
        };

        private int Currency(Ore ore) => ore switch
        {
            Ore.Gold => Gold,
            Ore.Diamond => Diamond,
            _ => Silver
        };

        private void AddCurrency(Ore ore, int amount)
        {
            switch (ore)
            {
                case Ore.Silver: Silver = AddCounter(Silver, amount); break;
                case Ore.Gold: Gold = AddCounter(Gold, amount); break;
                case Ore.Diamond: Diamond = AddCounter(Diamond, amount); break;
            }
        }

        private void EnterCombat()
        {
            if (Food < CombatFoodRequirement)
            {
                Say("巡逻部队", $"需要 {CombatFoodRequirement} 饱食度");
                return;
            }
            Area = Area.Combat;
            IsFighting = true;
            Notice("清理战区　左键射击 / 空格闪避");
        }

        public void TakeRealTimeHit(int damage)
        {
            if (!IsFighting || damage <= 0) return;

            int received = Math.Max(1, damage - (ArmorLevel - 1));
            Health -= received;
            Food -= Math.Min(received, 3);
            Say("巡逻部队", $"受伤 −{received} 生命");
            if (CheckVitals()) Effect("hit");
            Notice(Message);
            Publish();
        }

        public void CompleteRealTimeCombat()
        {
            if (!IsFighting) return;

            IsFighting = false;
            int reward = roll(1, 1 + WeaponLevel / 2);
            Diamond = AddCounter(Diamond, reward);
            Silver = AddCounter(Silver, 12);
            CombatVictories = AddCounter(CombatVictories, 1);
            TrackContract(ContractKind.Combat);
            Food -= 8f;
            Enter(Area.City);
            Notice($"战区清理完成　钻石 +{reward}　银 +12");
            Effect("victory");
            CheckVitals();
            Publish();
        }

        private bool CheckVitals()
        {
            if (Food > 80f) mildHungerArmed = true;
            if (Food > 20f) severeHungerArmed = true;
            if (Food <= 20f && severeHungerArmed)
            {
                severeHungerArmed = false;
                Message += " 请尽快补充食物。";
            }
            else if (Food <= 80f && mildHungerArmed)
            {
                mildHungerArmed = false;
                Message += " 你肚子有点饿了。";
            }

            if (Food <= 0f)
            {
                Food = 0f;
                Health = 0;
            }
            if (Health > 0) return true;

            Health = 0;
            IsFighting = IsMining = InBag = InContracts = choosingMining = false;
            Area = Area.Dead;
            Say("系统", Food == 0f ? "饱食度耗尽，你没能在地下城活下来。" : "生命耗尽，你没能在地下城活下来。");
            Effect("death");
            return false;
        }

        private void EnableEasterEgg()
        {
            HasRobot = true;
            if (!IsMining) MiningLevel = RobotLevel = 1;
            TrackContract(ContractKind.Robot);
            Silver = Gold = Diamond = 99999;
            Food = 10f;
            RobotName = "测试";
            Say("系统", "开启彩蛋。测试机器人已就绪，记得先补充食物。");
            CheckVitals();
        }

        private void Say(string speaker, string message)
        {
            Speaker = speaker;
            Message = message;
        }

        private void Notice(string text)
        {
            ShortNotice = text;
            NoticeSerial++;
        }
        private void Effect(string name)
        {
            LastEffect = name;
            EffectSerial++;
        }

        private void Add(string id, string key, string label, string detail = "") => actions.Add(new GameAction(id, key, label, detail));

        private void Publish()
        {
            actions.Clear();
            BuildActions();
            Changed?.Invoke();
        }

        private void BuildActions()
        {
            if (inputMode != InputMode.None)
            {
                Add("confirm", "ENTER", "确认");
                Add("back", "E", inputMode == InputMode.RobotName ? "使用默认名称" : "返回标题");
                return;
            }
            if (HasDialogue)
            {
                Add("next", "ENTER", "继续");
                return;
            }
            if (tutorial != Tutorial.None)
            {
                switch (tutorial)
                {
                    case Tutorial.MeetReception: Add("next", "E", "与小帅交谈"); return;
                    case Tutorial.EnterMine: Add("mine-area", "E", "进入矿区"); return;
                    case Tutorial.MineSilver: Add("mine", "E", "采集矿物"); return;
                    case Tutorial.OpenBag: Add("bag", "B", "打开背包"); return;
                }
            }
            if (InBag)
            {
                Add("eat", "1", "食物胶囊", $"×{Capsules}　饱食度 +10");
                Add("heal", "2", "血瓶", $"×{BloodBottles}　生命 +10");
                Add("back", "Esc", "收起背包");
                return;
            }
            if (InContracts)
            {
                if (ContractReady) Add("claim-contract", "", "领取委托奖励", ContractReward);
                Add("back", "Esc", "关闭");
                return;
            }
            if (choosingMining)
            {
                Add("mining-start", "ENTER", $"开始挖矿 · {MiningCount} 次", $"饱食度 −{MiningCount * 0.5f:0.##}");
                Add("mining-less", "−", "减少 1 次");
                Add("mining-more", "+", "增加 1 次");
                Add("mining-less-ten", "↓", "减少 10 次");
                Add("mining-more-ten", "↑", "增加 10 次");
                Add("back", "Esc", "返回矿区");
                return;
            }

            switch (Area)
            {
                case Area.Title:
                    Add("new", "ENTER", "开始游戏");
                    Add("skip", "J", "跳过教程");
                    break;
                case Area.City:
                    Add("mine-area", "E", "进入矿区");
                    Add("market", "E", "进入集市");
                    Add("combat", "E", "进入战区", Food < CombatFoodRequirement ? $"需要 {CombatFoodRequirement} 饱食度" : "");
                    Add("contract", "E", "查看委托");
                    AddStatusActions();
                    break;
                case Area.Mine:
                    Add("mine", "E", "采集矿物", "饱食度 −1");
                    Add("robot-mine", "E", "机器人挖矿", HasRobot ? "" : "在集市购买机器人");
                    AddStatusActions();
                    Add("back", "E", "返回主城区");
                    break;
                case Area.Market:
                    Add("robot", "E", "机器人商店");
                    Add("bank", "E", "银行");
                    Add("armory", "E", "武器商店");
                    Add("food-shop", "E", "补给商店");
                    AddStatusActions();
                    Add("back", "E", "返回主城区");
                    break;
                case Area.Robot:
                    if (!HasRobot) Add("buy-robot", "", "购买机器人", $"{RobotPrice} 银");
                    Add("upgrade-mining", "", "升级基础挖矿", UpgradeDetail(MiningLevel, 10, ManualUpgradeCost, "金"));
                    if (HasRobot) Add("upgrade-robot", "", "升级机器人采矿", UpgradeDetail(RobotLevel, MaxRobotLevel, RobotUpgradeCost, "金"));
                    Add("back", "Esc", "关闭");
                    break;
                case Area.Bank:
                    Add("silver-gold", "G", "银换金", "10 银 → 1 金");
                    Add("gold-silver", "GB", "金换银", "1 金 → 10 银");
                    Add("gold-diamond", "D", "金换钻石", "10 金 → 1 钻");
                    Add("diamond-gold", "DB", "钻石换金", "1 钻 → 10 金");
                    Add("silver-diamond", "S", "银换钻石", "100 银 → 1 钻");
                    Add("diamond-silver", "SB", "钻石换银", "1 钻 → 100 银");
                    AddStatusActions();
                    Add("back", "Esc", "关闭");
                    break;
                case Area.Armory:
                    Add("upgrade-weapon", "A", "升级 AK-47", UpgradeDetail(WeaponLevel, 10, WeaponLevel, "钻"));
                    Add("upgrade-armor", "H", "升级护甲", UpgradeDetail(ArmorLevel, 5, ArmorLevel, "钻"));
                    AddStatusActions();
                    Add("back", "Esc", "关闭");
                    break;
                case Area.Food:
                    Add("buy-capsule", "J", "购买食物胶囊", "5 银 / 颗 · 饱食度 +10");
                    Add("buy-bottle", "X", "购买血瓶", "2 钻 / 个 · 生命 +10");
                    AddStatusActions();
                    Add("back", "Esc", "关闭");
                    break;
                case Area.Dead:
                    Add("restart", "ENTER", "返回标题");
                    break;
            }
        }

        private void AddStatusActions()
        {
            Add("bag", "B", "查看背包");
        }

        private static string UpgradeDetail(int level, int max, int price, string currency) => level >= max
            ? $"Lv.{level}/{max} · 已满级"
            : $"Lv.{level}/{max} · {price} {currency}";
    }
}
