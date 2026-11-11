using System;
using System.Collections.Generic;
using System.Drawing;

namespace GridLocked
{
    /// <summary>
    /// Base class for any entity or large prop occupying the center of a tile.
    /// </summary>
    public abstract class CenterEntity
    {
        public string Id { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public int TileX { get; set; }
        public int TileY { get; set; }
        public virtual bool BlocksMovement { get; set; } = true;
    }

    /// <summary>
    /// Represents the player standing at the center of a tile.
    /// </summary>
    public sealed class PlayerOccupant : CenterEntity
    {
        public override bool BlocksMovement => true;

        public PlayerOccupant()
        {
            Id = "player";
            Name = "Player";
        }
    }

    public enum EntityMovementMode
    {
        Static,
        PatrolRoute,
        Wander
    }

    public enum PatrolCircuitStyle
    {
        Loop,
        PingPong,
        Once
    }

    /// <summary>
    /// Represents an active NPC or Monster standing at the center of a tile.
    /// </summary>
    public sealed class MonsterInstance : CenterEntity
    {
        public int Level { get; set; } = 1;
        public int HP { get; set; } = 30;
        public int MaxHP { get; set; } = 30;
        public int MP { get; set; } = 0;
        public int MaxMP { get; set; } = 0;
        public int MinDamage { get; set; } = 3;
        public int MaxDamage { get; set; } = 6;
        public int Armor { get; set; } = 0;
        public int Speed { get; set; } = 10;
        public int XpReward { get; set; } = 15;
        public List<string> ActiveAbilities { get; } = new List<string>();
        public double FacingAngle { get; set; } = 0.0;
        public string SpriteKind { get; set; } = "skeleton";
        public Color TintColor { get; set; } = Color.White;
        public bool IsHostile { get; set; } = true;

        // Unique Boss & Custom Loot Configuration
        public bool IsUnique { get; set; } = false;
        public string UniqueTitle { get; set; } = string.Empty;
        public bool DropArchetypeLoot { get; set; } = true;
        public bool DropZoneLoot { get; set; } = true;
        public string CustomLootTable { get; set; } = string.Empty;
        public List<string> GuaranteedDrops { get; } = new List<string>();

        // Movement & Routing Configuration
        public EntityMovementMode MovementMode { get; set; } = EntityMovementMode.Static;
        public List<Point> PatrolWaypoints { get; } = new List<Point>();
        public PatrolCircuitStyle CircuitStyle { get; set; } = PatrolCircuitStyle.Loop;
        public double WaypointPauseSeconds { get; set; } = 2.0;
        public int WanderRadius { get; set; } = 3;
        public double WanderIntervalSeconds { get; set; } = 3.0;

        // Painted Wander Territory & Kiting / Disengage Leash
        public List<Point> WanderAreaTiles { get; } = new List<Point>();
        public int MaxChaseDistance { get; set; } = 5; // Tiles outside territory before disengaging
        public bool ReturnToPreCombatSquare { get; set; } = true;

        // Runtime Navigation & Wander State
        public List<Point> CurrentPath { get; } = new List<Point>();
        public Point? TargetDestination { get; set; }
        public int IdleCyclesRemaining { get; set; } = 0;
        public int BlockedCyclesCount { get; set; } = 0;
        public Point SpawnTile { get; set; }
        public Point PreCombatTile { get; set; }
        public bool IsInCombat { get; set; } = false;
        public bool IsReturningToLeash { get; set; } = false;
        public double WanderTimerSeconds { get; set; } = 0.0;
        public int CurrentPatrolIndex { get; set; } = 0;
        public bool PatrolReverse { get; set; } = false;
        public string LastNavStatus { get; set; } = "Ready";

        public MonsterInstance(string id, string name, int hp = 30)
        {
            Id = id;
            Name = name;
            HP = hp;
            MaxHP = hp;
            BlocksMovement = true;
        }
    }

    /// <summary>
    /// Represents a large prop or fixture (statue, altar, fountain, tree, large sarcophagus)
    /// occupying the center of a tile. Can be drawn with custom runtime vector primitives.
    /// </summary>
    public class LargeObjectInstance : CenterEntity
    {
        public string ObjectKind { get; set; } = "altar";
        public bool IsInteractable { get; set; } = true;
        public string InteractionPrompt { get; set; } = "Examine";
        public VectorSprite CustomVectorSprite { get; set; }

        public LargeObjectInstance(string id, string name, string objectKind, bool blocksMovement = true, VectorSprite vectorSprite = null)
        {
            Id = id;
            Name = name;
            ObjectKind = objectKind;
            BlocksMovement = blocksMovement;
            CustomVectorSprite = vectorSprite;
        }
    }
}
