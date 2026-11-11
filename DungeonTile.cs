using System;
using System.Collections.Generic;

namespace GridLocked
{
    public enum TileType
    {
        Floor = 0,
        Wall = 1,
        Spawn = 2,
        Door = 3,
        StairsUp = 4,
        StairsDown = 5,
        VoidOrPit = 6
    }

    /// <summary>
    /// Represents a discrete 1x1 dungeon tile in the grid.
    /// Supports a center occupant (Player, Monster, or Large Object) and 4 quadrant sub-squares
    /// that can hold items, movement-blocking obstacles, or difficult terrain.
    /// </summary>
    public sealed class DungeonTile
    {
        public int X { get; }
        public int Y { get; }

        public TileType Type { get; set; } = TileType.Floor;

        // Archetype / Legend Metadata
        public char? ArchetypeKey { get; set; } = null;
        public string ArchetypeId { get; set; } = null;
        public string ArchetypeName { get; set; } = null;

        // Physical Interaction Flags (Walkable, Visibility, Breakable, Moveable, Openable, Hazardous)
        public bool CanWalkThru { get; set; } = true;
        public bool CanWalkOver { get; set; } = true;
        public bool BlocksVision { get; set; } = false;

        public bool IsBreakable { get; set; } = false;
        public int BreakHP { get; set; } = 0;
        public int MaxBreakHP { get; set; } = 0;
        public bool IsBroken { get; set; } = false;

        public bool IsMoveable { get; set; } = false;

        public bool CanBeOpened { get; set; } = false;
        public bool IsOpen { get; set; } = false;

        public bool IsHazardous { get; set; } = false;
        public int HazardDamage { get; set; } = 0;

        /// <summary>
        /// Major occupant standing at the center of the tile (Player, Monster, or Large Object).
        /// Only one center occupant is permitted at any given time.
        /// </summary>
        public CenterEntity CenterOccupant { get; set; }

        /// <summary>
        /// Transition fixture (Door, Stairs, Pit, Portal) attached to this tile.
        /// Backed by CenterOccupant for seamless movement blocking and 3D vector rendering.
        /// </summary>
        public TransitionObject Transition
        {
            get => CenterOccupant as TransitionObject;
            set => CenterOccupant = value;
        }

        /// <summary>
        /// 4 sub-square slots:
        /// Index 0 = Q1 (Top-Left / NW: dx = -0.25, dy = -0.25)
        /// Index 1 = Q2 (Top-Right / NE: dx = +0.25, dy = -0.25)
        /// Index 2 = Q3 (Bottom-Left / SW: dx = -0.25, dy = +0.25)
        /// Index 3 = Q4 (Bottom-Right / SE: dx = +0.25, dy = +0.25)
        /// </summary>
        public ItemInstance[] Slots { get; } = new ItemInstance[4];

        public DungeonTile(int x, int y, TileType type = TileType.Floor)
        {
            X = x;
            Y = y;
            Type = type;
            if (type == TileType.Wall)
            {
                CanWalkThru = false;
                CanWalkOver = false;
                BlocksVision = true;
            }
            else if (type == TileType.VoidOrPit)
            {
                CanWalkThru = false;
                CanWalkOver = false;
                IsHazardous = true;
                HazardDamage = 999;
            }
        }

        /// <summary>
        /// Applies all interaction flags and properties from a TileArchetype blueprint.
        /// </summary>
        public void ApplyArchetype(TileArchetype arch)
        {
            if (arch == null) return;
            ArchetypeKey = arch.Key;
            ArchetypeId = arch.Id;
            ArchetypeName = arch.Name;
            Type = arch.BaseType;

            CanWalkThru = arch.CanWalkThru;
            CanWalkOver = arch.CanWalkOver;
            BlocksVision = arch.BlocksVision;

            IsBreakable = arch.IsBreakable;
            BreakHP = arch.BreakHP;
            MaxBreakHP = arch.BreakHP;
            IsBroken = false;

            IsMoveable = arch.IsMoveable;

            CanBeOpened = arch.CanBeOpened;
            IsOpen = arch.StartsOpen;

            IsHazardous = arch.IsHazardous;
            HazardDamage = arch.HazardDamage;
        }

        public bool Break(int damage)
        {
            if (!IsBreakable || IsBroken) return false;
            BreakHP -= Math.Max(1, damage);
            if (BreakHP <= 0)
            {
                IsBroken = true;
                CanWalkThru = true;
                CanWalkOver = true;
                BlocksVision = false;
                if (CenterOccupant != null && CenterOccupant.BlocksMovement)
                    CenterOccupant.BlocksMovement = false;
                return true;
            }
            return false;
        }

        public bool ToggleOpen()
        {
            if (!CanBeOpened) return false;
            IsOpen = !IsOpen;
            CanWalkThru = IsOpen;
            if (Transition != null)
            {
                if (IsOpen) Transition.Open(); else Transition.Close();
            }
            return IsOpen;
        }

        /// <summary>
        /// Checks if a player or monster can enter and stand in the center of this tile.
        /// Returns false if the tile is a wall/void, if its center is occupied by a blocking entity,
        /// or if any of its 4 sub-squares contains a movement-blocking obstacle (such as a boulder).
        /// </summary>
        public bool IsPassable(bool ignorePlayer = false)
        {
            if (Type == TileType.Wall)
                return false;
            if (!CanWalkThru || !CanWalkOver)
                return false;
            if (CanBeOpened && !IsOpen)
                return false;
            if (IsBreakable && !IsBroken && !CanWalkThru)
                return false;
            if (IsMoveable && !CanWalkThru)
                return false;

            if (Type == TileType.VoidOrPit && (Transition == null || !Transition.AutoTransitionOnStep))
                return false;

            if (CenterOccupant != null)
            {
                if (CenterOccupant is PlayerOccupant && ignorePlayer)
                {
                    // ignore player if checking for monster movement or pathfinding
                }
                else if (CenterOccupant.BlocksMovement)
                {
                    return false;
                }
            }

            // Check if any sub-square item blocks movement
            for (int i = 0; i < 4; i++)
            {
                var item = Slots[i];
                if (item != null && item.Definition != null && !item.Definition.PassThru)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Checks if the tile has difficult terrain (e.g. rubble, spikes, slime) in any sub-square.
        /// Returns speed multiplier (< 1.0) and hazard damage.
        /// </summary>
        public bool HasDifficultTerrain(out float speedMultiplier, out int hazardDamage)
        {
            speedMultiplier = 1.0f;
            hazardDamage = 0;
            bool found = false;

            for (int i = 0; i < 4; i++)
            {
                var item = Slots[i];
                if (item != null && item.Definition != null && item.Definition.IsDifficultTerrain)
                {
                    found = true;
                    if (item.Definition.SpeedPenalty > 0f)
                        speedMultiplier = Math.Min(speedMultiplier, Math.Max(0.1f, 1.0f - item.Definition.SpeedPenalty));
                    hazardDamage += item.Definition.HazardDamage;
                }
            }
            return found;
        }

        /// <summary>
        /// Returns the first empty quadrant slot index (0..3). Returns -1 if all 4 slots are occupied.
        /// </summary>
        public int GetFirstEmptySlot(int preferredQuadrant = -1)
        {
            if (preferredQuadrant >= 0 && preferredQuadrant < 4 && Slots[preferredQuadrant] == null)
                return preferredQuadrant;

            for (int i = 0; i < 4; i++)
            {
                if (Slots[i] == null)
                    return i;
            }
            return -1;
        }

        /// <summary>
        /// Attempts to drop an item into the tile. Drops into preferredQuadrant if empty,
        /// or the next available quadrant. Returns true if dropped, false if full.
        /// </summary>
        public bool TryDropItem(ItemInstance item, int preferredQuadrant, out int droppedSlot)
        {
            droppedSlot = GetFirstEmptySlot(preferredQuadrant);
            if (droppedSlot < 0 || item == null)
                return false;

            Slots[droppedSlot] = item;
            return true;
        }

        /// <summary>
        /// Takes (removes) an item from a specific quadrant slot (0..3).
        /// </summary>
        public ItemInstance TakeItem(int slotIndex)
        {
            if (slotIndex < 0 || slotIndex >= 4) return null;
            var it = Slots[slotIndex];
            Slots[slotIndex] = null;
            return it;
        }

        public static void GetQuadrantOffset(int slotIndex, out double ox, out double oy)
        {
            switch (slotIndex)
            {
                case 0: ox = -0.25; oy = -0.25; break; // Q1 (NW)
                case 1: ox = +0.25; oy = -0.25; break; // Q2 (NE)
                case 2: ox = -0.25; oy = +0.25; break; // Q3 (SW)
                default: ox = +0.25; oy = +0.25; break; // Q4 (SE)
            }
        }
    }
}
