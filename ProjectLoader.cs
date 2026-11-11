using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.IO.Compression;
using System.Text;

namespace GridLocked
{
    public sealed class DungeonLevel
    {
        public int LevelNumber { get; set; } = 1;
        public string LevelName { get; set; } = "Floor 1";
        public int Width { get; set; }
        public int Height { get; set; }
        public DungeonTile[,] Tiles { get; set; }
        public double SpawnX { get; set; } = 1.5;
        public double SpawnY { get; set; } = 1.5;
        public double SpawnAngle { get; set; } = 0.0;
        public ZoneConfig Config { get; set; } = new ZoneConfig();

        public List<MonsterInstance> Monsters { get; } = new List<MonsterInstance>();
        public List<LargeObjectInstance> LargeObjects { get; } = new List<LargeObjectInstance>();
        public List<TransitionObject> Transitions { get; } = new List<TransitionObject>();

        /// <summary>
        /// Converts the tile grid to a raw int[,] map for raycaster geometry (1 = Wall, 0 = Floor).
        /// </summary>
        public int[,] ToRaycasterMap()
        {
            var map = new int[Height, Width];
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    var t = Tiles[y, x];
                    if (t.Type == TileType.Wall)
                        map[y, x] = 1;
                    else if (t.Type == TileType.Spawn)
                        map[y, x] = 2;
                    else
                        map[y, x] = 0;
                }
            }
            return map;
        }
    }

    public sealed class AsciiLegend
    {
        private readonly Dictionary<char, TileArchetype> _archetypes = new Dictionary<char, TileArchetype>();

        public IReadOnlyDictionary<char, string> Mappings
        {
            get
            {
                var dict = new Dictionary<char, string>();
                foreach (var kvp in _archetypes) dict[kvp.Key] = kvp.Value.Id;
                return dict;
            }
        }

        public IReadOnlyDictionary<char, TileArchetype> Archetypes => _archetypes;

        public AsciiLegend()
        {
            SetDefaults();
        }

        public void SetDefaults()
        {
            _archetypes.Clear();
            AddDefault(' ', "floor", "Floor", TileType.Floor);
            AddDefault('.', "floor", "Floor", TileType.Floor);
            AddDefault('#', "wall", "Solid Wall", TileType.Wall);
            AddDefault('@', "spawn", "Player Spawn", TileType.Spawn);
            AddDefault('S', "stairs_up", "Stairs Up", TileType.StairsUp);
            AddDefault('s', "stairs_down", "Stairs Down", TileType.StairsDown);
            AddDefault('D', "door", "Wooden Door", TileType.Door);
            AddDefault('~', "void", "Void / Pit", TileType.VoidOrPit);
            AddDefault('>', "stairs_down", "Stairs Down", TileType.StairsDown);
            AddDefault('<', "stairs_up", "Stairs Up", TileType.StairsUp);

            // Extended Archetypes with interaction flags
            AddDefault('I', "pillar", "Stone Pillar", TileType.Floor);
            AddDefault('B', "boulder", "Heavy Boulder", TileType.Floor);
            AddDefault('G', "gate", "Iron Gate", TileType.Door);
            AddDefault('C', "cracked_wall", "Cracked Wall", TileType.Wall);
            AddDefault('L', "lava", "Lava Pool", TileType.Floor);
            AddDefault('P', "stone_pillar", "Stone Pillar", TileType.Floor);
            AddDefault('A', "altar_gold", "Golden Altar", TileType.Floor);
            AddDefault('T', "tree_oak", "Oak Tree", TileType.Floor);
        }

        private void AddDefault(char key, string id, string name, TileType type)
        {
            var arch = new TileArchetype(key, id, name, type);
            _archetypes[key] = arch;
        }

        public void Set(char c, string typeOrId)
        {
            if (_archetypes.TryGetValue(c, out var existing))
            {
                existing.Id = typeOrId;
                existing.Name = typeOrId;
                existing.ApplyDefaultsForType(existing.BaseType, typeOrId);
            }
            else
            {
                TileType type = TileType.Floor;
                string lower = (typeOrId ?? "").ToLowerInvariant();
                if (lower == "wall") type = TileType.Wall;
                else if (lower == "spawn") type = TileType.Spawn;
                else if (lower == "stairs_up") type = TileType.StairsUp;
                else if (lower == "stairs_down") type = TileType.StairsDown;
                else if (lower == "door") type = TileType.Door;
                else if (lower == "void" || lower == "pit") type = TileType.VoidOrPit;

                var arch = new TileArchetype(c, typeOrId, typeOrId, type);
                _archetypes[c] = arch;
            }
        }

        public void SetArchetype(TileArchetype arch)
        {
            if (arch != null)
                _archetypes[arch.Key] = arch;
        }

        public bool Remove(char c)
        {
            return _archetypes.Remove(c);
        }

        public string Resolve(char c)
        {
            if (_archetypes.TryGetValue(c, out var val))
                return val.Id;
            return "floor";
        }

        public TileArchetype GetArchetype(char c)
        {
            if (_archetypes.TryGetValue(c, out var val))
                return val;
            return new TileArchetype(c, "floor", "Floor", TileType.Floor);
        }

        public char FindKeyFor(string typeOrId, char fallback = ' ')
        {
            if (string.IsNullOrEmpty(typeOrId)) return fallback;
            foreach (var kvp in _archetypes)
            {
                if (kvp.Value.Id.Equals(typeOrId, StringComparison.OrdinalIgnoreCase) ||
                    kvp.Value.ObjectKind.Equals(typeOrId, StringComparison.OrdinalIgnoreCase))
                    return kvp.Key;
            }
            return fallback;
        }

        public void ParseJson(JsonNode node)
        {
            if (node == null) return;
            var target = node.Get("legend") ?? node.Get("keys") ?? node;
            if (target.ObjectValue == null) return;

            foreach (var kvp in target.ObjectValue)
            {
                if (kvp.Key.Length > 0)
                {
                    char c = kvp.Key[0];
                    if (kvp.Key.Equals("space", StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.Equals("[space]", StringComparison.OrdinalIgnoreCase) ||
                        kvp.Key.Equals("blank", StringComparison.OrdinalIgnoreCase))
                    {
                        c = ' ';
                    }
                    else if (kvp.Key.Equals("dot", StringComparison.OrdinalIgnoreCase))
                    {
                        c = '.';
                    }

                    if (kvp.Value.ObjectValue != null)
                    {
                        // Extended object definition with interaction flags
                        var obj = kvp.Value;
                        string id = obj.GetString("id", kvp.Key);
                        string name = obj.GetString("name", id);
                        string baseTypeStr = obj.GetString("baseType", "Floor");
                        Enum.TryParse<TileType>(baseTypeStr, true, out var bType);

                        var dmgNode = obj.Get("hazardDamage") ?? obj.Get("hazardEffect");
                        Damage hazardDmg = Damage.FromJson(dmgNode);

                        var arch = new TileArchetype(c, id, name, bType)
                        {
                            CanWalkThru = obj.GetBool("canWalkThru", bType == TileType.Floor || bType == TileType.Spawn),
                            CanWalkOver = obj.GetBool("canWalkOver", bType != TileType.VoidOrPit && bType != TileType.Wall),
                            BlocksVision = obj.GetBool("blocksVision", bType == TileType.Wall || bType == TileType.Door),
                            IsBreakable = obj.GetBool("breakable", false),
                            BreakHP = obj.GetInt("breakHP", obj.GetInt("hp", 50)),
                            IsMoveable = obj.GetBool("moveable", false),
                            CanBeOpened = obj.GetBool("canBeOpened", bType == TileType.Door),
                            StartsOpen = obj.GetBool("startsOpen", false),
                            IsHazardous = obj.GetBool("hazardous", bType == TileType.VoidOrPit),
                            HazardEffect = hazardDmg,
                            HazardDamage = hazardDmg != null ? (int)Math.Round(hazardDmg.Amount) : obj.GetInt("hazardDamage", 0),
                            ObjectKind = obj.GetString("objectKind", obj.GetString("sprite", string.Empty)),
                            Description = obj.GetString("description", string.Empty)
                        };
                        _archetypes[c] = arch;
                    }
                    else
                    {
                        // Standard flat string mapping
                        Set(c, kvp.Value.AsString());
                    }
                }
            }
        }

        public void LoadFromFile(string filePath)
        {
            if (!File.Exists(filePath)) return;
            var json = JsonLite.Parse(File.ReadAllText(filePath));
            if (json != null)
            {
                ParseJson(json);
            }
        }

        public void SaveToFile(string filePath)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("{");
            int i = 0;
            foreach (var kvp in _archetypes)
            {
                var a = kvp.Value;
                string escapedKey = kvp.Key == '\\' ? "\\\\" : (kvp.Key == '"' ? "\\\"" : (kvp.Key == ' ' ? "[space]" : kvp.Key.ToString()));

                sb.AppendLine($"  \"{escapedKey}\": {{");
                sb.AppendLine($"    \"id\": \"{a.Id}\",");
                sb.AppendLine($"    \"name\": \"{a.Name}\",");
                sb.AppendLine($"    \"baseType\": \"{a.BaseType}\",");
                sb.AppendLine($"    \"canWalkThru\": {(a.CanWalkThru ? "true" : "false")},");
                sb.AppendLine($"    \"canWalkOver\": {(a.CanWalkOver ? "true" : "false")},");
                sb.AppendLine($"    \"blocksVision\": {(a.BlocksVision ? "true" : "false")},");
                sb.AppendLine($"    \"breakable\": {(a.IsBreakable ? "true" : "false")},");
                sb.AppendLine($"    \"breakHP\": {a.BreakHP},");
                sb.AppendLine($"    \"moveable\": {(a.IsMoveable ? "true" : "false")},");
                sb.AppendLine($"    \"canBeOpened\": {(a.CanBeOpened ? "true" : "false")},");
                sb.AppendLine($"    \"startsOpen\": {(a.StartsOpen ? "true" : "false")},");
                sb.AppendLine($"    \"hazardous\": {(a.IsHazardous ? "true" : "false")},");
                if (a.HazardEffect != null)
                {
                    sb.AppendLine($"    \"hazardDamage\": {a.HazardEffect.ToJsonString(4).TrimStart()},");
                }
                else
                {
                    sb.AppendLine($"    \"hazardDamage\": {a.HazardDamage},");
                }
                sb.AppendLine($"    \"objectKind\": \"{a.ObjectKind}\"");
                sb.Append("  }");
                if (++i < _archetypes.Count) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("}");
            File.WriteAllText(filePath, sb.ToString());
        }
    }

    public sealed class DungeonPackage
    {
        public string Id { get; set; } = "dungeon_01";
        public string Name { get; set; } = "Dungeon 1";
        public ZoneConfig DefaultConfig { get; set; } = new ZoneConfig();
        public List<DungeonLevel> Levels { get; } = new List<DungeonLevel>();
    }

    public sealed class GameProject
    {
        public string ProjectPath { get; set; } = string.Empty;
        public string Title { get; set; } = "GridLocked Adventure";
        public string StartDungeonId { get; set; } = "dungeon_01";
        public int StartLevelNumber { get; set; } = 1;

        public AsciiLegend Legend { get; } = new AsciiLegend();
        public ItemRegistry Items { get; } = new ItemRegistry();
        public VectorSpriteRegistry Objects { get; } = new VectorSpriteRegistry();
        public EntityRegistry Entities { get; } = new EntityRegistry();
        public LootTableRegistry LootTables { get; } = new LootTableRegistry();

        public List<string> DamageTypes { get; } = new List<string>();
        public Dictionary<string, Damage> DamageLibrary { get; } = new Dictionary<string, Damage>(StringComparer.OrdinalIgnoreCase);

        public void EnsureDefaultDamageTypes()
        {
            if (DamageTypes.Count == 0)
                DamageTypes.AddRange(Damage.CreateDefaultDamageTypes());
            if (DamageLibrary.Count == 0)
            {
                foreach (var p in Damage.CreateDefaultPresets())
                    DamageLibrary[p.Id] = p;
            }
        }

        public void ParseDamageConfig(JsonNode json)
        {
            if (json == null) return;
            var dtNode = json.Get("damageTypes");
            if (dtNode != null && dtNode.ArrayValue != null)
            {
                DamageTypes.Clear();
                foreach (var item in dtNode.ArrayValue)
                {
                    string t = item.AsString();
                    if (!string.IsNullOrWhiteSpace(t) && !DamageTypes.Contains(t))
                        DamageTypes.Add(t);
                }
            }

            var dlNode = json.Get("damageLibrary") ?? json.Get("damages") ?? json.Get("hazards");
            if (dlNode != null)
            {
                if (dlNode.ObjectValue != null)
                {
                    foreach (var kvp in dlNode.ObjectValue)
                    {
                        var d = Damage.FromJson(kvp.Value);
                        if (d != null)
                        {
                            if (string.IsNullOrEmpty(d.Id)) d.Id = kvp.Key;
                            DamageLibrary[d.Id] = d;
                        }
                    }
                }
                else if (dlNode.ArrayValue != null)
                {
                    foreach (var item in dlNode.ArrayValue)
                    {
                        var d = Damage.FromJson(item);
                        if (d != null && !string.IsNullOrEmpty(d.Id))
                            DamageLibrary[d.Id] = d;
                    }
                }
            }
        }

        public Dictionary<string, DungeonPackage> Dungeons { get; } =
            new Dictionary<string, DungeonPackage>(StringComparer.OrdinalIgnoreCase);

        public DungeonLevel CurrentLevel { get; set; }

        public static GameProject Load(string path)
        {
            if (string.IsNullOrEmpty(path))
                throw new ArgumentException("Path cannot be null or empty", nameof(path));

            if (File.Exists(path))
            {
                return LoadFromGlp(path);
            }
            if (Directory.Exists(path))
            {
                return LoadFromDirectory(path);
            }
            throw new FileNotFoundException($"Cannot find project directory or module package at: {path}");
        }

        public static GameProject LoadFromGlp(string glpPath)
        {
            if (!File.Exists(glpPath))
                throw new FileNotFoundException($"Module package not found: {glpPath}");

            var entries = GlpPackage.ReadAllTextEntries(glpPath);
            var project = new GameProject { ProjectPath = glpPath };

            string GetText(string name)
            {
                if (entries.TryGetValue(name, out var str)) return str;
                return null;
            }

            // 1. Legend
            string legendText = GetText("key legend.json") ?? GetText("key_legend.json") ?? GetText("legend.json");
            if (legendText != null)
            {
                project.Legend.ParseJson(JsonLite.Parse(legendText));
            }

            // 2. project.json
            string projJsonText = GetText("project.json");
            if (projJsonText != null)
            {
                var json = JsonLite.Parse(projJsonText);
                if (json != null)
                {
                    project.Title = json.GetString("title", project.Title);
                    project.StartDungeonId = json.GetString("startDungeon", project.StartDungeonId);
                    project.StartLevelNumber = json.GetInt("startLevel", project.StartLevelNumber);
                    project.Legend.ParseJson(json.Get("legend"));
                    project.ParseDamageConfig(json);
                }
            }

            // 2.5 damage_types.json in GLP
            string dtGlpText = GetText("damage_types.json");
            if (dtGlpText != null)
            {
                var dtJson = JsonLite.Parse(dtGlpText);
                if (dtJson != null) project.ParseDamageConfig(dtJson);
            }
            project.EnsureDefaultDamageTypes();

            // 3. item_classes.json
            string classesText = GetText("item_classes.json");
            if (classesText != null)
            {
                LoadItemClassesFromText(project.Items, classesText);
            }

            // 4. items.json
            string itemsText = GetText("items.json");
            if (itemsText != null)
            {
                LoadItemsFromText(project.Items, itemsText);
            }

            // 5. objects.json
            string objectsText = GetText("objects.json");
            if (objectsText != null)
            {
                LoadObjectsFromText(project.Objects, objectsText);
            }

            // 6. entities.json (or legacy monsters.json)
            string entitiesText = GetText("entities.json") ?? GetText("monsters.json");
            if (entitiesText != null)
            {
                LoadEntitiesFromText(project.Entities, entitiesText);
            }

            // 6.5. loot_tables.json
            string lootText = GetText("loot_tables.json");
            if (lootText != null)
            {
                project.LootTables.LoadFromJson(lootText);
            }

            // 7. Dungeons
            var dungeonIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            foreach (var key in entries.Keys)
            {
                if (key.StartsWith("dungeons/", StringComparison.OrdinalIgnoreCase))
                {
                    var parts = key.Split('/');
                    if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1]))
                        dungeonIds.Add(parts[1]);
                }
            }

            foreach (var dId in dungeonIds)
            {
                var pkg = LoadDungeonFromEntries(entries, dId, project);
                if (pkg != null)
                    project.Dungeons[dId] = pkg;
            }

            // 7. Select initial level
            if (project.Dungeons.TryGetValue(project.StartDungeonId, out var startPkg))
            {
                project.CurrentLevel = startPkg.Levels.Find(l => l.LevelNumber == project.StartLevelNumber)
                                       ?? (startPkg.Levels.Count > 0 ? startPkg.Levels[0] : null);
            }

            return project;
        }

        public static GameProject LoadFromDirectory(string projectDir)
        {
            if (!Directory.Exists(projectDir))
                throw new DirectoryNotFoundException($"Game project directory not found: {projectDir}");

            var project = new GameProject { ProjectPath = projectDir };

            // 1. Engine default keys are preset in project.Legend (floor, wall, spawn, stairs_up, stairs_down, door, void).
            // If "key legend.json" exists in the project, use those keys instead to define/override project mappings.
            string[] legendCandidates = new[]
            {
                Path.Combine(projectDir, "key legend.json"),
                Path.Combine(projectDir, "key_legend.json"),
                Path.Combine(projectDir, "legend.json")
            };
            foreach (var cand in legendCandidates)
            {
                if (File.Exists(cand))
                {
                    project.Legend.LoadFromFile(cand);
                    break;
                }
            }

            // 2. Load project.json if present (can also have title, start info, and optional legend overrides)
            string projJsonPath = Path.Combine(projectDir, "project.json");
            if (File.Exists(projJsonPath))
            {
                var json = JsonLite.Parse(File.ReadAllText(projJsonPath));
                if (json != null)
                {
                    project.Title = json.GetString("title", project.Title);
                    project.StartDungeonId = json.GetString("startDungeon", project.StartDungeonId);
                    project.StartLevelNumber = json.GetInt("startLevel", project.StartLevelNumber);
                    project.Legend.ParseJson(json.Get("legend"));
                    project.ParseDamageConfig(json);
                }
            }

            // 2.5 damage_types.json in directory
            string dtDirPath = Path.Combine(projectDir, "damage_types.json");
            if (File.Exists(dtDirPath))
            {
                var dtJson = JsonLite.Parse(File.ReadAllText(dtDirPath));
                if (dtJson != null) project.ParseDamageConfig(dtJson);
            }
            project.EnsureDefaultDamageTypes();

            // 2. Load item_classes.json if present
            string classesJsonPath = Path.Combine(projectDir, "item_classes.json");
            if (File.Exists(classesJsonPath))
            {
                LoadItemClasses(project.Items, classesJsonPath);
            }

            // Load items.json
            string itemsJsonPath = Path.Combine(projectDir, "items.json");
            if (File.Exists(itemsJsonPath))
            {
                LoadItems(project.Items, itemsJsonPath);
            }

            // 3. Load objects.json (vector sprites)
            string objectsJsonPath = Path.Combine(projectDir, "objects.json");
            if (File.Exists(objectsJsonPath))
            {
                LoadObjects(project.Objects, objectsJsonPath);
            }

            // 4. Load entities.json (or legacy monsters.json)
            string entitiesJsonPath = Path.Combine(projectDir, "entities.json");
            if (!File.Exists(entitiesJsonPath))
                entitiesJsonPath = Path.Combine(projectDir, "monsters.json");
            if (File.Exists(entitiesJsonPath))
            {
                LoadEntities(project.Entities, entitiesJsonPath);
            }

            // 4.5. Load loot_tables.json
            string lootJsonPath = Path.Combine(projectDir, "loot_tables.json");
            if (File.Exists(lootJsonPath))
            {
                project.LootTables.LoadFromJson(File.ReadAllText(lootJsonPath));
            }

            // 5. Scan and load dungeons/
            string dungeonsDir = Path.Combine(projectDir, "dungeons");
            if (Directory.Exists(dungeonsDir))
            {
                foreach (var dDir in Directory.GetDirectories(dungeonsDir))
                {
                    string dungeonId = Path.GetFileName(dDir);
                    var pkg = LoadDungeon(dDir, dungeonId, project);
                    if (pkg != null)
                        project.Dungeons[dungeonId] = pkg;
                }
            }

            // 5. Select initial level
            if (project.Dungeons.TryGetValue(project.StartDungeonId, out var startPkg))
            {
                project.CurrentLevel = startPkg.Levels.Find(l => l.LevelNumber == project.StartLevelNumber)
                                       ?? (startPkg.Levels.Count > 0 ? startPkg.Levels[0] : null);
            }

            return project;
        }

        private static void LoadObjects(VectorSpriteRegistry registry, string objectsPath)
        {
            if (File.Exists(objectsPath))
                LoadObjectsFromText(registry, File.ReadAllText(objectsPath));
        }

        private static void LoadObjectsFromText(VectorSpriteRegistry registry, string jsonText)
        {
            var json = JsonLite.Parse(jsonText);
            if (json == null || json.ArrayValue == null) return;

            foreach (var node in json.ArrayValue)
            {
                var sprite = new VectorSprite
                {
                    Id = node.GetString("id", ""),
                    Name = node.GetString("name", ""),
                    BaseScale = (float)node.GetDouble("scale", 1.0)
                };

                var shapesArr = node.Get("vector");
                if (shapesArr != null && shapesArr.ArrayValue != null)
                {
                    foreach (var sNode in shapesArr.ArrayValue)
                    {
                        string typeStr = sNode.GetString("type", "rect").ToLowerInvariant();
                        var type = VectorShapeType.Rect;
                        if (typeStr == "ellipse" || typeStr == "circle") type = VectorShapeType.Ellipse;
                        else if (typeStr == "line") type = VectorShapeType.Line;
                        else if (typeStr == "polygon") type = VectorShapeType.Polygon;

                        var shape = new VectorShape
                        {
                            Type = type,
                            X = (float)sNode.GetDouble("x", 0.0),
                            Y = (float)sNode.GetDouble("y", 0.0),
                            W = (float)sNode.GetDouble("w", 1.0),
                            H = (float)sNode.GetDouble("h", 1.0),
                            FillColor = ZoneConfig.ParseColorHex(sNode.GetString("fill", ""), Color.Empty),
                            StrokeColor = ZoneConfig.ParseColorHex(sNode.GetString("stroke", ""), Color.Empty),
                            StrokeWidth = (float)sNode.GetDouble("strokeWidth", 1.5)
                        };

                        var ptsNode = sNode.Get("points");
                        if (ptsNode != null)
                        {
                            var ptsList = new List<PointF>();
                            if (ptsNode.ArrayValue != null)
                            {
                                foreach (var pElem in ptsNode.ArrayValue)
                                {
                                    if (pElem.ArrayValue != null && pElem.ArrayValue.Count >= 2)
                                        ptsList.Add(new PointF((float)pElem.ArrayValue[0].NumberValue, (float)pElem.ArrayValue[1].NumberValue));
                                    else if (pElem.StringValue != null)
                                    {
                                        var xy = pElem.StringValue.Split(',');
                                        if (xy.Length >= 2 && float.TryParse(xy[0], out float px) && float.TryParse(xy[1], out float py))
                                            ptsList.Add(new PointF(px, py));
                                    }
                                }
                            }
                            else if (!string.IsNullOrEmpty(ptsNode.StringValue))
                            {
                                var rawPts = ptsNode.StringValue.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                                foreach (var rp in rawPts)
                                {
                                    var xy = rp.Split(',');
                                    if (xy.Length >= 2 && float.TryParse(xy[0], out float px) && float.TryParse(xy[1], out float py))
                                        ptsList.Add(new PointF(px, py));
                                }
                            }
                            if (ptsList.Count > 0) shape.Points = ptsList.ToArray();
                        }

                        sprite.Shapes.Add(shape);
                    }
                }

                registry.Register(sprite);
            }
        }

        private static void LoadItemClasses(ItemRegistry registry, string path)
        {
            if (File.Exists(path))
                LoadItemClassesFromText(registry, File.ReadAllText(path));
        }

        private static void LoadItemClassesFromText(ItemRegistry registry, string jsonText)
        {
            var json = JsonLite.Parse(jsonText);
            if (json == null || json.ArrayValue == null) return;

            foreach (var node in json.ArrayValue)
            {
                var tmpl = new ItemClassTemplate
                {
                    ClassName = node.GetString("className", ""),
                    InheritsFrom = node.GetString("inheritsFrom", ""),
                    Category = node.GetString("category", "Weapon"),
                    Description = node.GetString("description", ""),
                    DefaultSprite = node.GetString("defaultSprite", "weapon")
                };

                var fieldsNode = node.Get("fields");
                if (fieldsNode != null && fieldsNode.ArrayValue != null)
                {
                    foreach (var fNode in fieldsNode.ArrayValue)
                    {
                        var field = new TemplateField
                        {
                            Name = fNode.GetString("name", ""),
                            Type = fNode.GetString("type", "string"),
                            DefaultValue = fNode.GetString("default", ""),
                            MinValue = (float)fNode.GetDouble("min", 0),
                            MaxValue = (float)fNode.GetDouble("max", 100),
                            Tooltip = fNode.GetString("tooltip", "")
                        };
                        var optsNode = fNode.Get("options");
                        if (optsNode != null && optsNode.ArrayValue != null)
                        {
                            foreach (var opt in optsNode.ArrayValue)
                                if (opt.StringValue != null) field.Options.Add(opt.StringValue);
                        }
                        tmpl.Fields.Add(field);
                    }
                }

                if (!string.IsNullOrEmpty(tmpl.ClassName))
                    registry.RegisterClassTemplate(tmpl);
            }
        }

        private static void LoadItems(ItemRegistry registry, string itemsPath)
        {
            if (File.Exists(itemsPath))
                LoadItemsFromText(registry, File.ReadAllText(itemsPath));
        }

        private static void LoadItemsFromText(ItemRegistry registry, string jsonText)
        {
            var json = JsonLite.Parse(jsonText);
            if (json == null || json.ArrayValue == null) return;

            foreach (var itemNode in json.ArrayValue)
            {
                var def = new ItemDefinition
                {
                    Id = itemNode.GetString("id", ""),
                    Name = itemNode.GetString("name", ""),
                    Inherit = itemNode.GetString("inherit", ""),
                    Description = itemNode.GetString("description", ""),
                    IsTemplate = itemNode.GetBool("isTemplate", false),
                    Category = itemNode.GetString("category", "Prop"),
                    SubCategory = itemNode.GetString("subCategory", ""),
                    Weight = (float)itemNode.GetDouble("weight", 1.0),
                    Value = itemNode.GetInt("value", itemNode.GetInt("sellCost", 10)),
                    Collectable = itemNode.GetBool("collectable", true),
                    PassThru = itemNode.GetBool("passThru", true),
                    IsEquipable = itemNode.GetBool("isEquipable", false),
                    EquipSlot = itemNode.GetString("equipSlot", "MainHand"),
                    AttackType = itemNode.GetString("attackType", "Melee"),
                    Hands = itemNode.GetInt("hands", 1),
                    MinDamage = itemNode.GetInt("minDamage", 1),
                    MaxDamage = itemNode.GetInt("maxDamage", 4),
                    Range = itemNode.GetInt("range", 1),
                    AmmoType = itemNode.GetString("ammoType", ""),
                    HasEffect = itemNode.GetBool("hasEffect", itemNode.GetBool("isMagic", false)),
                    EffectType = itemNode.GetString("effectType", itemNode.GetString("magicElement", itemNode.GetString("energyType", "None"))),
                    ProcEffect = itemNode.GetString("procEffect", itemNode.GetString("spellEffect", itemNode.GetString("effect", ""))),
                    ResourceCost = itemNode.GetInt("resourceCost", itemNode.GetInt("manaCost", 0)),
                    EffectPower = itemNode.GetInt("effectPower", itemNode.GetInt("magicPower", 0)),
                    ArmorRating = itemNode.GetInt("armorRating", itemNode.GetInt("defense", 0)),
                    EnergyResistance = itemNode.GetInt("energyResistance", itemNode.GetInt("magicResistance", 0)),
                    ResistType = itemNode.GetString("resistType", itemNode.GetString("resistElement", "None")),
                    ConsumableType = itemNode.GetString("consumableType", "None"),
                    EffectAmount = itemNode.GetInt("effectAmount", 0),
                    Duration = (float)itemNode.GetDouble("duration", 0.0),
                    IsDifficultTerrain = itemNode.GetBool("isDifficultTerrain", false),
                    SpeedPenalty = (float)itemNode.GetDouble("speedPenalty", 0.0),
                    HazardDamage = itemNode.GetInt("hazardDamage", 0),
                    IsContainer = itemNode.GetBool("isContainer", false),
                    Capacity = itemNode.GetInt("capacity", 10),
                    SpriteKind = itemNode.GetString("sprite", "rock"),
                    Color = ZoneConfig.ParseColorHex(itemNode.GetString("color", ""), Color.FromArgb(160, 160, 160)),
                    DefaultTrapped = itemNode.GetString("trapped", null),
                    DefaultLocked = itemNode.GetString("locked", null),
                    DefaultCursed = itemNode.GetBool("cursed", false),
                    DefaultCurseType = itemNode.GetString("curseType", null)
                };

                // Normalize legacy "Magic" attack type to "Cast"
                if (string.Equals(def.AttackType, "Magic", StringComparison.OrdinalIgnoreCase))
                {
                    def.AttackType = "Cast";
                    def.HasEffect = true;
                }

                // Check nested "effect", "affect", or legacy "magic" object if present
                var effectNode = itemNode.Get("effect") ?? itemNode.Get("affect") ?? itemNode.Get("magic");
                if (effectNode != null && effectNode.ObjectValue != null)
                {
                    def.HasEffect = true;
                    def.EffectType = effectNode.GetString("type", effectNode.GetString("element", effectNode.GetString("energyType", def.EffectType)));
                    def.ProcEffect = effectNode.GetString("proc", effectNode.GetString("spellEffect", effectNode.GetString("name", def.ProcEffect)));
                    def.ResourceCost = effectNode.GetInt("resourceCost", effectNode.GetInt("manaCost", def.ResourceCost));
                    def.EffectPower = effectNode.GetInt("power", def.EffectPower);
                }

                // Custom properties
                var propsNode = itemNode.Get("properties");
                if (propsNode != null && propsNode.ObjectValue != null)
                {
                    foreach (var kvp in propsNode.ObjectValue)
                        def.CustomProperties[kvp.Key] = kvp.Value.StringValue ?? kvp.Value.ToString();
                }

                if (!string.IsNullOrEmpty(def.Id))
                    registry.Register(def);
            }

            registry.ResolveInheritance();
        }

        public static void LoadEntities(EntityRegistry registry, string path)
        {
            if (File.Exists(path))
                LoadEntitiesFromText(registry, File.ReadAllText(path));
        }

        public static void LoadEntitiesFromText(EntityRegistry registry, string jsonText)
        {
            var json = JsonLite.Parse(jsonText);
            if (json == null || json.ArrayValue == null) return;

            foreach (var mNode in json.ArrayValue)
            {
                string id = mNode.GetString("id", "");
                if (string.IsNullOrEmpty(id)) continue;

                string dispStr = mNode.GetString("disposition", mNode.GetBool("isHostile", true) ? "Hostile" : "Neutral");
                Enum.TryParse<EntityDisposition>(dispStr, true, out var disposition);

                var def = new EntityDefinition
                {
                    Id = id,
                    Name = mNode.GetString("name", id),
                    Category = mNode.GetString("category", mNode.GetString("creatureType", "Humanoid")),
                    Disposition = disposition,
                    SpriteKind = mNode.GetString("spriteKind", "skeleton"),
                    Description = mNode.GetString("description", "")
                };

                // Curve config
                var cNode = mNode.Get("curve");
                if (cNode != null)
                {
                    def.CurveConfig.MinLevel = cNode.GetInt("minLevel", 1);
                    def.CurveConfig.MaxLevel = cNode.GetInt("maxLevel", 20);
                    string styleStr = cNode.GetString("style", "SmoothStep");
                    if (Enum.TryParse<StatTweenCurve>(styleStr, true, out var curveStyle))
                        def.CurveConfig.CurveStyle = curveStyle;

                    var minNode = cNode.Get("minStats");
                    if (minNode != null)
                        ParseStatsNode(minNode, def.CurveConfig.MinStats);

                    var maxNode = cNode.Get("maxStats");
                    if (maxNode != null)
                        ParseStatsNode(maxNode, def.CurveConfig.MaxStats);
                }

                // Abilities progression
                var abNode = mNode.Get("abilities");
                if (abNode != null && abNode.ArrayValue != null)
                {
                    def.Abilities.Clear();
                    foreach (var an in abNode.ArrayValue)
                    {
                        var ab = new EntityAbilityProgression
                        {
                            AbilityId = an.GetString("id", an.GetString("abilityId", "ability")),
                            Name = an.GetString("name", "Ability"),
                            UnlockLevel = an.GetInt("unlockLevel", 1),
                            StartRank = an.GetInt("startRank", 1),
                            MaxRank = an.GetInt("maxRank", 5),
                            MaxRankLevel = an.GetInt("maxRankLevel", 20)
                        };
                        def.Abilities.Add(ab);
                    }
                }

                // Auto-tween first
                def.AutoTweenLevels();

                // Baked level table overrides (if present)
                var lvlNode = mNode.Get("levels");
                if (lvlNode != null && lvlNode.ArrayValue != null)
                {
                    foreach (var ln in lvlNode.ArrayValue)
                    {
                        int lvlNum = ln.GetInt("level", 1);
                        var stats = new EntityLevelStats
                        {
                            Level = lvlNum,
                            HP = ln.GetInt("hp", 30),
                            MP = ln.GetInt("mp", 0),
                            MinDamage = ln.GetInt("minDamage", 3),
                            MaxDamage = ln.GetInt("maxDamage", 6),
                            Armor = ln.GetInt("armor", 0),
                            Speed = ln.GetInt("speed", 10),
                            XpReward = ln.GetInt("xp", 15)
                        };

                        var abArr = ln.Get("abilities");
                        if (abArr != null && abArr.ArrayValue != null)
                        {
                            foreach (var item in abArr.ArrayValue)
                                if (!string.IsNullOrEmpty(item.StringValue))
                                    stats.Abilities.Add(item.StringValue);
                        }
                        def.LevelTable[lvlNum] = stats;
                    }
                }

                registry.Register(def);
            }
        }

        private static void ParseStatsNode(JsonNode node, EntityLevelStats stats)
        {
            stats.HP = node.GetInt("hp", stats.HP);
            stats.MP = node.GetInt("mp", stats.MP);
            stats.MinDamage = node.GetInt("minDamage", stats.MinDamage);
            stats.MaxDamage = node.GetInt("maxDamage", stats.MaxDamage);
            stats.Armor = node.GetInt("armor", stats.Armor);
            stats.Speed = node.GetInt("speed", stats.Speed);
            stats.XpReward = node.GetInt("xp", stats.XpReward);
        }

        public static void SaveEntities(EntityRegistry registry, string path)
        {
            var sb = new StringBuilder();
            sb.AppendLine("[");
            var list = new List<EntityDefinition>(registry.GetAll());
            for (int i = 0; i < list.Count; i++)
            {
                var m = list[i];
                sb.AppendLine("  {");
                sb.AppendLine($"    \"id\": \"{m.Id}\",");
                sb.AppendLine($"    \"name\": \"{m.Name}\",");
                sb.AppendLine($"    \"category\": \"{m.Category}\",");
                sb.AppendLine($"    \"disposition\": \"{m.Disposition}\",");
                sb.AppendLine($"    \"spriteKind\": \"{m.SpriteKind}\",");
                sb.AppendLine($"    \"description\": \"{m.Description}\",");

                // Curve config
                sb.AppendLine("    \"curve\": {");
                sb.AppendLine($"      \"minLevel\": {m.CurveConfig.MinLevel},");
                sb.AppendLine($"      \"maxLevel\": {m.CurveConfig.MaxLevel},");
                sb.AppendLine($"      \"style\": \"{m.CurveConfig.CurveStyle}\",");
                sb.AppendLine("      \"minStats\": {");
                sb.AppendLine($"        \"hp\": {m.CurveConfig.MinStats.HP}, \"mp\": {m.CurveConfig.MinStats.MP}, \"minDamage\": {m.CurveConfig.MinStats.MinDamage}, \"maxDamage\": {m.CurveConfig.MinStats.MaxDamage}, \"armor\": {m.CurveConfig.MinStats.Armor}, \"speed\": {m.CurveConfig.MinStats.Speed}, \"xp\": {m.CurveConfig.MinStats.XpReward}");
                sb.AppendLine("      },");
                sb.AppendLine("      \"maxStats\": {");
                sb.AppendLine($"        \"hp\": {m.CurveConfig.MaxStats.HP}, \"mp\": {m.CurveConfig.MaxStats.MP}, \"minDamage\": {m.CurveConfig.MaxStats.MinDamage}, \"maxDamage\": {m.CurveConfig.MaxStats.MaxDamage}, \"armor\": {m.CurveConfig.MaxStats.Armor}, \"speed\": {m.CurveConfig.MaxStats.Speed}, \"xp\": {m.CurveConfig.MaxStats.XpReward}");
                sb.AppendLine("      }");
                sb.AppendLine("    },");

                // Abilities
                sb.AppendLine("    \"abilities\": [");
                for (int a = 0; a < m.Abilities.Count; a++)
                {
                    var ab = m.Abilities[a];
                    sb.Append($"      {{ \"id\": \"{ab.AbilityId}\", \"name\": \"{ab.Name}\", \"unlockLevel\": {ab.UnlockLevel}, \"startRank\": {ab.StartRank}, \"maxRank\": {ab.MaxRank}, \"maxRankLevel\": {ab.MaxRankLevel} }}");
                    if (a < m.Abilities.Count - 1) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("    ],");

                // Baked Level Table
                sb.AppendLine("    \"levels\": [");
                int rowIdx = 0;
                foreach (var kvp in m.LevelTable)
                {
                    var row = kvp.Value;
                    string abStr = "";
                    if (row.Abilities.Count > 0)
                    {
                        var abList = new List<string>();
                        foreach (var ab in row.Abilities) abList.Add($"\"{ab}\"");
                        abStr = string.Join(", ", abList);
                    }

                    sb.Append($"      {{ \"level\": {row.Level}, \"hp\": {row.HP}, \"mp\": {row.MP}, \"minDamage\": {row.MinDamage}, \"maxDamage\": {row.MaxDamage}, \"armor\": {row.Armor}, \"speed\": {row.Speed}, \"xp\": {row.XpReward}, \"abilities\": [{abStr}] }}");
                    if (++rowIdx < m.LevelTable.Count) sb.Append(",");
                    sb.AppendLine();
                }
                sb.AppendLine("    ]");

                sb.Append("  }");
                if (i < list.Count - 1) sb.Append(",");
                sb.AppendLine();
            }
            sb.AppendLine("]");
            File.WriteAllText(path, sb.ToString());
        }

        private static DungeonPackage LoadDungeon(string dungeonDir, string dungeonId, GameProject project)
        {
            var pkg = new DungeonPackage { Id = dungeonId, Name = dungeonId };

            // Optional dungeon.json for default configs and dungeon-specific legend overrides
            string dJsonPath = Path.Combine(dungeonDir, "dungeon.json");
            if (File.Exists(dJsonPath))
            {
                var json = JsonLite.Parse(File.ReadAllText(dJsonPath));
                if (json != null)
                {
                    pkg.Name = json.GetString("name", pkg.Name);
                    ParseZoneConfig(json, pkg.DefaultConfig);
                    project.Legend.ParseJson(json.Get("legend"));
                }
            }

            // Look for level_* directories
            var levelDirs = Directory.GetDirectories(dungeonDir, "level_*");
            Array.Sort(levelDirs);

            foreach (var lvlDir in levelDirs)
            {
                string dirName = Path.GetFileName(lvlDir);
                int lvlNum = 1;
                string[] parts = dirName.Split('_');
                if (parts.Length > 1) int.TryParse(parts[1], out lvlNum);

                var level = LoadLevel(lvlDir, lvlNum, pkg.DefaultConfig, project);
                if (level != null)
                    pkg.Levels.Add(level);
            }

            return pkg;
        }

        private static DungeonPackage LoadDungeonFromEntries(Dictionary<string, string> entries, string dungeonId, GameProject project)
        {
            var pkg = new DungeonPackage { Id = dungeonId, Name = dungeonId };

            string dJsonKey = $"dungeons/{dungeonId}/dungeon.json";
            if (entries.TryGetValue(dJsonKey, out var dJsonStr))
            {
                var json = JsonLite.Parse(dJsonStr);
                if (json != null)
                {
                    pkg.Name = json.GetString("name", pkg.Name);
                    ParseZoneConfig(json, pkg.DefaultConfig);
                    project.Legend.ParseJson(json.Get("legend"));
                }
            }

            var levelNums = new SortedSet<int>();
            string prefix = $"dungeons/{dungeonId}/level_";
            foreach (var key in entries.Keys)
            {
                if (key.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    int afterPrefix = prefix.Length;
                    int slashIdx = key.IndexOf('/', afterPrefix);
                    if (slashIdx > afterPrefix)
                    {
                        string lvlStr = key.Substring(afterPrefix, slashIdx - afterPrefix);
                        if (int.TryParse(lvlStr, out int num))
                            levelNums.Add(num);
                    }
                }
            }

            foreach (var lvlNum in levelNums)
            {
                string lvlPrefix = $"dungeons/{dungeonId}/level_{lvlNum}/";
                if (entries.TryGetValue(lvlPrefix + "map.txt", out var mapText))
                {
                    entries.TryGetValue(lvlPrefix + "items.txt", out var itemsText);
                    entries.TryGetValue(lvlPrefix + "monsters.txt", out var monstersText);
                    entries.TryGetValue(lvlPrefix + "transitions.json", out var transText);
                    entries.TryGetValue(lvlPrefix + "config.json", out var cfgText);

                    var level = ParseLevel(mapText, itemsText, monstersText, transText, cfgText, lvlNum, pkg.DefaultConfig, project);
                    if (level != null)
                        pkg.Levels.Add(level);
                }
            }

            return pkg;
        }

        private static DungeonLevel LoadLevel(string levelDir, int levelNum, ZoneConfig defaultConfig, GameProject project)
        {
            string mapPath = Path.Combine(levelDir, "map.txt");
            if (!File.Exists(mapPath)) return null;

            string mapText = File.ReadAllText(mapPath);
            string cfgPath = Path.Combine(levelDir, "config.json");
            string cfgText = File.Exists(cfgPath) ? File.ReadAllText(cfgPath) : null;
            string itemsFile = Path.Combine(levelDir, "items.txt");
            string itemsText = File.Exists(itemsFile) ? File.ReadAllText(itemsFile) : null;
            string monstersFile = Path.Combine(levelDir, "monsters.txt");
            string monstersText = File.Exists(monstersFile) ? File.ReadAllText(monstersFile) : null;
            string transFile = Path.Combine(levelDir, "transitions.json");
            string transText = File.Exists(transFile) ? File.ReadAllText(transFile) : null;

            return ParseLevel(mapText, itemsText, monstersText, transText, cfgText, levelNum, defaultConfig, project);
        }

        private static DungeonLevel ParseLevel(string mapText, string itemsText, string monstersText, string transitionsText, string cfgText, int levelNum, ZoneConfig defaultConfig, GameProject project)
        {
            if (string.IsNullOrEmpty(mapText)) return null;

            var level = new DungeonLevel
            {
                LevelNumber = levelNum,
                LevelName = $"Level {levelNum}",
                Config = CloneConfig(defaultConfig)
            };

            // Optional level config override (can also have level-specific legend)
            if (!string.IsNullOrEmpty(cfgText))
            {
                var json = JsonLite.Parse(cfgText);
                if (json != null)
                {
                    ParseZoneConfig(json, level.Config);
                    project.Legend.ParseJson(json.Get("legend"));
                }
            }

            // 1. Parse map text lines
            var lines = mapText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
            if (lines.Length > 0 && string.IsNullOrEmpty(lines[lines.Length - 1]))
            {
                var trimmed = new string[lines.Length - 1];
                Array.Copy(lines, trimmed, lines.Length - 1);
                lines = trimmed;
            }

            int h = lines.Length;
            int w = 0;
            for (int i = 0; i < h; i++) if (lines[i].Length > w) w = lines[i].Length;

            level.Width = w;
            level.Height = h;
            level.Tiles = new DungeonTile[h, w];

            for (int y = 0; y < h; y++)
            {
                string line = lines[y];
                for (int x = 0; x < w; x++)
                {
                    char c = (x < line.Length) ? line[x] : ' ';
                    var arch = project.Legend.GetArchetype(c);
                    string mapping = arch.Id.ToLowerInvariant();
                    var tileType = arch.BaseType;
                    LargeObjectInstance customObj = null;

                    if (mapping == "wall" || tileType == TileType.Wall) tileType = TileType.Wall;
                    else if (mapping == "spawn" || tileType == TileType.Spawn)
                    {
                        tileType = TileType.Spawn;
                        level.SpawnX = x + 0.5;
                        level.SpawnY = y + 0.5;
                    }
                    else if (mapping == "stairs_up" || tileType == TileType.StairsUp)
                    {
                        tileType = TileType.StairsUp;       // 'S'
                        var sprite = project.Objects.Get("stairs_up");
                        var st = new StairsTransition($"stairs_up_{x}_{y}", TransitionKind.StairsUp, x, y, Math.Max(1, levelNum - 1), sprite);
                        customObj = st;
                        level.Transitions.Add(st);
                        level.LargeObjects.Add(st);
                    }
                    else if (mapping == "stairs_down" || tileType == TileType.StairsDown)
                    {
                        tileType = TileType.StairsDown;   // 's'
                        var sprite = project.Objects.Get("stairs_down");
                        var st = new StairsTransition($"stairs_down_{x}_{y}", TransitionKind.StairsDown, x, y, levelNum + 1, sprite);
                        customObj = st;
                        level.Transitions.Add(st);
                        level.LargeObjects.Add(st);
                    }
                    else if (mapping == "door" || tileType == TileType.Door || arch.CanBeOpened)
                    {
                        tileType = TileType.Door;
                        var clSprite = project.Objects.Get("door_closed");
                        var opSprite = project.Objects.Get("door_open");
                        var dt = new DoorTransition($"door_{x}_{y}", x, y, clSprite, opSprite);
                        customObj = dt;
                        level.Transitions.Add(dt);
                        level.LargeObjects.Add(dt);
                    }
                    else if (mapping == "void" || mapping == "pit" || tileType == TileType.VoidOrPit)
                    {
                        tileType = TileType.VoidOrPit;
                        var pitSprite = project.Objects.Get("pit");
                        var pt = new PitTransition($"pit_{x}_{y}", x, y, pitSprite);
                        customObj = pt;
                        level.Transitions.Add(pt);
                        level.LargeObjects.Add(pt);
                    }
                    else if (!string.IsNullOrEmpty(arch.ObjectKind) || project.Objects.Contains(mapping))
                    {
                        tileType = TileType.Floor;
                        string objKind = !string.IsNullOrEmpty(arch.ObjectKind) ? arch.ObjectKind : mapping;
                        var sprite = project.Objects.Get(objKind);
                        customObj = new LargeObjectInstance(objKind, sprite?.Name ?? arch.Name, objKind, !arch.CanWalkThru, sprite)
                        {
                            TileX = x,
                            TileY = y
                        };
                        level.LargeObjects.Add(customObj);
                    }
                    else
                    {
                        tileType = TileType.Floor;
                    }

                    var tile = new DungeonTile(x, y, tileType);
                    tile.ApplyArchetype(arch);
                    if (customObj != null)
                        tile.CenterOccupant = customObj;

                    level.Tiles[y, x] = tile;
                }
            }

            // 2. Parse transitions.json if present (applies custom destinations, locks, traps)
            if (!string.IsNullOrEmpty(transitionsText))
            {
                var transJson = JsonLite.Parse(transitionsText);
                if (transJson != null && transJson.ArrayValue != null)
                {
                    foreach (var node in transJson.ArrayValue)
                    {
                        int tx = node.GetInt("x", -1);
                        int ty = node.GetInt("y", -1);
                        if (tx < 0 || ty < 0 || tx >= level.Width || ty >= level.Height) continue;

                        var existing = level.Tiles[ty, tx].Transition;
                        string typeStr = node.GetString("type", "Door");
                        Enum.TryParse(typeStr, true, out TransitionKind tKind);

                        if (existing == null)
                        {
                            if (tKind == TransitionKind.Door)
                                existing = new DoorTransition($"door_{tx}_{ty}", tx, ty, project.Objects.Get("door_closed"), project.Objects.Get("door_open"));
                            else if (tKind == TransitionKind.StairsUp || tKind == TransitionKind.StairsDown)
                                existing = new StairsTransition($"stairs_{tx}_{ty}", tKind, tx, ty, tKind == TransitionKind.StairsUp ? Math.Max(1, levelNum - 1) : levelNum + 1, project.Objects.Get(tKind == TransitionKind.StairsUp ? "stairs_up" : "stairs_down"));
                            else if (tKind == TransitionKind.Pit)
                                existing = new PitTransition($"pit_{tx}_{ty}", tx, ty, project.Objects.Get("pit"));
                            else
                                existing = new PortalTransition($"portal_{tx}_{ty}", tx, ty, project.Objects.Get("portal"));

                            level.Tiles[ty, tx].Transition = existing;
                            level.Transitions.Add(existing);
                            level.LargeObjects.Add(existing);
                        }

                        existing.TargetDungeon = node.GetString("targetDungeon", existing.TargetDungeon);
                        existing.TargetLevel = node.GetInt("targetLevel", existing.TargetLevel);
                        existing.TargetX = node.GetInt("targetX", existing.TargetX);
                        existing.TargetY = node.GetInt("targetY", existing.TargetY);
                        existing.TargetFacing = node.GetString("targetFacing", existing.TargetFacing);
                        existing.IsOpen = node.GetBool("isOpen", existing.IsOpen);
                        if (existing.IsOpen) existing.Open(); else existing.Close();
                        existing.IsLocked = node.GetBool("isLocked", existing.IsLocked);
                        existing.RequiredKey = node.GetString("requiredKey", existing.RequiredKey);
                        existing.IsTrapped = node.GetBool("isTrapped", existing.IsTrapped);
                        existing.TrapType = node.GetString("trapType", existing.TrapType);
                        existing.TrapDamage = node.GetInt("trapDamage", existing.TrapDamage);
                        existing.IsTrapDisarmed = node.GetBool("isTrapDisarmed", existing.IsTrapDisarmed);
                        existing.TrapIsRecurring = node.GetBool("trapIsRecurring", node.GetBool("isRecurring", existing.TrapIsRecurring));
                        existing.TrapCharges = node.GetInt("trapCharges", existing.TrapCharges);
                        existing.TrapJiggleChance = node.GetDouble("trapJiggleChance", existing.TrapJiggleChance);
                        existing.TrapOpenChance = node.GetDouble("trapOpenChance", existing.TrapOpenChance);
                        existing.AutoTransitionOnStep = node.GetBool("autoTransition", node.GetBool("autoTransitionOnStep", existing.AutoTransitionOnStep));
                        existing.OpenMessage = node.GetString("openMessage", existing.OpenMessage);
                        existing.LockedMessage = node.GetString("lockedMessage", existing.LockedMessage);
                        existing.TrapMessage = node.GetString("trapMessage", existing.TrapMessage);
                        existing.TransitionMessage = node.GetString("transitionMessage", existing.TransitionMessage);
                        string prompt = node.GetString("promptText", "");
                        if (!string.IsNullOrEmpty(prompt)) existing.InteractionPrompt = prompt;
                    }
                }
            }

            // 3. Parse items text
            if (!string.IsNullOrEmpty(itemsText))
            {
                var itemLines = itemsText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var rawLine in itemLines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                        continue;

                    ParseItemLine(line, level, project.Items);
                }
            }

            // 3. Parse monsters text
            if (!string.IsNullOrEmpty(monstersText))
            {
                var monsterLines = monstersText.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.RemoveEmptyEntries);
                foreach (var rawLine in monsterLines)
                {
                    string line = rawLine.Trim();
                    if (string.IsNullOrEmpty(line) || line.StartsWith("#") || line.StartsWith("//"))
                        continue;

                    ParseMonsterLine(line, level, project.Entities);
                }
            }

            return level;
        }

        private static void ParseItemLine(string line, DungeonLevel level, ItemRegistry items)
        {
            // Syntax: [x, y, subSquare, count] itemID {optional json modifiers}
            int closeBracket = line.IndexOf(']');
            int openBracket = line.IndexOf('[');
            if (openBracket < 0 || closeBracket < openBracket) return;

            string coords = line.Substring(openBracket + 1, closeBracket - openBracket - 1);
            string remainder = line.Substring(closeBracket + 1).Trim();

            string[] cparts = coords.Split(',');
            if (cparts.Length < 3) return;

            if (!int.TryParse(cparts[0].Trim(), out int x)) return;
            if (!int.TryParse(cparts[1].Trim(), out int y)) return;
            if (!int.TryParse(cparts[2].Trim(), out int subSquare)) return;
            int count = 1;
            if (cparts.Length >= 4) int.TryParse(cparts[3].Trim(), out count);

            // Remainder contains itemID and optional { ... }
            string itemId = remainder;
            Dictionary<string, string> modifiers = null;

            int braceIdx = remainder.IndexOf('{');
            if (braceIdx >= 0)
            {
                itemId = remainder.Substring(0, braceIdx).Trim();
                string jsonBlock = remainder.Substring(braceIdx);
                var jmod = JsonLite.Parse(jsonBlock);
                if (jmod != null && jmod.ObjectValue != null)
                {
                    modifiers = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var kvp in jmod.ObjectValue)
                        modifiers[kvp.Key] = kvp.Value.AsString();
                }
            }

            if (x < 0 || y < 0 || x >= level.Width || y >= level.Height) return;

            var inst = items.CreateInstance(itemId, count, modifiers);
            if (inst != null)
            {
                int slotIdx = Math.Max(0, Math.Min(3, subSquare - 1)); // 1..4 -> 0..3
                level.Tiles[y, x].Slots[slotIdx] = inst;
            }
        }

        private static void ParseMonsterLine(string line, DungeonLevel level, EntityRegistry entities = null)
        {
            // Syntax: [x, y, facing, level] monsterID {optional json metadata}
            int closeBracket = line.IndexOf(']');
            int openBracket = line.IndexOf('[');
            if (openBracket < 0 || closeBracket < openBracket) return;

            string coords = line.Substring(openBracket + 1, closeBracket - openBracket - 1);
            string remainder = line.Substring(closeBracket + 1).Trim();

            string[] cparts = coords.Split(',');
            if (cparts.Length < 2) return;

            if (!int.TryParse(cparts[0].Trim(), out int x)) return;
            if (!int.TryParse(cparts[1].Trim(), out int y)) return;

            double angle = 0.0;
            if (cparts.Length >= 3)
            {
                string f = cparts[2].Trim().ToUpperInvariant();
                if (f == "N") angle = 1.5 * Math.PI;
                else if (f == "S") angle = 0.5 * Math.PI;
                else if (f == "E") angle = 0.0;
                else if (f == "W") angle = Math.PI;
                else if (double.TryParse(f, out double parsedDeg))
                    angle = parsedDeg * Math.PI / 180.0;
            }

            int lvl = 1;
            if (cparts.Length >= 4 && int.TryParse(cparts[3].Trim(), out int pl))
                lvl = Math.Max(1, pl);

            if (x < 0 || y < 0 || x >= level.Width || y >= level.Height) return;

            string monsterId = remainder;
            JsonNode metaJson = null;
            int jsonIdx = remainder.IndexOf('{');
            if (jsonIdx >= 0)
            {
                monsterId = remainder.Substring(0, jsonIdx).Trim();
                string jsonText = remainder.Substring(jsonIdx).Trim();
                metaJson = JsonLite.Parse(jsonText);
            }

            var def = entities?.Get(monsterId);
            var stats = def?.GetStatsForLevel(lvl);

            var monster = new MonsterInstance(monsterId, def?.Name ?? monsterId)
            {
                TileX = x,
                TileY = y,
                FacingAngle = angle,
                Level = lvl,
                HP = stats?.HP ?? 30,
                MaxHP = stats?.HP ?? 30,
                MP = stats?.MP ?? 0,
                MaxMP = stats?.MP ?? 0,
                MinDamage = stats?.MinDamage ?? 3,
                MaxDamage = stats?.MaxDamage ?? 6,
                Armor = stats?.Armor ?? 0,
                Speed = stats?.Speed ?? 10,
                XpReward = stats?.XpReward ?? 15,
                SpriteKind = def?.SpriteKind ?? "skeleton"
            };
            if (stats?.Abilities != null)
            {
                monster.ActiveAbilities.AddRange(stats.Abilities);
            }

            if (metaJson != null)
            {
                monster.IsUnique = metaJson.GetBool("unique", false);
                monster.UniqueTitle = metaJson.GetString("title", "");
                if (monster.IsUnique && !string.IsNullOrEmpty(monster.UniqueTitle))
                    monster.Name = monster.UniqueTitle;

                monster.DropArchetypeLoot = metaJson.GetBool("archetypeLoot", true);
                monster.DropZoneLoot = metaJson.GetBool("zoneLoot", true);
                monster.CustomLootTable = metaJson.GetString("customLoot", "");

                var gArr = metaJson.Get("guaranteed");
                if (gArr?.ArrayValue != null)
                {
                    foreach (var itemNode in gArr.ArrayValue)
                    {
                        string gId = itemNode.StringValue?.Trim();
                        if (!string.IsNullOrEmpty(gId)) monster.GuaranteedDrops.Add(gId);
                    }
                }

                // Movement & Routing
                string moveModeStr = metaJson.GetString("moveMode", "Static");
                if (Enum.TryParse<EntityMovementMode>(moveModeStr, true, out var parsedMode))
                    monster.MovementMode = parsedMode;

                var wpArr = metaJson.Get("waypoints");
                if (wpArr?.ArrayValue != null)
                {
                    foreach (var ptNode in wpArr.ArrayValue)
                    {
                        if (ptNode.ArrayValue != null && ptNode.ArrayValue.Count >= 2)
                        {
                            int wx = (int)ptNode.ArrayValue[0].NumberValue;
                            int wy = (int)ptNode.ArrayValue[1].NumberValue;
                            monster.PatrolWaypoints.Add(new Point(wx, wy));
                        }
                    }
                }

                string circuitStr = metaJson.GetString("circuit", "Loop");
                if (Enum.TryParse<PatrolCircuitStyle>(circuitStr, true, out var parsedCircuit))
                    monster.CircuitStyle = parsedCircuit;

                monster.WaypointPauseSeconds = metaJson.GetDouble("waypointPause", 2.0);
                monster.WanderRadius = metaJson.GetInt("wanderRadius", 3);
                monster.WanderIntervalSeconds = metaJson.GetDouble("wanderInterval", 3.0);

                var wtArr = metaJson.Get("wanderTiles");
                if (wtArr?.ArrayValue != null)
                {
                    foreach (var ptNode in wtArr.ArrayValue)
                    {
                        if (ptNode.ArrayValue != null && ptNode.ArrayValue.Count >= 2)
                        {
                            int wx = (int)ptNode.ArrayValue[0].NumberValue;
                            int wy = (int)ptNode.ArrayValue[1].NumberValue;
                            monster.WanderAreaTiles.Add(new Point(wx, wy));
                        }
                    }
                }

                monster.MaxChaseDistance = metaJson.GetInt("maxChase", 5);
                monster.ReturnToPreCombatSquare = metaJson.GetBool("returnPreCombat", true);
            }

            level.Tiles[y, x].CenterOccupant = monster;
            level.Monsters.Add(monster);
        }

        private static void ParseZoneConfig(JsonNode node, ZoneConfig cfg)
        {
            var lNode = node.Get("lighting");
            if (lNode != null)
            {
                cfg.EnableLighting = lNode.GetBool("enabled", cfg.EnableLighting);
                cfg.EnableShadows = lNode.GetBool("shadows", cfg.EnableShadows);
                cfg.DarkMaxBrightness = (float)lNode.GetDouble("darkMaxBrightness", cfg.DarkMaxBrightness);
                cfg.InvertColorsInDarkness = lNode.GetBool("invertDarkness", cfg.InvertColorsInDarkness);
                cfg.MaxViewDepth = (float)lNode.GetDouble("maxViewDepth", cfg.MaxViewDepth);
                cfg.MinViewDepth = (float)lNode.GetDouble("minViewDepth", cfg.MinViewDepth);
                cfg.SconceInnerRadius = lNode.GetDouble("sconceInnerRadius", cfg.SconceInnerRadius);
                cfg.SconceOuterRadius = lNode.GetDouble("sconceOuterRadius", cfg.SconceOuterRadius);
                cfg.LanternColor = ZoneConfig.ParseColorHex(lNode.GetString("lanternColor", ""), cfg.LanternColor);
                cfg.AmbientLight = (float)lNode.GetDouble("ambientLight", cfg.AmbientLight);
            }

            var envNode = node.Get("environment");
            if (envNode != null)
            {
                cfg.FloorColor = ZoneConfig.ParseColorHex(envNode.GetString("floorColor", ""), cfg.FloorColor);
                cfg.CeilingColor = ZoneConfig.ParseColorHex(envNode.GetString("ceilingColor", ""), cfg.CeilingColor);
                cfg.ShowFloorGrid = envNode.GetBool("showFloorGrid", cfg.ShowFloorGrid);
                cfg.FloorGridColor = ZoneConfig.ParseColorHex(envNode.GetString("floorGridColor", ""), cfg.FloorGridColor);
            }
        }

        private static ZoneConfig CloneConfig(ZoneConfig src)
        {
            return new ZoneConfig
            {
                EnableLighting = src.EnableLighting,
                EnableShadows = src.EnableShadows,
                DarkMaxBrightness = src.DarkMaxBrightness,
                InvertColorsInDarkness = src.InvertColorsInDarkness,
                MaxViewDepth = src.MaxViewDepth,
                MinViewDepth = src.MinViewDepth,
                SconceInnerRadius = src.SconceInnerRadius,
                SconceOuterRadius = src.SconceOuterRadius,
                LanternColor = src.LanternColor,
                AmbientLight = src.AmbientLight,
                FloorColor = src.FloorColor,
                CeilingColor = src.CeilingColor,
                ShowFloorGrid = src.ShowFloorGrid,
                FloorGridColor = src.FloorGridColor
            };
        }
    }

    /// <summary>
    /// Zero-dependency lightweight JSON parser for game project files.
    /// Supports objects, arrays, strings, numbers, booleans, and nulls.
    /// </summary>
    public sealed class JsonNode
    {
        public Dictionary<string, JsonNode> ObjectValue;
        public List<JsonNode> ArrayValue;
        public string StringValue;
        public double NumberValue;
        public bool BoolValue;
        public bool IsNull;

        public JsonNode Get(string key)
        {
            if (ObjectValue == null) return null;
            if (ObjectValue.TryGetValue(key, out var val))
                return val;
            // Fallback to case-insensitive match for general config properties
            foreach (var kvp in ObjectValue)
            {
                if (kvp.Key.Equals(key, StringComparison.OrdinalIgnoreCase))
                    return kvp.Value;
            }
            return null;
        }

        public string GetString(string key, string defVal)
        {
            var node = Get(key);
            return node != null && node.StringValue != null ? node.StringValue : defVal;
        }

        public int GetInt(string key, int defVal)
        {
            var node = Get(key);
            return node != null ? (int)node.NumberValue : defVal;
        }

        public double GetDouble(string key, double defVal)
        {
            var node = Get(key);
            return node != null ? node.NumberValue : defVal;
        }

        public bool GetBool(string key, bool defVal)
        {
            var node = Get(key);
            return node != null ? node.BoolValue : defVal;
        }

        public string AsString()
        {
            if (StringValue != null) return StringValue;
            if (IsNull) return "null";
            if (BoolValue) return "true";
            if (NumberValue != 0.0) return NumberValue.ToString();
            return "";
        }
    }

    public static class JsonLite
    {
        public static JsonNode Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json)) return null;
            int pos = 0;
            return ParseValue(json, ref pos);
        }

        private static void SkipWhitespace(string s, ref int pos)
        {
            while (pos < s.Length)
            {
                char c = s[pos];
                if (char.IsWhiteSpace(c)) { pos++; continue; }
                if (c == '/' && pos + 1 < s.Length)
                {
                    if (s[pos + 1] == '/')
                    {
                        pos += 2;
                        while (pos < s.Length && s[pos] != '\n') pos++;
                        continue;
                    }
                    if (s[pos + 1] == '*')
                    {
                        pos += 2;
                        while (pos + 1 < s.Length && !(s[pos] == '*' && s[pos + 1] == '/')) pos++;
                        pos += 2;
                        continue;
                    }
                }
                break;
            }
        }

        private static JsonNode ParseValue(string s, ref int pos)
        {
            SkipWhitespace(s, ref pos);
            if (pos >= s.Length) return null;

            char c = s[pos];
            if (c == '{') return ParseObject(s, ref pos);
            if (c == '[') return ParseArray(s, ref pos);
            if (c == '"' || c == '\'') return ParseString(s, ref pos);
            if (char.IsDigit(c) || c == '-') return ParseNumber(s, ref pos);
            if (c == 't' || c == 'T' || c == 'f' || c == 'F') return ParseBool(s, ref pos);
            if (c == 'n' || c == 'N') return ParseNull(s, ref pos);

            pos++;
            return null;
        }

        private static JsonNode ParseObject(string s, ref int pos)
        {
            pos++; // '{'
            var node = new JsonNode { ObjectValue = new Dictionary<string, JsonNode>(StringComparer.Ordinal) };

            while (pos < s.Length)
            {
                SkipWhitespace(s, ref pos);
                if (pos >= s.Length) break;
                if (s[pos] == '}') { pos++; break; }

                string key = ParseRawString(s, ref pos);
                SkipWhitespace(s, ref pos);
                if (pos < s.Length && s[pos] == ':') pos++;

                var val = ParseValue(s, ref pos);
                if (key != null) node.ObjectValue[key] = val;

                SkipWhitespace(s, ref pos);
                if (pos < s.Length && s[pos] == ',') pos++;
            }
            return node;
        }

        private static JsonNode ParseArray(string s, ref int pos)
        {
            pos++; // '['
            var node = new JsonNode { ArrayValue = new List<JsonNode>() };

            while (pos < s.Length)
            {
                SkipWhitespace(s, ref pos);
                if (pos >= s.Length) break;
                if (s[pos] == ']') { pos++; break; }

                var val = ParseValue(s, ref pos);
                if (val != null) node.ArrayValue.Add(val);

                SkipWhitespace(s, ref pos);
                if (pos < s.Length && s[pos] == ',') pos++;
            }
            return node;
        }

        private static string ParseRawString(string s, ref int pos)
        {
            SkipWhitespace(s, ref pos);
            if (pos >= s.Length) return "";
            char quote = s[pos];
            if (quote != '"' && quote != '\'') return "";
            pos++;

            var sb = new StringBuilder();
            while (pos < s.Length)
            {
                char c = s[pos++];
                if (c == quote) break;
                if (c == '\\' && pos < s.Length)
                {
                    char esc = s[pos++];
                    if (esc == 'n') sb.Append('\n');
                    else if (esc == 'r') sb.Append('\r');
                    else if (esc == 't') sb.Append('\t');
                    else sb.Append(esc);
                }
                else
                {
                    sb.Append(c);
                }
            }
            return sb.ToString();
        }

        private static JsonNode ParseString(string s, ref int pos)
        {
            return new JsonNode { StringValue = ParseRawString(s, ref pos) };
        }

        private static JsonNode ParseNumber(string s, ref int pos)
        {
            int start = pos;
            while (pos < s.Length && (char.IsDigit(s[pos]) || s[pos] == '.' || s[pos] == '-' || s[pos] == 'e' || s[pos] == 'E'))
                pos++;

            string numStr = s.Substring(start, pos - start);
            double.TryParse(numStr, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out double num);
            return new JsonNode { NumberValue = num };
        }

        private static JsonNode ParseBool(string s, ref int pos)
        {
            int start = pos;
            while (pos < s.Length && char.IsLetter(s[pos])) pos++;
            string str = s.Substring(start, pos - start).ToLowerInvariant();
            return new JsonNode { BoolValue = (str == "true") };
        }

        private static JsonNode ParseNull(string s, ref int pos)
        {
            while (pos < s.Length && char.IsLetter(s[pos])) pos++;
            return new JsonNode { IsNull = true };
        }
    }

    public sealed class ModuleInfo
    {
        public string FilePath { get; set; }
        public string FileName { get; set; }
        public string Title { get; set; }
        public string StartDungeonId { get; set; } = "dungeon_01";
        public int StartLevelNumber { get; set; } = 1;
        public bool IsGlpPackage { get; set; } = true;
        public int DungeonCount { get; set; } = 1;

        public override string ToString()
        {
            string tag = IsGlpPackage ? "[GLP]" : "[DIR]";
            return $"{tag} {Title} ({FileName})";
        }
    }

    public static class GlpPackage
    {
        public static readonly byte[] MagicBytes = new byte[] { (byte)'G', (byte)'L', (byte)'P', 0x01 };

        public static void CreateFromDirectory(string projectDir, string glpPath)
        {
            if (!Directory.Exists(projectDir))
                throw new DirectoryNotFoundException($"Directory not found: {projectDir}");

            string targetDir = Path.GetDirectoryName(glpPath);
            if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
                Directory.CreateDirectory(targetDir);

            string tempGlp = glpPath + ".tmp";
            if (File.Exists(tempGlp)) File.Delete(tempGlp);

            using (var fs = new FileStream(tempGlp, FileMode.Create, FileAccess.Write, FileShare.None))
            {
                // Write magic header: 'G','L','P', 0x01
                fs.Write(MagicBytes, 0, MagicBytes.Length);

                using (var archive = new ZipArchive(fs, ZipArchiveMode.Create, leaveOpen: true))
                {
                    var allFiles = Directory.GetFiles(projectDir, "*.*", SearchOption.AllDirectories);
                    foreach (var file in allFiles)
                    {
                        string ext = Path.GetExtension(file).ToLowerInvariant();
                        if (ext == ".tmp" || ext == ".bak") continue;

                        string relPath = file.Substring(projectDir.Length).TrimStart('\\', '/').Replace('\\', '/');
                        var entry = archive.CreateEntry(relPath, CompressionLevel.Optimal);
                        using (var entryStream = entry.Open())
                        using (var fileStream = File.OpenRead(file))
                        {
                            fileStream.CopyTo(entryStream);
                        }
                    }
                }
            }

            if (File.Exists(glpPath)) File.Delete(glpPath);
            File.Move(tempGlp, glpPath);
        }

        public static Dictionary<string, string> ReadAllTextEntries(string glpPath)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            using (var fs = File.OpenRead(glpPath))
            {
                byte[] header = new byte[4];
                int read = fs.Read(header, 0, 4);
                if (read == 4 && header[0] == MagicBytes[0] && header[1] == MagicBytes[1] &&
                    header[2] == MagicBytes[2] && header[3] == MagicBytes[3])
                {
                    // Stream positioned right after magic header
                }
                else
                {
                    // Fallback to offset 0 for plain zip
                    fs.Position = 0;
                }

                using (var archive = new ZipArchive(fs, ZipArchiveMode.Read, leaveOpen: true))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (string.IsNullOrEmpty(entry.Name) && entry.FullName.EndsWith("/")) continue;

                        string key = entry.FullName.Replace('\\', '/');
                        using (var reader = new StreamReader(entry.Open(), Encoding.UTF8))
                        {
                            result[key] = reader.ReadToEnd();
                        }
                    }
                }
            }
            return result;
        }

        public static List<ModuleInfo> ScanModules(string modulesDir)
        {
            var list = new List<ModuleInfo>();
            if (!Directory.Exists(modulesDir))
            {
                try { Directory.CreateDirectory(modulesDir); } catch { }
                return list;
            }

            // 1. Scan for .glp packages
            var glpFiles = Directory.GetFiles(modulesDir, "*.glp");
            foreach (var glpFile in glpFiles)
            {
                try
                {
                    var info = new ModuleInfo
                    {
                        FilePath = glpFile,
                        FileName = Path.GetFileName(glpFile),
                        Title = Path.GetFileNameWithoutExtension(glpFile),
                        IsGlpPackage = true
                    };

                    var entries = ReadAllTextEntries(glpFile);
                    if (entries.TryGetValue("project.json", out var pJson))
                    {
                        var json = JsonLite.Parse(pJson);
                        if (json != null)
                        {
                            info.Title = json.GetString("title", info.Title);
                            info.StartDungeonId = json.GetString("startDungeon", "dungeon_01");
                            info.StartLevelNumber = json.GetInt("startLevel", 1);
                        }
                    }

                    var dIds = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
                    foreach (var key in entries.Keys)
                    {
                        if (key.StartsWith("dungeons/", StringComparison.OrdinalIgnoreCase))
                        {
                            var parts = key.Split('/');
                            if (parts.Length >= 2 && !string.IsNullOrEmpty(parts[1]))
                                dIds.Add(parts[1]);
                        }
                    }
                    info.DungeonCount = Math.Max(1, dIds.Count);

                    list.Add(info);
                }
                catch { }
            }

            // 2. Scan for unpacked module directories (for developers)
            var dirs = Directory.GetDirectories(modulesDir);
            foreach (var dir in dirs)
            {
                try
                {
                    string pJsonPath = Path.Combine(dir, "project.json");
                    if (File.Exists(pJsonPath) || Directory.Exists(Path.Combine(dir, "dungeons")))
                    {
                        var info = new ModuleInfo
                        {
                            FilePath = dir,
                            FileName = Path.GetFileName(dir),
                            Title = Path.GetFileName(dir),
                            IsGlpPackage = false
                        };

                        if (File.Exists(pJsonPath))
                        {
                            var json = JsonLite.Parse(File.ReadAllText(pJsonPath));
                            if (json != null)
                            {
                                info.Title = json.GetString("title", info.Title);
                                info.StartDungeonId = json.GetString("startDungeon", "dungeon_01");
                                info.StartLevelNumber = json.GetInt("startLevel", 1);
                            }
                        }

                        string dDir = Path.Combine(dir, "dungeons");
                        if (Directory.Exists(dDir))
                        {
                            info.DungeonCount = Directory.GetDirectories(dDir).Length;
                        }

                        list.Add(info);
                    }
                }
                catch { }
            }

            return list;
        }
    }
}
