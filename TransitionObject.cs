using System;
using System.Collections.Generic;

namespace GridLocked
{
    public enum TransitionKind
    {
        Door = 0,
        StairsUp = 1,
        StairsDown = 2,
        Pit = 3,
        Portal = 4
    }

    public enum TransitionStyle
    {
        Immediate = 0,
        Fade = 1,
        Slide = 2,
        Fall = 3
    }

    /// <summary>
    /// Interaction result when a player attempts to open, trigger, or step through a transition object.
    /// Encapsulates whether a trap was sprung, a lock blocked passage or was unlocked, door opened, or player moved.
    /// </summary>
    public sealed class TransitionInteractionResult
    {
        public bool Success { get; set; }
        public bool TrapTriggered { get; set; }
        public string TrapType { get; set; }
        public int TrapDamage { get; set; }
        public bool WasLocked { get; set; }
        public bool WasUnlocked { get; set; }
        public bool DidOpen { get; set; }
        public bool DidClose { get; set; }
        public bool TransitionTriggered { get; set; }
        public string TargetDungeon { get; set; } = string.Empty;
        public int TargetLevel { get; set; } = -1;
        public int TargetX { get; set; } = -1;
        public int TargetY { get; set; } = -1;
        public string TargetFacing { get; set; } = "Keep";
        public string Message { get; set; } = string.Empty;
    }

    /// <summary>
    /// Base class for all interactive transition fixtures (Doors, Stairs, Pits, Portals)
    /// that connect dungeon rooms, levels, or dungeons.
    /// Handles trap triggering, lock validation, state changes (opening/closing), and transition execution.
    /// </summary>
    public class TransitionObject : LargeObjectInstance
    {
        public TransitionKind Kind { get; set; } = TransitionKind.Door;

        // Destination target
        public string TargetDungeon { get; set; } = string.Empty; // empty = current dungeon
        public int TargetLevel { get; set; } = -1;                // -1 = current level
        public int TargetX { get; set; } = -1;                    // destination tile X (-1 = no teleport / in-room door)
        public int TargetY { get; set; } = -1;                    // destination tile Y (-1 = no teleport / in-room door)
        public string TargetFacing { get; set; } = "Keep";       // "Keep", "North", "East", "South", "West"

        // Behavior & Presentation
        public TransitionStyle Style { get; set; } = TransitionStyle.Immediate;
        public bool AutoTransitionOnStep { get; set; } = false;   // True for stairs/pits/portals when entered
        public string TransitionMessage { get; set; } = string.Empty;

        // State (Door / Gate)
        public bool IsOpen { get; set; } = false;
        public bool IsLocked { get; set; } = false;
        public string RequiredKey { get; set; } = string.Empty;
        public string LockedMessage { get; set; } = string.Empty;
        public string OpenMessage { get; set; } = string.Empty;

        // Trap mechanics
        public bool IsTrapped { get; set; } = false;
        public string TrapType { get; set; } = "PoisonDart";
        public int TrapDamage { get; set; } = 10;
        public bool IsTrapDisarmed { get; set; } = false;
        public bool TrapIsRecurring { get; set; } = false;         // false = 1-time dart/needle; true = recurring shock/rune
        public int TrapCharges { get; set; } = 1;                  // 1 = single use; -1 = infinite recurring; >1 = counter
        public double TrapJiggleChance { get; set; } = 0.33;       // 33% chance to trip trap when jiggling locked handle
        public double TrapOpenChance { get; set; } = 1.00;         // 100% chance to trip trap when opening door
        public bool TrapDiscovered { get; set; } = false;          // Whether player/party has spotted the trap
        public string TrapMessage { get; set; } = string.Empty;

        // Visual sprites for closed and open states
        public VectorSprite SpriteClosed { get; set; }
        public VectorSprite SpriteOpen { get; set; }

        private static readonly Random _rng = new Random();

        public override bool BlocksMovement
        {
            get => !IsOpen;
            set => base.BlocksMovement = value;
        }

        public TransitionObject(string id, string name, TransitionKind kind, int tileX, int tileY, VectorSprite closedSprite = null, VectorSprite openSprite = null)
            : base(id, name, kind.ToString().ToLowerInvariant(), true, closedSprite)
        {
            Kind = kind;
            TileX = tileX;
            TileY = tileY;
            SpriteClosed = closedSprite;
            SpriteOpen = openSprite;
            CustomVectorSprite = IsOpen ? (openSprite ?? closedSprite) : (closedSprite ?? openSprite);
        }

        public void Open()
        {
            IsOpen = true;
            BlocksMovement = false;
            if (SpriteOpen != null)
                CustomVectorSprite = SpriteOpen;
        }

        public void Close()
        {
            IsOpen = false;
            BlocksMovement = true;
            if (SpriteClosed != null)
                CustomVectorSprite = SpriteClosed;
        }

        public bool HasDestination()
        {
            return TargetLevel != -1 || (TargetX >= 0 && TargetY >= 0) || !string.IsNullOrEmpty(TargetDungeon);
        }

        /// <summary>
        /// Fires the armed trap if triggered by a roll.
        /// Handles 1-time expendable traps (Poison Dart, Needle) vs recurring traps (Shock, Arcane Rune).
        /// </summary>
        public bool TrySpringTrap(TransitionInteractionResult res, string actionDescription, double chance)
        {
            if (!IsTrapped || IsTrapDisarmed) return false;

            if (_rng.NextDouble() > chance)
            {
                return false; // Escaped triggering the trap this time!
            }

            res.TrapTriggered = true;
            res.TrapType = TrapType;
            res.TrapDamage = TrapDamage;

            // Handle charge depletion: 1-time vs recurring
            if (!TrapIsRecurring)
            {
                if (TrapCharges > 0)
                {
                    TrapCharges--;
                    if (TrapCharges <= 0)
                    {
                        IsTrapDisarmed = true; // Expended 1-time trap (e.g. poison dart fired)
                    }
                }
                else
                {
                    IsTrapDisarmed = true;
                }
            }

            string trapNotice = !string.IsNullOrEmpty(TrapMessage)
                ? TrapMessage
                : TrapIsRecurring
                    ? $"⚡ [SHOCK / WARD TRAP!] {actionDescription} trips an active {TrapType} trap! You take {TrapDamage} damage! The ward hums and remains active!"
                    : $"⚡ [TRAP SPRUNG!] {actionDescription} trips a hidden {TrapType} trap! You take {TrapDamage} damage! The single-use trap is now spent.";

            res.Message = (string.IsNullOrEmpty(res.Message) ? "" : res.Message + "\n") + trapNotice;
            return true;
        }

        /// <summary>
        /// Attempts to disarm a discovered trap before opening or unlocking.
        /// </summary>
        public bool TryDisarmTrap(out string message)
        {
            if (!IsTrapped)
            {
                message = "There is no trap on this fixture.";
                return false;
            }
            if (IsTrapDisarmed)
            {
                message = "The trap is already disarmed or expended.";
                return false;
            }

            IsTrapDisarmed = true;
            message = $"🛠 You carefully disarmed the {TrapType} trap mechanism!";
            return true;
        }

        /// <summary>
        /// Performs an interaction following the realistic dungeon crawl flow:
        /// 1. If LOCKED:
        ///    - If player has required key: Unlocks the lock (turning the key).
        ///      *CRITICAL*: Unlocking does NOT disarm the trap, and leaves the door closed so the player can choose next action.
        ///    - If player lacks key: Player tries handle -> Jiggles the lock!
        ///      Jiggling has a 33% chance to trip the trap.
        /// 2. If UNLOCKED and CLOSED (player pushes/swings door open):
        ///    - Attempting to open has a 100% chance to trip an armed trap!
        ///    - Door opens, sprite updates to open, movement unblocks.
        /// 3. If ALREADY OPEN:
        ///    - If it has a destination, triggers transition movement.
        /// </summary>
        public virtual TransitionInteractionResult TryInteract(IList<ItemInstance> playerInventory)
        {
            var res = new TransitionInteractionResult
            {
                TargetDungeon = TargetDungeon,
                TargetLevel = TargetLevel,
                TargetX = TargetX,
                TargetY = TargetY,
                TargetFacing = TargetFacing
            };

            // CASE 1: Fixture is LOCKED
            if (IsLocked)
            {
                res.WasLocked = true;
                bool hasKey = false;
                string keyDisplayName = RequiredKey;

                if (playerInventory != null && !string.IsNullOrEmpty(RequiredKey))
                {
                    for (int i = 0; i < playerInventory.Count; i++)
                    {
                        var inv = playerInventory[i];
                        if (inv?.Definition != null &&
                            (string.Equals(inv.Definition.Id, RequiredKey, StringComparison.OrdinalIgnoreCase) ||
                             (string.Equals(inv.Definition.SubCategory, "Key", StringComparison.OrdinalIgnoreCase) && string.Equals(inv.Definition.Name, RequiredKey, StringComparison.OrdinalIgnoreCase))))
                        {
                            hasKey = true;
                            keyDisplayName = inv.Definition.Name;
                            break;
                        }
                    }
                }

                if (hasKey)
                {
                    // Turning the key unlocks the bolt.
                    // IMPORTANT: Does NOT trigger the trap, and does NOT disarm the trap!
                    IsLocked = false;
                    res.WasUnlocked = true;
                    res.Success = true;
                    string unlockMsg = $"🗝 You insert the {keyDisplayName} and turn the mechanism with a clean *CLICK*.\n{Name} is unlocked, but remains closed.";
                    if (IsTrapped && !IsTrapDisarmed)
                    {
                        unlockMsg += "\n⚠ You notice a suspicious tension behind the latch...";
                    }
                    res.Message = (string.IsNullOrEmpty(res.Message) ? "" : res.Message + "\n") + unlockMsg;
                    return res;
                }
                else
                {
                    // Player has no key and tries the locked door -> jiggles the handle/lock
                    res.Success = false;

                    // Jiggling has a 33% chance to trip an armed trap!
                    bool sprung = TrySpringTrap(res, "Jiggling the locked handle", TrapJiggleChance);

                    string lockMsg = !string.IsNullOrEmpty(LockedMessage)
                        ? LockedMessage
                        : $"🔒 {Name} is locked tight. Requires key: '{RequiredKey}'.";

                    if (!sprung && IsTrapped && !IsTrapDisarmed)
                    {
                        lockMsg += " You rattled the handle, but nothing happened... this time.";
                    }

                    res.Message = (string.IsNullOrEmpty(res.Message) ? "" : res.Message + "\n") + lockMsg;
                    return res;
                }
            }

            // CASE 2: Fixture is UNLOCKED but CLOSED (Attempting to swing/push door open)
            if (!IsOpen)
            {
                // Opening an armed door has 100% chance to trip the trap!
                TrySpringTrap(res, "Pushing the door open", TrapOpenChance);

                // Swing door open
                Open();
                res.DidOpen = true;
                string openMsg = !string.IsNullOrEmpty(OpenMessage) ? OpenMessage : $"{Name} swings open.";
                res.Message = (string.IsNullOrEmpty(res.Message) ? "" : res.Message + "\n") + openMsg;

                // If door has immediate transition without separate step, or if auto-transition is set
                if (AutoTransitionOnStep && HasDestination())
                {
                    res.TransitionTriggered = true;
                    if (!string.IsNullOrEmpty(TransitionMessage))
                        res.Message = (string.IsNullOrEmpty(res.Message) ? "" : res.Message + "\n") + TransitionMessage;
                }

                res.Success = true;
                return res;
            }

            // CASE 3: Fixture is ALREADY OPEN (e.g. walking through, stairs, or portal)
            if (HasDestination())
            {
                res.TransitionTriggered = true;
                if (!string.IsNullOrEmpty(TransitionMessage))
                    res.Message = (string.IsNullOrEmpty(res.Message) ? "" : res.Message + "\n") + TransitionMessage;
            }
            else
            {
                // Can close the open door
                Close();
                res.DidClose = true;
                res.Message = (string.IsNullOrEmpty(res.Message) ? "" : res.Message + "\n") + $"{Name} is closed shut.";
            }

            res.Success = true;
            return res;
        }
    }

    public sealed class DoorTransition : TransitionObject
    {
        public DoorTransition(string id, int tileX, int tileY, VectorSprite closedSprite = null, VectorSprite openSprite = null)
            : base(id, "Door", TransitionKind.Door, tileX, tileY, closedSprite, openSprite)
        {
            IsOpen = false;
            BlocksMovement = true;
            AutoTransitionOnStep = false;
            InteractionPrompt = "Open Door";
        }
    }

    public sealed class StairsTransition : TransitionObject
    {
        public StairsTransition(string id, TransitionKind kind, int tileX, int tileY, int targetLevel = -1, VectorSprite sprite = null)
            : base(id, kind == TransitionKind.StairsUp ? "Stairs Up" : "Stairs Down", kind, tileX, tileY, sprite, sprite)
        {
            IsOpen = true;
            BlocksMovement = false;
            AutoTransitionOnStep = true;
            TargetLevel = targetLevel;
            InteractionPrompt = kind == TransitionKind.StairsUp ? "Climb Stairs Up" : "Descend Stairs Down";
        }
    }

    public sealed class PitTransition : TransitionObject
    {
        public PitTransition(string id, int tileX, int tileY, VectorSprite sprite = null)
            : base(id, "Pit", TransitionKind.Pit, tileX, tileY, sprite, sprite)
        {
            IsOpen = true;
            BlocksMovement = false;
            AutoTransitionOnStep = true;
            Style = TransitionStyle.Fall;
            TrapDamage = 10;
            InteractionPrompt = "Descend Pit";
        }
    }

    public sealed class PortalTransition : TransitionObject
    {
        public PortalTransition(string id, int tileX, int tileY, VectorSprite sprite = null)
            : base(id, "Dimensional Portal", TransitionKind.Portal, tileX, tileY, sprite, sprite)
        {
            IsOpen = true;
            BlocksMovement = false;
            AutoTransitionOnStep = true;
            InteractionPrompt = "Enter Portal";
        }
    }
}
