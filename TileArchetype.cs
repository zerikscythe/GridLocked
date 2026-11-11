using System;

namespace GridLocked
{
    /// <summary>
    /// Reusable blueprint for a map tile or fixture mapped to an ASCII key in the legend.
    /// Encapsulates traversal, visibility blocking, destructibility, pushability, and opening mechanisms.
    /// </summary>
    public sealed class TileArchetype
    {
        public char Key { get; set; } = ' ';
        public string Id { get; set; } = "floor";
        public string Name { get; set; } = "Floor";
        public TileType BaseType { get; set; } = TileType.Floor;

        // Traversal & Visibility
        public bool CanWalkThru { get; set; } = true;
        public bool CanWalkOver { get; set; } = true;
        public bool BlocksVision { get; set; } = false;

        // Destructibility (Breakable Walls, Crates, Pillars, Urns)
        public bool IsBreakable { get; set; } = false;
        public int BreakHP { get; set; } = 50;

        // Movement / Pushability (Sokoban Boulders, Sliding Statues)
        public bool IsMoveable { get; set; } = false;

        // Opening Mechanisms (Doors, Gates, Secret Passages, Grates)
        public bool CanBeOpened { get; set; } = false;
        public bool StartsOpen { get; set; } = false;

        // Hazards (Spikes, Lava, Acid, Void)
        public bool IsHazardous { get; set; } = false;
        public int HazardDamage { get; set; } = 0;

        // 3D Vector Sprite / Prop Mapping
        public string ObjectKind { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        public TileArchetype() { }

        public TileArchetype(char key, string id, string name, TileType baseType = TileType.Floor)
        {
            Key = key;
            Id = id;
            Name = name;
            BaseType = baseType;
            ApplyDefaultsForType(baseType, id);
        }

        public void ApplyDefaultsForType(TileType type, string id = "")
        {
            string lowerId = (id ?? string.Empty).ToLowerInvariant();

            if (type == TileType.Wall || lowerId.Contains("wall"))
            {
                BaseType = TileType.Wall;
                CanWalkThru = false;
                CanWalkOver = false;
                BlocksVision = true;
                IsBreakable = lowerId.Contains("cracked") || lowerId.Contains("break");
                BreakHP = IsBreakable ? 30 : 100;
            }
            else if (type == TileType.Door || lowerId.Contains("door") || lowerId.Contains("gate"))
            {
                BaseType = TileType.Door;
                CanWalkThru = false;
                CanWalkOver = true;
                BlocksVision = !lowerId.Contains("gate") && !lowerId.Contains("grate");
                CanBeOpened = true;
                StartsOpen = false;
            }
            else if (type == TileType.VoidOrPit || lowerId.Contains("void") || lowerId.Contains("pit"))
            {
                BaseType = TileType.VoidOrPit;
                CanWalkThru = false;
                CanWalkOver = false;
                BlocksVision = false;
                IsHazardous = true;
                HazardDamage = 999;
            }
            else if (type == TileType.StairsUp || type == TileType.StairsDown)
            {
                BaseType = type;
                CanWalkThru = true;
                CanWalkOver = true;
                BlocksVision = false;
            }
            else if (lowerId.Contains("pillar"))
            {
                BaseType = TileType.Floor;
                CanWalkThru = false;
                CanWalkOver = false;
                BlocksVision = false;
                IsBreakable = true;
                BreakHP = 100;
                ObjectKind = string.IsNullOrEmpty(ObjectKind) ? "stone_pillar" : ObjectKind;
            }
            else if (lowerId.Contains("boulder") || lowerId.Contains("block"))
            {
                BaseType = TileType.Floor;
                CanWalkThru = false;
                CanWalkOver = false;
                BlocksVision = false;
                IsMoveable = true;
                IsBreakable = true;
                BreakHP = 40;
                ObjectKind = string.IsNullOrEmpty(ObjectKind) ? "boulder" : ObjectKind;
            }
            else if (lowerId.Contains("lava") || lowerId.Contains("fire") || lowerId.Contains("acid"))
            {
                BaseType = TileType.Floor;
                CanWalkThru = true;
                CanWalkOver = true;
                BlocksVision = false;
                IsHazardous = true;
                HazardDamage = 25;
            }
            else // Standard Floor / Spawn
            {
                BaseType = type;
                CanWalkThru = true;
                CanWalkOver = true;
                BlocksVision = false;
            }
        }

        public TileArchetype Clone()
        {
            return new TileArchetype
            {
                Key = this.Key,
                Id = this.Id,
                Name = this.Name,
                BaseType = this.BaseType,
                CanWalkThru = this.CanWalkThru,
                CanWalkOver = this.CanWalkOver,
                BlocksVision = this.BlocksVision,
                IsBreakable = this.IsBreakable,
                BreakHP = this.BreakHP,
                IsMoveable = this.IsMoveable,
                CanBeOpened = this.CanBeOpened,
                StartsOpen = this.StartsOpen,
                IsHazardous = this.IsHazardous,
                HazardDamage = this.HazardDamage,
                ObjectKind = this.ObjectKind,
                Description = this.Description
            };
        }
    }
}
