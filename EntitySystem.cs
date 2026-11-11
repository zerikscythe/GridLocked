using System;
using System.Collections.Generic;
using System.Drawing;
using System.Text;

namespace GridLocked
{
    public enum EntityDisposition
    {
        Hostile = 0,
        Neutral = 1,
        Friendly = 2,
        Merchant = 3,
        Companion = 4
    }

    public enum StatTweenCurve
    {
        Linear = 0,
        Exponential = 1,
        SmoothStep = 2,
        Logarithmic = 3
    }

    /// <summary>
    /// Snapshot of all combat attributes, stats, and active power/spell ranks for an entity at a specific level.
    /// </summary>
    public sealed class EntityLevelStats
    {
        public int Level { get; set; } = 1;
        public int HP { get; set; } = 30;
        public int MP { get; set; } = 0;
        public int MinDamage { get; set; } = 3;
        public int MaxDamage { get; set; } = 6;
        public int Armor { get; set; } = 0;
        public int Speed { get; set; } = 10;
        public int XpReward { get; set; } = 15;
        public List<string> Abilities { get; set; } = new List<string>();

        public EntityLevelStats Clone()
        {
            var clone = (EntityLevelStats)MemberwiseClone();
            clone.Abilities = new List<string>(Abilities);
            return clone;
        }

        public string GetAbilitiesSummary()
        {
            if (Abilities == null || Abilities.Count == 0) return "(None)";
            return string.Join(", ", Abilities);
        }
    }

    /// <summary>
    /// Configures an ability/spell unlock and rank scaling across entity levels.
    /// E.g. "Flare", unlocks at Level 5 (Rank 1), scales to Rank 6 at Level 20.
    /// </summary>
    public sealed class EntityAbilityProgression
    {
        public string AbilityId { get; set; } = "ability_flare";
        public string Name { get; set; } = "Flare";
        public int UnlockLevel { get; set; } = 5;
        public int StartRank { get; set; } = 1;
        public int MaxRank { get; set; } = 6;
        public int MaxRankLevel { get; set; } = 20;

        public EntityAbilityProgression() { }

        public EntityAbilityProgression(string id, string name, int unlockLevel, int startRank = 1, int maxRank = 5, int maxRankLevel = 20)
        {
            AbilityId = id;
            Name = name;
            UnlockLevel = unlockLevel;
            StartRank = startRank;
            MaxRank = maxRank;
            MaxRankLevel = Math.Max(unlockLevel + 1, maxRankLevel);
        }

        public int GetRankAtLevel(int level)
        {
            if (level < UnlockLevel) return 0;
            if (level >= MaxRankLevel) return MaxRank;
            double progress = (double)(level - UnlockLevel) / Math.Max(1, (MaxRankLevel - UnlockLevel));
            int rank = StartRank + (int)Math.Round(progress * (MaxRank - StartRank));
            return Math.Max(StartRank, Math.Min(MaxRank, rank));
        }

        public string GetRankString(int level)
        {
            int rank = GetRankAtLevel(level);
            if (rank <= 0) return null;
            return $"{Name} (Rank {rank})";
        }

        public EntityAbilityProgression Clone()
        {
            return (EntityAbilityProgression)MemberwiseClone();
        }
    }

    /// <summary>
    /// Stores the min/max anchors and curve interpolation models used to auto-tween entity stats.
    /// </summary>
    public sealed class EntityCurveConfig
    {
        public int MinLevel { get; set; } = 1;
        public int MaxLevel { get; set; } = 20;

        public EntityLevelStats MinStats { get; set; } = new EntityLevelStats
        {
            Level = 1,
            HP = 25,
            MP = 0,
            MinDamage = 3,
            MaxDamage = 6,
            Armor = 1,
            Speed = 10,
            XpReward = 15
        };

        public EntityLevelStats MaxStats { get; set; } = new EntityLevelStats
        {
            Level = 20,
            HP = 500,
            MP = 80,
            MinDamage = 40,
            MaxDamage = 55,
            Armor = 18,
            Speed = 16,
            XpReward = 850
        };

        public StatTweenCurve CurveStyle { get; set; } = StatTweenCurve.SmoothStep;

        public EntityCurveConfig Clone()
        {
            return new EntityCurveConfig
            {
                MinLevel = MinLevel,
                MaxLevel = MaxLevel,
                MinStats = MinStats.Clone(),
                MaxStats = MaxStats.Clone(),
                CurveStyle = CurveStyle
            };
        }
    }

    /// <summary>
    /// Definition and progression blueprint for any in-game creature or NPC (friendly, hostile, neutral, merchant).
    /// </summary>
    public sealed class EntityDefinition
    {
        public string Id { get; set; } = "skeleton_warrior";
        public string Name { get; set; } = "Skeleton Warrior";
        public EntityDisposition Disposition { get; set; } = EntityDisposition.Hostile;
        public string Category { get; set; } = "Undead"; // Humanoid, Elf, Beast, Dragon, Undead, Construct, Alien, Mech, Cyber, Eldritch
        public string SpriteKind { get; set; } = "skeleton";
        public Color TintColor { get; set; } = Color.White;
        public string Description { get; set; } = "An animate skeleton wielding ancient arms.";

        public bool IsHostile => Disposition == EntityDisposition.Hostile;

        public EntityCurveConfig CurveConfig { get; set; } = new EntityCurveConfig();
        public List<EntityAbilityProgression> Abilities { get; } = new List<EntityAbilityProgression>();
        public Dictionary<int, EntityLevelStats> LevelTable { get; } = new Dictionary<int, EntityLevelStats>();

        public EntityDefinition()
        {
            AutoTweenLevels();
        }

        public EntityDefinition(string id, string name, string category, EntityDisposition disposition = EntityDisposition.Hostile, string spriteKind = "skeleton")
        {
            Id = id;
            Name = name;
            Category = category;
            Disposition = disposition;
            SpriteKind = spriteKind;
            AutoTweenLevels();
        }

        public EntityLevelStats GetStatsForLevel(int level)
        {
            if (LevelTable.TryGetValue(level, out var stats))
                return stats;

            int minL = CurveConfig.MinLevel;
            int maxL = CurveConfig.MaxLevel;
            if (level <= minL && LevelTable.TryGetValue(minL, out var minStats)) return minStats;
            if (level >= maxL && LevelTable.TryGetValue(maxL, out var maxStats)) return maxStats;

            return InterpolateLevel(level, (double)(level - minL) / Math.Max(1, maxL - minL));
        }

        /// <summary>
        /// Recalculates and fills LevelTable from MinLevel to MaxLevel using the curve configuration.
        /// Also updates the active ability ranks at every level.
        /// </summary>
        public void AutoTweenLevels()
        {
            LevelTable.Clear();
            int minL = CurveConfig.MinLevel;
            int maxL = Math.Max(minL, CurveConfig.MaxLevel);
            int range = Math.Max(1, maxL - minL);

            for (int lvl = minL; lvl <= maxL; lvl++)
            {
                double rawT = (double)(lvl - minL) / range;
                double t = EvaluateCurve(rawT, CurveConfig.CurveStyle);

                var row = InterpolateLevel(lvl, t);
                UpdateAbilitiesForRow(row);
                LevelTable[lvl] = row;
            }
        }

        /// <summary>
        /// Updates the abilities string list for each row based on the current Abilities list without resetting custom stat tweaks.
        /// </summary>
        public void SyncAbilitiesAcrossAllLevels()
        {
            foreach (var kvp in LevelTable)
            {
                UpdateAbilitiesForRow(kvp.Value);
            }
        }

        private void UpdateAbilitiesForRow(EntityLevelStats row)
        {
            row.Abilities.Clear();
            foreach (var ab in Abilities)
            {
                string rankStr = ab.GetRankString(row.Level);
                if (!string.IsNullOrEmpty(rankStr))
                {
                    row.Abilities.Add(rankStr);
                }
            }
        }

        private EntityLevelStats InterpolateLevel(int level, double t)
        {
            var a = CurveConfig.MinStats;
            var b = CurveConfig.MaxStats;

            double hpT = CurveConfig.CurveStyle == StatTweenCurve.Linear ? t : Math.Pow(t, 1.4);
            double xpT = Math.Pow(t, 1.6);

            return new EntityLevelStats
            {
                Level = level,
                HP = (int)Math.Round(a.HP + (b.HP - a.HP) * hpT),
                MP = (int)Math.Round(a.MP + (b.MP - a.MP) * t),
                MinDamage = (int)Math.Round(a.MinDamage + (b.MinDamage - a.MinDamage) * t),
                MaxDamage = (int)Math.Round(a.MaxDamage + (b.MaxDamage - a.MaxDamage) * t),
                Armor = (int)Math.Round(a.Armor + (b.Armor - a.Armor) * t),
                Speed = (int)Math.Round(a.Speed + (b.Speed - a.Speed) * t),
                XpReward = (int)Math.Round(a.XpReward + (b.XpReward - a.XpReward) * xpT)
            };
        }

        public static double EvaluateCurve(double t, StatTweenCurve curve)
        {
            t = Math.Max(0.0, Math.Min(1.0, t));
            switch (curve)
            {
                case StatTweenCurve.Linear:
                    return t;
                case StatTweenCurve.Exponential:
                    return t * t;
                case StatTweenCurve.SmoothStep:
                    return t * t * (3.0 - 2.0 * t);
                case StatTweenCurve.Logarithmic:
                    return Math.Sqrt(t);
                default:
                    return t;
            }
        }

        public EntityDefinition Clone()
        {
            var clone = new EntityDefinition
            {
                Id = Id,
                Name = Name,
                Disposition = Disposition,
                Category = Category,
                SpriteKind = SpriteKind,
                TintColor = TintColor,
                Description = Description,
                CurveConfig = CurveConfig.Clone()
            };
            clone.Abilities.Clear();
            foreach (var ab in Abilities)
                clone.Abilities.Add(ab.Clone());

            clone.LevelTable.Clear();
            foreach (var kvp in LevelTable)
                clone.LevelTable[kvp.Key] = kvp.Value.Clone();

            return clone;
        }
    }

    /// <summary>
    /// Registry managing all Entity blueprints (NPCs, creatures, hostiles, merchants).
    /// </summary>
    public sealed class EntityRegistry
    {
        private readonly Dictionary<string, EntityDefinition> _entities =
            new Dictionary<string, EntityDefinition>(StringComparer.OrdinalIgnoreCase);

        public EntityRegistry()
        {
            RegisterBuiltins();
        }

        public void Register(EntityDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) return;
            _entities[def.Id] = def;
        }

        public EntityDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _entities.TryGetValue(id, out var def);
            return def;
        }

        public IEnumerable<EntityDefinition> GetAll() => _entities.Values;

        public void Clear() => _entities.Clear();

        public void RegisterBuiltins()
        {
            // 1. Hostile Undead: Skeleton Warrior
            var skel = new EntityDefinition("skeleton_warrior", "Skeleton Warrior", "Undead", EntityDisposition.Hostile, "skeleton")
            {
                Description = "A reanimated skeleton soldier wielding ancient arms."
            };
            skel.CurveConfig.MinStats.HP = 25;
            skel.CurveConfig.MinStats.MinDamage = 3;
            skel.CurveConfig.MinStats.MaxDamage = 6;
            skel.CurveConfig.MinStats.Armor = 1;
            skel.CurveConfig.MinStats.Speed = 10;
            skel.CurveConfig.MinStats.XpReward = 15;

            skel.CurveConfig.MaxStats.HP = 480;
            skel.CurveConfig.MaxStats.MinDamage = 38;
            skel.CurveConfig.MaxStats.MaxDamage = 52;
            skel.CurveConfig.MaxStats.Armor = 16;
            skel.CurveConfig.MaxStats.Speed = 14;
            skel.CurveConfig.MaxStats.XpReward = 750;

            skel.Abilities.Add(new EntityAbilityProgression("rend", "Bone Rend", 4, startRank: 1, maxRank: 5, maxRankLevel: 20));
            skel.Abilities.Add(new EntityAbilityProgression("shield", "Shield Wall", 10, startRank: 1, maxRank: 3, maxRankLevel: 20));
            skel.AutoTweenLevels();
            Register(skel);

            // 2. Hostile Beast: Dire Wolf
            var wolf = new EntityDefinition("dire_wolf", "Dire Wolf", "Beast", EntityDisposition.Hostile, "wolf")
            {
                Description = "A massive feral predator with glowing eyes and razor fangs."
            };
            wolf.CurveConfig.MinStats.HP = 22;
            wolf.CurveConfig.MinStats.MinDamage = 4;
            wolf.CurveConfig.MinStats.MaxDamage = 7;
            wolf.CurveConfig.MinStats.Speed = 15;
            wolf.CurveConfig.MinStats.XpReward = 18;

            wolf.CurveConfig.MaxStats.HP = 440;
            wolf.CurveConfig.MaxStats.MinDamage = 36;
            wolf.CurveConfig.MaxStats.MaxDamage = 50;
            wolf.CurveConfig.MaxStats.Speed = 24;
            wolf.CurveConfig.MaxStats.XpReward = 720;

            wolf.Abilities.Add(new EntityAbilityProgression("feral_pounce", "Pounce", 3, startRank: 1, maxRank: 5, maxRankLevel: 20));
            wolf.Abilities.Add(new EntityAbilityProgression("howl", "Terrifying Howl", 7, startRank: 1, maxRank: 3, maxRankLevel: 20));
            wolf.AutoTweenLevels();
            Register(wolf);

            // 3. Friendly NPC: Town Guard
            var guard = new EntityDefinition("town_guard", "City Sentry", "Humanoid", EntityDisposition.Friendly, "humanoid")
            {
                Description = "A vigilant city sentinel watching over travelers."
            };
            guard.CurveConfig.MinStats.HP = 40;
            guard.CurveConfig.MinStats.MinDamage = 5;
            guard.CurveConfig.MinStats.MaxDamage = 8;
            guard.CurveConfig.MinStats.Armor = 4;
            guard.CurveConfig.MinStats.Speed = 11;

            guard.CurveConfig.MaxStats.HP = 600;
            guard.CurveConfig.MaxStats.MinDamage = 45;
            guard.CurveConfig.MaxStats.MaxDamage = 62;
            guard.CurveConfig.MaxStats.Armor = 25;
            guard.CurveConfig.MaxStats.Speed = 15;

            guard.Abilities.Add(new EntityAbilityProgression("taunt", "Challenging Shout", 2, startRank: 1, maxRank: 5, maxRankLevel: 20));
            guard.AutoTweenLevels();
            Register(guard);

            // 4. Merchant NPC: Elven Arcanist
            var elf = new EntityDefinition("elven_trader", "Elven Trader", "Elf", EntityDisposition.Merchant, "elf")
            {
                Description = "A traveling elven antiquarian trading scrolls and curiosities."
            };
            elf.CurveConfig.MinStats.HP = 30;
            elf.CurveConfig.MinStats.MP = 50;
            elf.CurveConfig.MinStats.MinDamage = 2;
            elf.CurveConfig.MinStats.MaxDamage = 5;

            elf.CurveConfig.MaxStats.HP = 350;
            elf.CurveConfig.MaxStats.MP = 300;
            elf.CurveConfig.MaxStats.MinDamage = 25;
            elf.CurveConfig.MaxStats.MaxDamage = 38;

            elf.Abilities.Add(new EntityAbilityProgression("teleport", "Blink", 5, startRank: 1, maxRank: 4, maxRankLevel: 20));
            elf.AutoTweenLevels();
            Register(elf);

            // 5. Hostile Dragon: Ash Drake
            var drake = new EntityDefinition("ash_drake", "Ash Drake", "Dragon", EntityDisposition.Hostile, "dragon")
            {
                Description = "A scaled wyrm exhaling sulfurous fumes and cinder flares."
            };
            drake.CurveConfig.MinStats.HP = 65;
            drake.CurveConfig.MinStats.MP = 30;
            drake.CurveConfig.MinStats.MinDamage = 8;
            drake.CurveConfig.MinStats.MaxDamage = 14;
            drake.CurveConfig.MinStats.Armor = 5;
            drake.CurveConfig.MinStats.Speed = 12;
            drake.CurveConfig.MinStats.XpReward = 45;

            drake.CurveConfig.MaxStats.HP = 1200;
            drake.CurveConfig.MaxStats.MP = 250;
            drake.CurveConfig.MaxStats.MinDamage = 75;
            drake.CurveConfig.MaxStats.MaxDamage = 110;
            drake.CurveConfig.MaxStats.Armor = 32;
            drake.CurveConfig.MaxStats.Speed = 18;
            drake.CurveConfig.MaxStats.XpReward = 2200;

            drake.Abilities.Add(new EntityAbilityProgression("fire_breath", "Flame Breath", 1, startRank: 1, maxRank: 6, maxRankLevel: 20));
            drake.Abilities.Add(new EntityAbilityProgression("tail_sweep", "Tail Sweep", 6, startRank: 1, maxRank: 4, maxRankLevel: 20));
            drake.AutoTweenLevels();
            Register(drake);
        }
    }
}
