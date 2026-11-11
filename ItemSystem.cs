using System;
using System.Collections.Generic;
using System.Drawing;

namespace GridLocked
{
    /// <summary>
    /// Represents a discrete affect, proc, or modifier attached to an item or attack.
    /// Genre-agnostic: can represent magical enchantments, sci-fi plasma coils, cybernetic mods, or toxic coatings.
    /// </summary>
    public sealed class ItemEffect
    {
        public string Name { get; set; } = string.Empty;
        public string EnergyType { get; set; } = "Physical"; // Physical, Fire, Frost, Lightning, Toxic, Plasma, Laser, Arcane, EMP, Sonic, Void
        public int Power { get; set; } = 0;
        public float Chance { get; set; } = 1.0f; // 0.0 to 1.0 proc rate
        public int Duration { get; set; } = 0;    // turns or seconds
        public int ResourceCost { get; set; } = 0;
        public string Description { get; set; } = string.Empty;

        public ItemEffect Clone() => (ItemEffect)MemberwiseClone();
    }

    /// <summary>
    /// Archetype definition for an item, obstacle, or container.
    /// Loaded from items.json with support for template inheritance ("inherit": "base_id").
    /// </summary>
    public sealed class ItemDefinition
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public string Inherit { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        /// <summary>True if this entry acts as a base archetype template for other items.</summary>
        public bool IsTemplate { get; set; } = false;

        /// <summary>Primary Category: "Weapon", "Armor", "Accessory", "Consumable", "Container", "Prop", etc.</summary>
        public string Category { get; set; } = "Prop";

        /// <summary>Sub-type: e.g. "Sword", "Dagger", "Bow", "Staff", "Shield", "Helmet", "Chest", "Potion", "Scroll", "Chest", "Rock".</summary>
        public string SubCategory { get; set; } = string.Empty;

        // --- Physical & Economy ---
        public float Weight { get; set; } = 1.0f;
        public int Value { get; set; } = 10;
        public bool Collectable { get; set; } = true;
        public bool PassThru { get; set; } = true;

        // --- Equipment & Slots ---
        public bool IsEquipable { get; set; } = false;
        public string EquipSlot { get; set; } = "MainHand"; // MainHand, OffHand, TwoHand, Head, Chest, Legs, Feet, Hands, Neck, Ring, Back

        // --- Combat / Weapon Stats ---
        public string AttackType { get; set; } = "Melee"; // Melee, Ranged, Cast
        public int Hands { get; set; } = 1; // 1 or 2
        public int MinDamage { get; set; } = 1;
        public int MaxDamage { get; set; } = 4;
        public int Range { get; set; } = 1; // 1 for melee, 4+ for ranged/cast
        public string AmmoType { get; set; } = string.Empty;

        // --- Affects & Energy Modifiers (Genre-Agnostic) ---
        public bool HasEffect { get; set; } = false;
        public bool IsMagic { get => HasEffect; set => HasEffect = value; } // Backward-compatible alias
        public string EffectType { get; set; } = "None"; // None, Physical, Fire, Frost, Lightning, Toxic, Plasma, Laser, Arcane, EMP, Sonic, Void
        public string MagicElement { get => EffectType; set => EffectType = value; } // Backward-compatible alias
        public string ProcEffect { get; set; } = string.Empty; // e.g. "Plasma Burn", "Freeze", "Fireball", "Shock", "Armor Melt"
        public string SpellEffect { get => ProcEffect; set => ProcEffect = value; } // Backward-compatible alias
        public int ResourceCost { get; set; } = 0; // Mana, Energy, Heat, Battery, or Stamina
        public int ManaCost { get => ResourceCost; set => ResourceCost = value; } // Backward-compatible alias
        public int EffectPower { get; set; } = 0;
        public int MagicPower { get => EffectPower; set => EffectPower = value; } // Backward-compatible alias
        public List<ItemEffect> Effects { get; } = new List<ItemEffect>();

        // --- Armor & Defense Stats ---
        public int ArmorRating { get; set; } = 0;
        public int EnergyResistance { get; set; } = 0;
        public int MagicResistance { get => EnergyResistance; set => EnergyResistance = value; } // Backward-compatible alias
        public string ResistType { get; set; } = "None";
        public string ResistElement { get => ResistType; set => ResistType = value; } // Backward-compatible alias

        // --- Consumable Stats ---
        public string ConsumableType { get; set; } = "None"; // HealHP, RestoreMP, CurePoison, Buff, Teleport, Food
        public int EffectAmount { get; set; } = 0;
        public float Duration { get; set; } = 0.0f;

        // --- Containers & Security ---
        public bool IsContainer { get; set; } = false;
        public int Capacity { get; set; } = 10;
        public string DefaultTrapped { get; set; } = null;
        public string DefaultLocked { get; set; } = null;
        public bool DefaultCursed { get; set; } = false;
        public string DefaultCurseType { get; set; } = null;

        // --- Terrain & Hazards ---
        public bool IsDifficultTerrain { get; set; } = false;
        public float SpeedPenalty { get; set; } = 0.0f; // e.g. 0.4 for 40% slower
        public int HazardDamage { get; set; } = 0;      // e.g. spikes or acid puddle

        // --- Sprite Rendering Kind & Tint ---
        public string SpriteKind { get; set; } = "rock";
        public Color Color { get; set; } = Color.FromArgb(160, 160, 160);

        // --- Dynamic Custom Properties (for user-defined class templates) ---
        public Dictionary<string, string> CustomProperties { get; } =
            new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        public ItemDefinition Clone()
        {
            var clone = (ItemDefinition)this.MemberwiseClone();
            foreach (var kvp in this.CustomProperties)
                clone.CustomProperties[kvp.Key] = kvp.Value;
            clone.Effects.Clear();
            foreach (var eff in this.Effects)
                clone.Effects.Add(eff.Clone());
            return clone;
        }
    }

    /// <summary>
    /// Definition of a custom field inside a user-defined Class Template.
    /// </summary>
    public sealed class TemplateField
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = "string"; // "int", "float", "string", "bool", "enum"
        public string DefaultValue { get; set; } = string.Empty;
        public float MinValue { get; set; } = 0;
        public float MaxValue { get; set; } = 100;
        public List<string> Options { get; } = new List<string>();
        public string Tooltip { get; set; } = string.Empty;
    }

    /// <summary>
    /// User-defined or built-in Class Template defining the blueprint schema for a group of items.
    /// </summary>
    public sealed class ItemClassTemplate
    {
        public string ClassName { get; set; } = string.Empty;
        public string InheritsFrom { get; set; } = string.Empty;
        public string Category { get; set; } = "Weapon";
        public string Description { get; set; } = string.Empty;
        public string DefaultSprite { get; set; } = "weapon";
        public List<TemplateField> Fields { get; } = new List<TemplateField>();
    }

    /// <summary>
    /// A concrete item instance placed in a tile quadrant or stored in an inventory/container.
    /// Tracks runtime state: count, traps, disarmed status, locked status, cursed status.
    /// </summary>
    public sealed class ItemInstance
    {
        public ItemDefinition Definition { get; set; }
        public int Count { get; set; } = 1;

        // Dynamic interaction flags
        public bool IsTrapped { get; set; }
        public string TrapType { get; set; }
        public bool IsTrapDetected { get; set; }

        public bool IsLocked { get; set; }
        public string RequiredKey { get; set; }

        public bool IsCursed { get; set; }
        public string CurseType { get; set; }

        public bool IsContainer { get; set; }
        public List<ItemInstance> ContainerContents { get; } = new List<ItemInstance>();

        public ItemInstance(ItemDefinition def, int count = 1)
        {
            Definition = def ?? throw new ArgumentNullException(nameof(def));
            Count = Math.Max(1, count);

            IsContainer = def.IsContainer;

            if (!string.IsNullOrEmpty(def.DefaultTrapped))
            {
                IsTrapped = true;
                TrapType = def.DefaultTrapped;
            }

            if (!string.IsNullOrEmpty(def.DefaultLocked))
            {
                IsLocked = true;
                RequiredKey = def.DefaultLocked;
            }

            if (def.DefaultCursed)
            {
                IsCursed = true;
                CurseType = def.DefaultCurseType;
            }
        }
    }

    /// <summary>
    /// Registry for all item definitions loaded from items.json and class templates.
    /// Handles template inheritance ("inherit": "base_id").
    /// </summary>
    public sealed class ItemRegistry
    {
        private readonly Dictionary<string, ItemDefinition> _defs =
            new Dictionary<string, ItemDefinition>(StringComparer.OrdinalIgnoreCase);

        private readonly Dictionary<string, ItemClassTemplate> _classTemplates =
            new Dictionary<string, ItemClassTemplate>(StringComparer.OrdinalIgnoreCase);

        public ItemRegistry()
        {
            RegisterBuiltinArchetypes();
        }

        public void Register(ItemDefinition def)
        {
            if (def == null || string.IsNullOrEmpty(def.Id)) return;
            _defs[def.Id] = def;
        }

        public ItemDefinition Get(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            _defs.TryGetValue(id, out var def);
            return def;
        }

        public IEnumerable<ItemDefinition> GetAll() => _defs.Values;

        public void RegisterClassTemplate(ItemClassTemplate template)
        {
            if (template == null || string.IsNullOrEmpty(template.ClassName)) return;
            _classTemplates[template.ClassName] = template;
        }

        public ItemClassTemplate GetClassTemplate(string className)
        {
            if (string.IsNullOrEmpty(className)) return null;
            _classTemplates.TryGetValue(className, out var tmpl);
            return tmpl;
        }

        public IEnumerable<ItemClassTemplate> GetClassTemplates() => _classTemplates.Values;

        /// <summary>
        /// Registers baseline archetypes for standard RPG items.
        /// </summary>
        public void RegisterBuiltinArchetypes()
        {
            // --- WEAPONS ---
            Register(new ItemDefinition
            {
                Id = "generic_weapon_sword",
                Name = "Generic Sword",
                Category = "Weapon",
                SubCategory = "Sword",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                IsEquipable = true,
                EquipSlot = "MainHand",
                AttackType = "Melee",
                Hands = 1,
                MinDamage = 3,
                MaxDamage = 7,
                Range = 1,
                Weight = 3.0f,
                Value = 25,
                SpriteKind = "weapon",
                Color = Color.FromArgb(192, 192, 192)
            });

            Register(new ItemDefinition
            {
                Id = "generic_weapon_dagger",
                Name = "Generic Dagger",
                Category = "Weapon",
                SubCategory = "Dagger",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                IsEquipable = true,
                EquipSlot = "MainHand",
                AttackType = "Melee",
                Hands = 1,
                MinDamage = 2,
                MaxDamage = 5,
                Range = 1,
                Weight = 1.0f,
                Value = 15,
                SpriteKind = "weapon",
                Color = Color.FromArgb(176, 196, 222)
            });

            Register(new ItemDefinition
            {
                Id = "generic_weapon_greatsword",
                Name = "Generic Greatsword",
                Category = "Weapon",
                SubCategory = "Sword",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                IsEquipable = true,
                EquipSlot = "TwoHand",
                AttackType = "Melee",
                Hands = 2,
                MinDamage = 6,
                MaxDamage = 14,
                Range = 1,
                Weight = 6.0f,
                Value = 60,
                SpriteKind = "weapon",
                Color = Color.FromArgb(169, 169, 169)
            });

            Register(new ItemDefinition
            {
                Id = "generic_weapon_bow",
                Name = "Generic Bow",
                Category = "Weapon",
                SubCategory = "Bow",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                IsEquipable = true,
                EquipSlot = "TwoHand",
                AttackType = "Ranged",
                Hands = 2,
                MinDamage = 3,
                MaxDamage = 8,
                Range = 6,
                AmmoType = "arrow",
                Weight = 2.0f,
                Value = 35,
                SpriteKind = "weapon",
                Color = Color.FromArgb(139, 90, 43)
            });

            Register(new ItemDefinition
            {
                Id = "generic_weapon_staff",
                Name = "Generic Magic Staff",
                Category = "Weapon",
                SubCategory = "Staff",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                IsEquipable = true,
                EquipSlot = "TwoHand",
                AttackType = "Cast",
                Hands = 2,
                MinDamage = 4,
                MaxDamage = 10,
                Range = 5,
                HasEffect = true,
                EffectType = "Arcane",
                ProcEffect = "Arcane Bolt",
                ResourceCost = 5,
                Weight = 3.5f,
                Value = 80,
                SpriteKind = "weapon",
                Color = Color.FromArgb(147, 112, 219)
            });

            Register(new ItemDefinition
            {
                Id = "generic_weapon_wand",
                Name = "Generic Magic Wand",
                Category = "Weapon",
                SubCategory = "Wand",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                IsEquipable = true,
                EquipSlot = "MainHand",
                AttackType = "Cast",
                Hands = 1,
                MinDamage = 3,
                MaxDamage = 7,
                Range = 4,
                HasEffect = true,
                EffectType = "Fire",
                ProcEffect = "Fire Spark",
                ResourceCost = 3,
                Weight = 0.8f,
                Value = 65,
                SpriteKind = "weapon",
                Color = Color.FromArgb(255, 215, 0)
            });

            // --- ARMOR ---
            Register(new ItemDefinition
            {
                Id = "generic_armor_shield",
                Name = "Generic Shield",
                Category = "Armor",
                SubCategory = "Shield",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                IsEquipable = true,
                EquipSlot = "OffHand",
                ArmorRating = 4,
                Weight = 4.0f,
                Value = 30,
                SpriteKind = "shield",
                Color = Color.FromArgb(112, 128, 144)
            });

            Register(new ItemDefinition
            {
                Id = "generic_armor_helmet",
                Name = "Generic Helmet",
                Category = "Armor",
                SubCategory = "Helmet",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                IsEquipable = true,
                EquipSlot = "Head",
                ArmorRating = 2,
                Weight = 2.5f,
                Value = 20,
                SpriteKind = "armor",
                Color = Color.FromArgb(119, 136, 153)
            });

            Register(new ItemDefinition
            {
                Id = "generic_armor_chest",
                Name = "Generic Chestplate",
                Category = "Armor",
                SubCategory = "Chest",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                IsEquipable = true,
                EquipSlot = "Chest",
                ArmorRating = 6,
                Weight = 8.0f,
                Value = 50,
                SpriteKind = "armor",
                Color = Color.FromArgb(105, 105, 105)
            });

            Register(new ItemDefinition
            {
                Id = "generic_armor_boots",
                Name = "Generic Boots",
                Category = "Armor",
                SubCategory = "Boots",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                IsEquipable = true,
                EquipSlot = "Feet",
                ArmorRating = 2,
                Weight = 2.0f,
                Value = 15,
                SpriteKind = "armor",
                Color = Color.FromArgb(139, 69, 19)
            });

            // --- ACCESSORY ---
            Register(new ItemDefinition
            {
                Id = "generic_accessory_ring",
                Name = "Generic Ring",
                Category = "Accessory",
                SubCategory = "Ring",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                IsEquipable = true,
                EquipSlot = "Ring",
                Weight = 0.1f,
                Value = 50,
                SpriteKind = "key",
                Color = Color.FromArgb(255, 215, 0)
            });

            // --- CONSUMABLES ---
            Register(new ItemDefinition
            {
                Id = "generic_consumable_potion_hp",
                Name = "Health Potion",
                Category = "Consumable",
                SubCategory = "Potion",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                ConsumableType = "HealHP",
                EffectAmount = 50,
                Weight = 0.5f,
                Value = 20,
                SpriteKind = "potion",
                Color = Color.FromArgb(224, 48, 48)
            });

            Register(new ItemDefinition
            {
                Id = "generic_consumable_potion_mp",
                Name = "Mana Potion",
                Category = "Consumable",
                SubCategory = "Potion",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                ConsumableType = "RestoreMP",
                EffectAmount = 40,
                Weight = 0.5f,
                Value = 25,
                SpriteKind = "potion",
                Color = Color.FromArgb(48, 96, 224)
            });

            Register(new ItemDefinition
            {
                Id = "generic_consumable_scroll",
                Name = "Magic Scroll",
                Category = "Consumable",
                SubCategory = "Scroll",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                ConsumableType = "CastSpell",
                SpellEffect = "Fireball",
                Weight = 0.2f,
                Value = 35,
                SpriteKind = "scroll",
                Color = Color.FromArgb(245, 222, 179)
            });

            // --- CONTAINERS ---
            Register(new ItemDefinition
            {
                Id = "generic_container_chest",
                Name = "Wooden Chest",
                Category = "Container",
                SubCategory = "Chest",
                IsTemplate = true,
                IsContainer = true,
                PassThru = false,
                Collectable = false,
                Capacity = 12,
                Weight = 25.0f,
                Value = 15,
                SpriteKind = "chest",
                Color = Color.FromArgb(139, 90, 43)
            });

            // --- PROPS & HAZARDS ---
            Register(new ItemDefinition
            {
                Id = "generic_prop_rock",
                Name = "Small Rock",
                Category = "Prop",
                SubCategory = "Rock",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                Weight = 1.0f,
                Value = 0,
                SpriteKind = "rock",
                Color = Color.FromArgb(144, 144, 144)
            });

            Register(new ItemDefinition
            {
                Id = "generic_prop_boulder",
                Name = "Heavy Boulder",
                Category = "Prop",
                SubCategory = "Boulder",
                IsTemplate = true,
                Collectable = false,
                PassThru = false,
                Weight = 150.0f,
                Value = 0,
                SpriteKind = "rock",
                Color = Color.FromArgb(80, 80, 80)
            });

            Register(new ItemDefinition
            {
                Id = "generic_prop_rubble",
                Name = "Loose Rubble",
                Category = "Prop",
                SubCategory = "Rubble",
                IsTemplate = true,
                Collectable = false,
                PassThru = true,
                IsDifficultTerrain = true,
                SpeedPenalty = 0.4f,
                Weight = 10.0f,
                Value = 0,
                SpriteKind = "rock",
                Color = Color.FromArgb(120, 101, 80)
            });

            Register(new ItemDefinition
            {
                Id = "generic_prop_spikes",
                Name = "Floor Spikes",
                Category = "Prop",
                SubCategory = "Spikes",
                IsTemplate = true,
                Collectable = false,
                PassThru = true,
                IsDifficultTerrain = true,
                HazardDamage = 15,
                Weight = 0.0f,
                Value = 0,
                SpriteKind = "rock",
                Color = Color.FromArgb(139, 0, 0)
            });

            Register(new ItemDefinition
            {
                Id = "generic_prop_key",
                Name = "Iron Key",
                Category = "Prop",
                SubCategory = "Key",
                IsTemplate = true,
                Collectable = true,
                PassThru = true,
                Weight = 0.2f,
                Value = 5,
                SpriteKind = "key",
                Color = Color.FromArgb(192, 192, 192)
            });
        }

        /// <summary>
        /// Resolves inheritance for all registered items across multiple passes.
        /// Copies base archetype properties to child definitions where not explicitly overwritten.
        /// </summary>
        public void ResolveInheritance()
        {
            // Up to 4 passes to handle chained inheritance (e.g. child -> sub_archetype -> root_archetype)
            for (int pass = 0; pass < 4; pass++)
            {
                bool anyChanged = false;
                foreach (var kvp in _defs)
                {
                    var def = kvp.Value;
                    if (string.IsNullOrEmpty(def.Inherit)) continue;

                    if (_defs.TryGetValue(def.Inherit, out var parent))
                    {
                        if (string.IsNullOrEmpty(def.Name) && !string.IsNullOrEmpty(parent.Name))
                        {
                            def.Name = parent.Name;
                            anyChanged = true;
                        }
                        if (string.IsNullOrEmpty(def.Description) && !string.IsNullOrEmpty(parent.Description))
                            def.Description = parent.Description;

                        if (def.Category == "Prop" && parent.Category != "Prop")
                            def.Category = parent.Category;
                        if (string.IsNullOrEmpty(def.SubCategory) && !string.IsNullOrEmpty(parent.SubCategory))
                            def.SubCategory = parent.SubCategory;

                        if (def.SpriteKind == "rock" && parent.SpriteKind != "rock")
                            def.SpriteKind = parent.SpriteKind;
                        if (def.Color == Color.FromArgb(160, 160, 160) && parent.Color != Color.FromArgb(160, 160, 160))
                            def.Color = parent.Color;

                        if (!def.IsContainer && parent.IsContainer) def.IsContainer = true;
                        if (def.Collectable && !parent.Collectable) def.Collectable = false;
                        if (def.PassThru && !parent.PassThru) def.PassThru = false;
                        if (!def.IsEquipable && parent.IsEquipable) def.IsEquipable = true;
                        if (def.EquipSlot == "MainHand" && parent.EquipSlot != "MainHand")
                            def.EquipSlot = parent.EquipSlot;

                        // Combat
                        if (def.AttackType == "Melee" && parent.AttackType != "Melee")
                            def.AttackType = parent.AttackType;
                        if (def.Hands == 1 && parent.Hands != 1)
                            def.Hands = parent.Hands;
                        if (def.MinDamage == 1 && parent.MinDamage != 1)
                            def.MinDamage = parent.MinDamage;
                        if (def.MaxDamage == 4 && parent.MaxDamage != 4)
                            def.MaxDamage = parent.MaxDamage;
                        if (def.Range == 1 && parent.Range != 1)
                            def.Range = parent.Range;
                        if (string.IsNullOrEmpty(def.AmmoType) && !string.IsNullOrEmpty(parent.AmmoType))
                            def.AmmoType = parent.AmmoType;

                        // Magic
                        if (!def.IsMagic && parent.IsMagic) def.IsMagic = true;
                        if (def.MagicElement == "None" && parent.MagicElement != "None")
                            def.MagicElement = parent.MagicElement;
                        if (string.IsNullOrEmpty(def.SpellEffect) && !string.IsNullOrEmpty(parent.SpellEffect))
                            def.SpellEffect = parent.SpellEffect;
                        if (def.ManaCost == 0 && parent.ManaCost != 0)
                            def.ManaCost = parent.ManaCost;
                        if (def.MagicPower == 0 && parent.MagicPower != 0)
                            def.MagicPower = parent.MagicPower;

                        // Armor
                        if (def.ArmorRating == 0 && parent.ArmorRating != 0)
                            def.ArmorRating = parent.ArmorRating;
                        if (def.MagicResistance == 0 && parent.MagicResistance != 0)
                            def.MagicResistance = parent.MagicResistance;
                        if (def.ResistElement == "None" && parent.ResistElement != "None")
                            def.ResistElement = parent.ResistElement;

                        // Consumable
                        if (def.ConsumableType == "None" && parent.ConsumableType != "None")
                            def.ConsumableType = parent.ConsumableType;
                        if (def.EffectAmount == 0 && parent.EffectAmount != 0)
                            def.EffectAmount = parent.EffectAmount;

                        // Terrain & Hazards
                        if (!def.IsDifficultTerrain && parent.IsDifficultTerrain)
                        {
                            def.IsDifficultTerrain = true;
                            def.SpeedPenalty = parent.SpeedPenalty;
                            def.HazardDamage = parent.HazardDamage;
                        }

                        // Defaults
                        if (string.IsNullOrEmpty(def.DefaultTrapped) && !string.IsNullOrEmpty(parent.DefaultTrapped))
                            def.DefaultTrapped = parent.DefaultTrapped;
                        if (string.IsNullOrEmpty(def.DefaultLocked) && !string.IsNullOrEmpty(parent.DefaultLocked))
                            def.DefaultLocked = parent.DefaultLocked;
                        if (!def.DefaultCursed && parent.DefaultCursed)
                        {
                            def.DefaultCursed = true;
                            def.DefaultCurseType = parent.DefaultCurseType;
                        }

                        // Copy custom properties
                        foreach (var kvpParent in parent.CustomProperties)
                        {
                            if (!def.CustomProperties.ContainsKey(kvpParent.Key))
                                def.CustomProperties[kvpParent.Key] = kvpParent.Value;
                        }
                    }
                }
                if (!anyChanged) break;
            }
        }

        /// <summary>
        /// Creates a new instance of an item, applying any custom ad-hoc overrides.
        /// </summary>
        public ItemInstance CreateInstance(string id, int count = 1, Dictionary<string, string> modifiers = null)
        {
            var def = Get(id);
            if (def == null) return null;

            var inst = new ItemInstance(def, count);

            if (modifiers != null)
            {
                if (modifiers.TryGetValue("trapped", out var trapped))
                {
                    inst.IsTrapped = !string.Equals(trapped, "false", StringComparison.OrdinalIgnoreCase);
                    if (inst.IsTrapped && !string.Equals(trapped, "true", StringComparison.OrdinalIgnoreCase))
                        inst.TrapType = trapped;
                }

                if (modifiers.TryGetValue("locked", out var locked))
                {
                    inst.IsLocked = !string.Equals(locked, "false", StringComparison.OrdinalIgnoreCase);
                    if (inst.IsLocked && !string.Equals(locked, "true", StringComparison.OrdinalIgnoreCase))
                        inst.RequiredKey = locked;
                }

                if (modifiers.TryGetValue("cursed", out var cursed))
                {
                    inst.IsCursed = !string.Equals(cursed, "false", StringComparison.OrdinalIgnoreCase);
                    if (modifiers.TryGetValue("curseEffect", out var ce))
                        inst.CurseType = ce;
                }
            }

            return inst;
        }
    }
}
