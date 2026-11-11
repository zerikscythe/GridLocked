# GridLocked — Agent & Developer Guide

This document defines core architectural conventions, UI principles, and system requirements for AI coding assistants and developers contributing to **GridLocked**.

---

## 1. Non-Negotiable Workspace Rules

- **Strict D: Drive Storage Only**: All project work, permanent files, temporary/scratch files, build outputs, and caches must stay on the `D:` drive. The `C:` drive has minimal free space and must not be used for any operations.
- **Temporary & Scratch Files**: When running scripts, tests, or generating temporary files, always use a local directory on `D:` (such as `D:\Documents\projects\GridLocked\.tmp` or `D:\tmp`). Do not use Windows default `%TEMP%` or `%TMP%` located on `C:`.
- **MSBuild Compiler**: Always build using the Visual Studio 2022 MSBuild executable:
  ```powershell
  & "C:\Program Files\Microsoft Visual Studio\2022\Community\MSBuild\Current\Bin\MSBuild.exe" "d:\Documents\projects\GridLocked\GridLocked.sln" /verbosity:minimal
  ```
  *(Do not use the legacy .NET 4.0 framework MSBuild at `C:\Windows\Microsoft.NET\Framework64\v4.0.30319\MSBuild.exe` as it lacks C# 6+ compiler support).*

---

## 2. Naming & Terminology Conventions

- **Creatures / Entities**: Always use the terms **"Entities"**, **"Creatures"**, or **"EntityDefinition"** in code, comments, documentation, and user-facing UI.
  - *Do NOT use "monsters"* or *"bestiary"* in new features or UI labels (legacy serialized identifiers such as `monsters.txt` are preserved for backward compatibility).
- **Damage Objects**: Use the source-typed `Damage` class rather than raw integer damage.

---

## 3. UI Design: Minimalist Progressive Disclosure

The editor interface follows a strict **minimalist progressive disclosure** model:

1. **No Blank/Disabled Sub-Controls**:
   - If a parent option or toggle is **OFF**, its child controls must not be displayed in a disabled/empty state. Instead, set `Visible = false` on their container or control elements.
2. **Dynamic Card Resizing & Reflow**:
   - Inspector cards (`_grpTileBase`, `_grpEntityCard`, `_grpMovementCard`, `_grpTransition`, `_grpSubSquareItems`) stack vertically inside `_inspectorPanel`.
   - When an option is toggled, recalculate the card's height based on visible child controls and call `ReflowInspectorCards()` so cards below glide up or down without leaving dead vertical space.
3. **Preset vs. Custom Modes**:
   - When a library preset is active, only show the preset selector dropdown and the live badge summary.
   - Only reveal custom field inputs (Type, Amount, Scale, Status Effect, `+ Preset`) when `(Custom Damage)` is explicitly selected.

---

## 4. Core Systems Overview

### A. Source-Typed Damage Pipeline (`Damage.cs`)
- **Damage Types**: User-defined list stored in `project.json` under `"damageTypes"`. Defaults include `Normal`, `Physical`, `Fire`, `Ice`, `Lightning`, `Poison`, `Arcane`, `Holy`, `Shadow`, `Mental`, `Laser`.
- **Damage Library**: Named presets stored in `project.json` under `"damageLibrary"` (e.g., `Spike Impale`, `Lava Scald`, `Poison Dart`).
- **Calculation**: Supports `Flat`, `PercentMaxHp`, and `PercentCurrentHp` with optional status effects, duration, and proc chance:
  ```csharp
  int finalDmg = damage.Calculate(currentHp: 80, maxHp: 100);
  ```

### B. Tile Archetypes & ASCII Map Painter (`TileArchetype.cs`)
- Tiles support archetype characters (`' '` = Floor, `'#'` = Wall, `'D'` = Door, etc.).
- Archetypes hold default properties (walkable, vision blocking, breakable, hazard effect, item loot table).
- Individual tiles in a level can override base archetype flags.

### C. Entity Movement & AI (`EntityMovementSystem.cs`)
Entities on the dungeon grid support 3 distinct movement behaviors:
1. **Static**: Stationed in place, does not move outside combat.
2. **Patrol Route**: Follows explicit user-drawn waypoints (`Loop`, `PingPong`, or `Once`) with configurable waypoint pause intervals.
3. **Wander (Area Leash)**: Constrained to user-painted territory cells. Selects random target tiles within the painted zone and pathfinds using A*. If engaged in combat, will pursue up to `MaxChaseDistance` before disengaging and returning to its pre-combat square.

### D. Sub-Square Item Slots
- Each walkable tile contains 4 sub-quadrants (Q1 Top-Left / NW, Q2 Top-Right / NE, Q3 Bottom-Left / SW, Q4 Bottom-Right / SE).
- Items can be placed directly in any quadrant or dropped by creatures upon defeat.

---

## 5. File & Project Packaging Formats

- **Directory-Based Projects**:
  - `project.json`: Project metadata, damage types, damage library presets.
  - `items.json`: Item definitions.
  - `dungeons/<dungeon_id>/dungeon.json`: Dungeon metadata and floor listing.
  - `dungeons/<dungeon_id>/<level_id>/map.txt`: ASCII level layout.
  - `dungeons/<dungeon_id>/<level_id>/transitions.json`: Door, stairs, and pit transitions.
  - `dungeons/<dungeon_id>/<level_id>/monsters.txt`: Spawned entity instances and movement routes.
- **GLP Package Format (`.glp`)**:
  - Compressed zip package encapsulating all project JSON and level text files for self-contained runtime distribution.
