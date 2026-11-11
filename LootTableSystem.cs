using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace GridLocked
{
    /// <summary>
    /// A single item drop rule in a loot table with weight, probability, and stack count range.
    /// </summary>
    public sealed class LootDropEntry
    {
        public string ItemId { get; set; } = string.Empty;
        public int Weight { get; set; } = 10;
        public double DropChance { get; set; } = 100.0; // 0..100%
        public int MinCount { get; set; } = 1;
        public int MaxCount { get; set; } = 1;

        public LootDropEntry() { }

        public LootDropEntry(string itemId, int weight = 10, double dropChance = 100.0, int minCount = 1, int maxCount = 1)
        {
            ItemId = itemId;
            Weight = weight;
            DropChance = dropChance;
            MinCount = minCount;
            MaxCount = maxCount;
        }
    }

    /// <summary>
    /// Represents a collection of possible drops rolled for an encounter, chest, or dungeon floor.
    /// </summary>
    public sealed class LootTable
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string DungeonId { get; set; } = string.Empty;
        public int LevelNumber { get; set; } = -1; // -1 = all levels in dungeon
        public int MinGold { get; set; } = 0;
        public int MaxGold { get; set; } = 15;
        public int Rolls { get; set; } = 1;
        public List<LootDropEntry> Entries { get; } = new List<LootDropEntry>();

        public LootTable() { }

        public LootTable(string id, string name, string dungeonId = "", int levelNum = -1)
        {
            Id = id;
            Name = name;
            DungeonId = dungeonId;
            LevelNumber = levelNum;
        }

        public void AddEntry(string itemId, int weight = 10, double chance = 100.0, int min = 1, int max = 1)
        {
            Entries.Add(new LootDropEntry(itemId, weight, chance, min, max));
        }
    }

    /// <summary>
    /// Registry managing all project and dungeon-specific loot tables.
    /// </summary>
    public sealed class LootTableRegistry
    {
        private readonly Dictionary<string, LootTable> _tables = new Dictionary<string, LootTable>(StringComparer.OrdinalIgnoreCase);

        public LootTableRegistry()
        {
            RegisterDefaults();
        }

        public void RegisterDefaults()
        {
            if (_tables.Count > 0) return;

            var d1Common = new LootTable("dungeon_01_common", "Upper Crypts - Common Loot", "dungeon_01", 1)
            {
                MinGold = 2,
                MaxGold = 12,
                Rolls = 1
            };
            d1Common.AddEntry("potion_healing", 30, 75.0, 1, 2);
            d1Common.AddEntry("scroll_light", 20, 50.0, 1, 1);
            d1Common.AddEntry("dagger_iron", 15, 30.0, 1, 1);
            d1Common.AddEntry("generic_weapon_sword", 10, 20.0, 1, 1);
            _tables[d1Common.Id] = d1Common;

            var d1Boss = new LootTable("crypt_boss_loot", "Upper Crypts - Boss Relic Hoard", "dungeon_01", 1)
            {
                MinGold = 50,
                MaxGold = 150,
                Rolls = 2
            };
            d1Boss.AddEntry("sword_iron", 40, 100.0, 1, 1);
            d1Boss.AddEntry("shield_bronze", 30, 80.0, 1, 1);
            d1Boss.AddEntry("potion_healing", 50, 100.0, 2, 4);
            _tables[d1Boss.Id] = d1Boss;

            var undeadDrop = new LootTable("undead_archetype_drops", "Undead - Skeletal Remains", "", -1)
            {
                MinGold = 0,
                MaxGold = 5,
                Rolls = 1
            };
            undeadDrop.AddEntry("potion_healing", 15, 25.0, 1, 1);
            _tables[undeadDrop.Id] = undeadDrop;
        }

        public void Register(LootTable table)
        {
            if (table != null && !string.IsNullOrEmpty(table.Id))
                _tables[table.Id] = table;
        }

        public bool Remove(string id)
        {
            if (string.IsNullOrEmpty(id)) return false;
            return _tables.Remove(id);
        }

        public LootTable Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _tables.TryGetValue(id, out var table);
            return table;
        }

        public IEnumerable<LootTable> GetAll()
        {
            return _tables.Values;
        }

        public IEnumerable<LootTable> GetForDungeon(string dungeonId)
        {
            foreach (var t in _tables.Values)
            {
                if (string.IsNullOrEmpty(t.DungeonId) || string.Equals(t.DungeonId, dungeonId, StringComparison.OrdinalIgnoreCase))
                    yield return t;
            }
        }

        public IEnumerable<LootTable> GetForLevel(string dungeonId, int levelNum)
        {
            foreach (var t in _tables.Values)
            {
                bool dMatch = string.IsNullOrEmpty(t.DungeonId) || string.Equals(t.DungeonId, dungeonId, StringComparison.OrdinalIgnoreCase);
                bool lMatch = t.LevelNumber < 0 || t.LevelNumber == levelNum;
                if (dMatch && lMatch) yield return t;
            }
        }

        public void LoadFromJson(string jsonText)
        {
            if (string.IsNullOrEmpty(jsonText)) return;
            var root = JsonLite.Parse(jsonText);
            if (root?.ArrayValue == null) return;

            foreach (var node in root.ArrayValue)
            {
                string id = node.GetString("id", "");
                if (string.IsNullOrEmpty(id)) continue;

                var table = new LootTable(id, node.GetString("name", id))
                {
                    DungeonId = node.GetString("dungeonId", ""),
                    LevelNumber = node.GetInt("levelNumber", -1),
                    MinGold = node.GetInt("minGold", 0),
                    MaxGold = node.GetInt("maxGold", 10),
                    Rolls = node.GetInt("rolls", 1)
                };

                var entriesNode = node.Get("entries");
                if (entriesNode?.ArrayValue != null)
                {
                    foreach (var en in entriesNode.ArrayValue)
                    {
                        string itemId = en.GetString("itemId", "");
                        if (!string.IsNullOrEmpty(itemId))
                        {
                            table.Entries.Add(new LootDropEntry(
                                itemId,
                                en.GetInt("weight", 10),
                                en.GetDouble("chance", 100.0),
                                en.GetInt("minCount", 1),
                                en.GetInt("maxCount", 1)
                            ));
                        }
                    }
                }

                _tables[id] = table;
            }
        }

        public string ToJson()
        {
            var sb = new StringBuilder();
            sb.AppendLine("[");
            int tableCount = 0;
            foreach (var t in _tables.Values)
            {
                if (tableCount++ > 0) sb.AppendLine(",");
                sb.AppendLine("  {");
                sb.AppendLine($"    \"id\": \"{t.Id.Replace("\"", "\\\"")}\",");
                sb.AppendLine($"    \"name\": \"{t.Name.Replace("\"", "\\\"")}\",");
                sb.AppendLine($"    \"dungeonId\": \"{t.DungeonId.Replace("\"", "\\\"")}\",");
                sb.AppendLine($"    \"levelNumber\": {t.LevelNumber},");
                sb.AppendLine($"    \"minGold\": {t.MinGold},");
                sb.AppendLine($"    \"maxGold\": {t.MaxGold},");
                sb.AppendLine($"    \"rolls\": {t.Rolls},");
                sb.AppendLine("    \"entries\": [");
                for (int i = 0; i < t.Entries.Count; i++)
                {
                    var e = t.Entries[i];
                    string comma = (i < t.Entries.Count - 1) ? "," : "";
                    sb.AppendLine($"      {{ \"itemId\": \"{e.ItemId}\", \"weight\": {e.Weight}, \"chance\": {e.DropChance:0.##}, \"minCount\": {e.MinCount}, \"maxCount\": {e.MaxCount} }}{comma}");
                }
                sb.AppendLine("    ]");
                sb.Append("  }");
            }
            sb.AppendLine();
            sb.AppendLine("]");
            return sb.ToString();
        }

        public void SaveToFile(string path)
        {
            string dir = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                Directory.CreateDirectory(dir);
            File.WriteAllText(path, ToJson());
        }
    }
}
