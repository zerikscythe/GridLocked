# GridLocked

[![License: MIT](https://img.shields.io/badge/License-MIT-blue.svg)](LICENSE)
[![.NET Framework](https://img.shields.io/badge/.NET%20Framework-4.8-purple.svg)](https://dotnet.microsoft.com/)
[![Platform](https://img.shields.io/badge/Platform-Windows%20x64-lightgrey.svg)]()

**GridLocked** is a retro-style first-person grid-based dungeon crawler engine and visual level editor built in C# and .NET Windows Forms. Drawing inspiration from classic dungeon crawlers (*Wizardry*, *Eye of the Beholder*, *Dungeon Master*), GridLocked combines pseudo-3D raycast rendering with a modular, data-driven architecture.

---

## Highlights & Features

### 🎮 The Game Engine (`GridLocked`)
- **First-Person Grid Raycaster**: Custom software raycasting renderer with perspective projection, distance fogging, and directional lighting.
- **Darkvision & Lighting System**: Multi-tiered lighting engine with darkvision illumination falloff and pitch-black dungeon corridors.
- **Dynamic Transition Mechanics**: Interactive doors, stairs up/down between floors, and pitfalls with locks, keys, and security traps.
- **4 Sub-Square Item Quadrants**: Every walkable floor tile features four independent sub-quadrant slots (NW, NE, SW, SE) for granular world-item placement and drops.
- **Live HUD & Compass**: Integrated real-time compass control and HUD status panels.

### 🛠️ The Visual Dungeon Studio (`GridLockedEditor`)
- **ASCII & Tile Palette Painter**: Visual map painter supporting keyboard shortcuts, pencil/fill tools, and instant level preview.
- **Tile Archetype & Legend System**: Configurable ASCII tile archetypes mapping character glyphs to physical terrain properties (walkable, opaque, breakable, hazardous).
- **Source-Typed Damage & Hazard Pipeline**:
  - User-managed **Damage Types** (`Normal`, `Physical`, `Fire`, `Ice`, `Poison`, `Arcane`, etc.).
  - **Damage Library Presets** (`Spike Impale`, `Lava Scald`, `Poison Dart`) with flat and percentage-based calculations (`% of Max HP`, `% of Current HP`).
- **Entity & Creature Management**:
  - Level scaling (1–20) with automated stat preview (HP, MP, Damage, Armor, Speed).
  - ★ Unique / Boss creatures with custom titles.
  - Multi-tiered loot drops: Archetype loot, Zone master loot tables, custom tables, and guaranteed drops.
- **Movement & Navigation Behavior**:
  - **Static**: Stationed guards.
  - **Patrol Route**: Custom waypoint recorder with loop, ping-pong, and once circuit modes plus idle delays.
  - **Wander (Area Leash)**: Paint custom meander territories for entities to roam using A* pathfinding, complete with leash distance and combat return logic.
- **Minimalist Progressive Disclosure**: Clean UI design where sub-controls dynamically appear only when their parent option is toggled on, keeping inspector cards compact and vertical space maximized.
- **Single-Click GLP Packaging**: Export entire dungeon projects into compressed `.glp` distribution files.

---

## Project Structure

```
GridLocked/
├── GridLocked.sln                     # Main solution file
├── GridLocked.csproj                  # Runtime engine project
│   ├── Form1.cs                       # 3D viewport, input loop, and HUD
│   ├── ScreenDrawing.cs               # Raycasting canvas & projection
│   ├── Lighting.cs                    # Darkvision & illumination pipeline
│   ├── Damage.cs                      # Source-typed Damage class & calculations
│   ├── TileArchetype.cs               # Archetype definition & legend mappings
│   ├── DungeonTile.cs                 # Tile state & sub-square item slots
│   ├── EntityMovementSystem.cs        # AI pathfinding, patrols, and wander leashes
│   ├── EntitySystem.cs                # Creature definitions & stat scaling
│   ├── LootTableSystem.cs             # Zone & creature loot table resolution
│   ├── TransitionObject.cs            # Doors, stairs, and pitfall transitions
│   └── ProjectLoader.cs               # Directory & GLP package loaders
├── GridLockedEditor/                  # Visual Dungeon Editor project
│   ├── EditorForm.cs                  # Main studio form & progressive disclosure inspector
│   ├── MapPainterControl.cs           # Level grid painter & route visualizer
│   └── ProjectExplorerControl.cs      # Project hierarchy & asset tree
├── Games/                             # Sample projects & level packages
│   └── Dungeon01/                     # Multi-floor dungeon project
└── docs/
    └── AGENT_GUIDE.md                 # Architecture, conventions & agent guide
```

---

## Getting Started

### Prerequisites
- **Operating System**: Windows 10 / 11 (64-bit)
- **Development Tools**: Visual Studio 2022 (Community or higher) with the **.NET desktop development** workload (targets .NET Framework 4.8).

### Building from Source

#### Via Visual Studio
1. Open `GridLocked.sln` in Visual Studio 2022.
2. Select `Debug` or `Release` configuration and `Any CPU` / `x64`.
3. Press `Ctrl + Shift + B` to build the entire solution.

#### Via Command Line (MSBuild)
```powershell
& "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "GridLocked.sln" /verbosity:minimal
```

### Running the Applications
- **Level Editor**: Run `GridLockedEditor\bin\Debug\GridLockedEditor.exe` to design dungeons, configure entities, and define damage presets.
- **Game Engine**: Run `bin\Debug\GridLocked.exe` to playtest loaded levels.

---

## Developer & Agent Guidelines

For conventions on terminology, storage boundaries, UI progressive disclosure rules, and compiler flags, see [docs/AGENT_GUIDE.md](docs/AGENT_GUIDE.md).

---

## License

This project is licensed under the MIT License — see the [LICENSE](LICENSE) file for details.
