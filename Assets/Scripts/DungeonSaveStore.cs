using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Dungeons2087
{
    public sealed class DungeonSaveStore
    {
        public const int SchemaVersion = 1;
        public const int MaxNameLength = 24;
        private const int MaxSaveBytes = 64 * 1024;
        private const string SaveSuffix = ".json";
        private const string BackupSuffix = ".json.bak";
        private static readonly Encoding SaveEncoding = new UTF8Encoding(false, true);

        public string Folder { get; }

        public DungeonSaveStore(string folder)
        {
            Folder = Path.GetFullPath(folder);
        }

        public IReadOnlyList<DungeonSaveEntry> List()
        {
            var entries = new List<DungeonSaveEntry>();
            if (!Directory.Exists(Folder)) return entries;

            var seen = new HashSet<string>(StringComparer.Ordinal);
            foreach (string path in Directory.EnumerateFiles(Folder, "*.json*"))
            {
                string fileName = Path.GetFileName(path);
                string id;
                if (fileName.EndsWith(BackupSuffix, StringComparison.Ordinal))
                    id = fileName[..^BackupSuffix.Length];
                else if (fileName.EndsWith(SaveSuffix, StringComparison.Ordinal))
                    id = fileName[..^SaveSuffix.Length];
                else
                    continue;

                if (!IsValidId(id) || !seen.Add(id)) continue;

                try
                {
                    entries.Add(Load(id));
                }
                catch (Exception error) when (error is InvalidDataException or IOException or UnauthorizedAccessException)
                {
                    // Skip unreadable slots so a damaged file cannot hide the rest of the list.
                }
            }

            entries.Sort((left, right) => string.CompareOrdinal(right.UpdatedUtc, left.UpdatedUtc));
            return entries;
        }

        public DungeonSaveEntry Create(string name, DungeonSnapshot snapshot)
        {
            name = name?.Trim();
            if (string.IsNullOrEmpty(name)) name = "新存档";
            var entry = NewEntry(Guid.NewGuid().ToString("N"), name, snapshot);
            Write(entry);
            return entry;
        }

        public DungeonSaveEntry Load(string id) => ReadLocal(id, out _);

        public DungeonSaveEntry Save(string id, DungeonSnapshot snapshot)
        {
            DungeonSaveEntry previous = ReadLocal(id, out bool recoveredFromBackup);
            var entry = NewEntry(previous.Id, previous.Name, snapshot);
            Write(entry, true, recoveredFromBackup);
            return entry;
        }

        public DungeonSaveEntry Import(string sourcePath)
        {
            if (string.IsNullOrWhiteSpace(sourcePath))
                throw new InvalidDataException("请选择要导入的存档文件。");

            DungeonSaveEntry source = ReadFile(sourcePath, null);
            return Create(source.Name, source.Snapshot);
        }

        private static DungeonSaveEntry NewEntry(string id, string name, DungeonSnapshot snapshot)
        {
            return new DungeonSaveEntry
            {
                SchemaVersion = SchemaVersion,
                Id = id,
                Name = name,
                UpdatedUtc = DateTime.UtcNow.ToString("O", CultureInfo.InvariantCulture),
                Snapshot = snapshot
            };
        }

        private DungeonSaveEntry ReadLocal(string id, out bool recoveredFromBackup)
        {
            string path = SlotPath(id);
            try
            {
                DungeonSaveEntry entry = ReadFile(path, id);
                recoveredFromBackup = false;
                return entry;
            }
            catch (Exception primaryError) when (IsRecoverable(primaryError))
            {
                try
                {
                    DungeonSaveEntry entry = ReadFile(path + ".bak", id);
                    recoveredFromBackup = true;
                    return entry;
                }
                catch (Exception backupError) when (IsRecoverable(backupError))
                {
                    throw new InvalidDataException("找不到可读取的存档。文件可能已移走或损坏。", primaryError);
                }
            }
        }

        private static bool IsRecoverable(Exception error) =>
            error is InvalidDataException or FileNotFoundException or DirectoryNotFoundException;

        private static DungeonSaveEntry ReadFile(string path, string expectedId)
        {
            string json;
            try
            {
                using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read | FileShare.Delete);
                if (file.Length == 0 || file.Length > MaxSaveBytes)
                    throw new InvalidDataException("存档文件为空或超出允许大小。");

                using var reader = new StreamReader(file, SaveEncoding);
                json = reader.ReadToEnd();
            }
            catch (DecoderFallbackException error)
            {
                throw new InvalidDataException("存档文件的文字编码无效。", error);
            }

            DungeonSaveEntry entry;
            try
            {
                entry = JsonUtility.FromJson<DungeonSaveEntry>(json);
            }
            catch (ArgumentException error)
            {
                throw new InvalidDataException("无法解析该存档文件。", error);
            }

            ValidateEntry(entry);
            if (expectedId != null && !string.Equals(entry.Id, expectedId, StringComparison.Ordinal))
                throw new InvalidDataException("存档编号与文件名不一致。");
            return entry;
        }

        private void Write(DungeonSaveEntry entry, bool replace = false, bool preserveBackup = false)
        {
            ValidateEntry(entry);
            Directory.CreateDirectory(Folder);
            string destination = SlotPath(entry.Id);
            string temporary = Path.Combine(Folder, "." + Guid.NewGuid().ToString("N") + ".tmp");
            byte[] bytes = SaveEncoding.GetBytes(JsonUtility.ToJson(entry, true));

            try
            {
                using (var stream = new FileStream(temporary, FileMode.CreateNew, FileAccess.Write, FileShare.None))
                {
                    stream.Write(bytes, 0, bytes.Length);
                    stream.Flush(true);
                }

                if (replace && File.Exists(destination))
                    File.Replace(temporary, destination, preserveBackup ? null : destination + ".bak");
                else
                    File.Move(temporary, destination);
            }
            finally
            {
                File.Delete(temporary);
            }
        }

        private string SlotPath(string id)
        {
            if (!IsValidId(id)) throw new InvalidDataException("存档编号无效。");
            return Path.Combine(Folder, id + SaveSuffix);
        }

        private static bool IsValidId(string id) =>
            Guid.TryParseExact(id, "N", out Guid value) && value != Guid.Empty && id == value.ToString("N");

        private static void ValidateEntry(DungeonSaveEntry entry)
        {
            if (entry == null || entry.SchemaVersion != SchemaVersion)
                throw new InvalidDataException("该文件不是当前版本的地下城存档。");

            if (!IsValidId(entry.Id) || !IsValidName(entry.Name, MaxNameLength))
                throw new InvalidDataException("存档名称或编号无效。");

            if (!DateTime.TryParseExact(entry.UpdatedUtc, "O", CultureInfo.InvariantCulture,
                    DateTimeStyles.RoundtripKind, out DateTime updatedUtc) || updatedUtc.Kind != DateTimeKind.Utc)
                throw new InvalidDataException("存档时间无效。");

            ValidateSnapshot(entry.Snapshot);
        }

        private static void ValidateSnapshot(DungeonSnapshot snapshot)
        {
            if (snapshot == null) throw new InvalidDataException("存档中缺少角色进度。");

            bool validLocation = snapshot.Area switch
            {
                "Infirmary" => snapshot.Tutorial == 1,
                "City" => snapshot.Tutorial is 0 or 2,
                "Mine" => snapshot.Tutorial is 0 or 3 or 4 or 5,
                "Market" => snapshot.Tutorial == 0,
                _ => false
            };
            if (!validLocation || snapshot.StoryIndex < -1 ||
                snapshot.StoryIndex >= 0 && (snapshot.Area != "Infirmary" || snapshot.StoryIndex > 3))
                throw new InvalidDataException("存档中的地点或剧情进度无效。");

            if (!InRange(snapshot.X, -100f, 100f) || !InRange(snapshot.Z, -100f, 100f) ||
                !InRange(snapshot.Yaw, -360f, 360f))
                throw new InvalidDataException("存档中的角色位置无效。");

            if (snapshot.Health < 1 || snapshot.Health > 20 || !InRange(snapshot.Food, float.Epsilon, 100f) ||
                !IsCounter(snapshot.Silver) || !IsCounter(snapshot.Gold) || !IsCounter(snapshot.Diamond) ||
                !IsCounter(snapshot.Capsules) || !IsCounter(snapshot.BloodBottles) ||
                snapshot.MiningLevel is < 1 or > 10 || snapshot.RobotLevel is < 1 or > 8 ||
                snapshot.WeaponLevel is < 1 or > 10 || snapshot.ArmorLevel is < 1 or > 5)
                throw new InvalidDataException("存档中的生命、资源或等级数据无效。");

            ValidateRobot(snapshot);

            if (snapshot.ContractIndex is < 0 or > 3 || snapshot.ContractProgress < 0 ||
                snapshot.ContractProgress > (snapshot.ContractIndex == 0 ? 8 : 1) ||
                !IsCounter(snapshot.ContractsCompleted) || !IsCounter(snapshot.CombatVictories))
                throw new InvalidDataException("存档中的委托进度无效。");
        }

        private static void ValidateRobot(DungeonSnapshot snapshot)
        {
            if (!IsValidName(snapshot.RobotName, 14) || !snapshot.HasRobot && snapshot.RobotLevel != 1 ||
                snapshot.MiningCount < 1 || snapshot.MiningCount > snapshot.RobotLevel * 10)
                throw new InvalidDataException("存档中的机器人或采矿设置无效。");

            if (!snapshot.IsMining)
            {
                if (snapshot.MiningRemaining == 0 && snapshot.BatchRolls == 0 && snapshot.RollElapsed == 0f &&
                    snapshot.HaulSilver == 0 && snapshot.HaulGold == 0 && snapshot.HaulDiamond == 0)
                    return;
            }
            else if (snapshot.HasRobot && snapshot.BatchRolls == snapshot.MiningCount + 1 &&
                snapshot.MiningRemaining > 0 && snapshot.MiningRemaining <= snapshot.BatchRolls &&
                InRange(snapshot.RollElapsed, 0f, 1f) &&
                snapshot.HaulSilver is >= 0 and <= 81 && snapshot.HaulGold is >= 0 and <= 81 &&
                snapshot.HaulDiamond is >= 0 and <= 81 &&
                snapshot.HaulSilver + snapshot.HaulGold + snapshot.HaulDiamond == snapshot.BatchRolls - snapshot.MiningRemaining)
            {
                return;
            }

            throw new InvalidDataException("存档中的机器人采矿任务无效。");
        }

        private static bool IsValidName(string name, int maxLength)
        {
            if (string.IsNullOrWhiteSpace(name) || name.Length > maxLength) return false;
            for (int i = 0; i < name.Length; i++)
            {
                if (char.IsControl(name[i]) || char.IsLowSurrogate(name[i])) return false;
                if (char.IsHighSurrogate(name[i]) && (++i >= name.Length || !char.IsLowSurrogate(name[i]))) return false;
            }
            return true;
        }

        private static bool IsCounter(int value) => value >= 0;

        private static bool InRange(float value, float minimum, float maximum) => value >= minimum && value <= maximum;
    }
}
