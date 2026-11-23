using System;

namespace Dungeons2087
{
    [Serializable]
    public sealed class DungeonSaveEntry
    {
        public int SchemaVersion;
        public string Id;
        public string Name;
        public string UpdatedUtc;
        public DungeonSnapshot Snapshot;
    }

    [Serializable]
    public sealed class DungeonSnapshot
    {
        public string Area;
        public int Tutorial;
        public int StoryIndex;
        public int Silver;
        public int Gold;
        public int Diamond;
        public int Capsules;
        public int BloodBottles;
        public int Health;
        public float Food;
        public int MiningLevel;
        public int RobotLevel;
        public int WeaponLevel;
        public int ArmorLevel;
        public bool HasRobot;
        public string RobotName;
        public bool IsMining;
        public int MiningCount;
        public int MiningRemaining;
        public int BatchRolls;
        public float RollElapsed;
        public int HaulSilver;
        public int HaulGold;
        public int HaulDiamond;
        public int ContractIndex;
        public int ContractProgress;
        public int ContractsCompleted;
        public int CombatVictories;
        public bool HasPosition;
        public float X;
        public float Z;
        public float Yaw;
    }
}
