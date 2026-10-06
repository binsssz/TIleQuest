# Project Context for AI Agents

## Project at a glance

- **Name:** TileQuest — Forest Village Defense.
- **Purpose:** C# / MonoGame DSA course project.
- **Current state:** the playable game is an overworld prototype with village
  and forest exploration. DSA modules are implemented and self-checked, but
  most are not integrated into player-facing gameplay.
- **Target framework:** .NET 8 (`net8.0`).
- **Game framework:** `MonoGame.Framework.DesktopGL` 3.8.x.
- **Project file:** `TileQuest.csproj`.

## Verify progress from the code

Do not treat the five-night defense arc or the old phase proposal as shipped
features. The current build includes:

- A 36 × 28 village generated from `villagelayout.cs`.
- The forest loaded from `Content/forestmap.tmx`.
- Grid-locked movement with smooth tile slides, collision, sprite animation,
  camera follow, map travel, and a hitbox debug overlay.
- Map validation/connectivity checks and `DsaDemo` self-checks at startup.
- Standalone queue, stack, linked-list inventory, binary-search, and insertion
  sort modules. These do not currently implement live enemy combat, crafting,
  a shop, a full game inventory, or a leaderboard.

Day/night progression, harvesting, defense placement, enemies/combat, boss
fights, story endings, and high scores are planned work unless subsequent code
changes add them. Check the README and implementation before describing any
feature as complete.

## Important code map

| File | Responsibility |
| --- | --- |
| `Program.cs` | Runs DSA self-check, creates the game, and reports startup errors |
| `Game1.cs` | MonoGame lifecycle, asset loading, map travel, input, drawing |
| `Player.cs` | Grid movement, smooth interpolation, facing, animation state |
| `TileMap.cs` | Map data, walkability, collision/travel points, graph checks |
| `VillageGenerator.cs` | Parses and validates the village grid |
| `villagelayout.cs` | Editable village map rows and legend |
| `ForestTmxMap.cs` | Parses TMX map/tilesets/layers and forest walkable/tree-anchor tiles |
| `Content/forestmap.tmx` | Authoritative forest map and its TMX layer data |
| `TileGraph.cs` | Walkable adjacency graph and breadth-first connectivity |
| `DsaDemo.cs` | Startup checks for the DSA modules |
| `WaveSpawner.cs`, `EnemySpawnInfo.cs` | Timed FIFO enemy-spawn module |
| `ActionHistory.cs`, `PlayerAction.cs` | LIFO undo-history module |
| `Inventory.cs`, `Item.cs` | Linked-list inventory module and value-sorted snapshot |
| `BinarySearchUtil.cs`, `CraftingRecipe.cs` | Binary-search utility and sorted recipe sample |
| `InsertionSortUtil.cs` | Generic descending insertion sort |
| `TileSprites.cs`, `pathcorners.cs`, `Draw*.cs` | Sprite source rectangles and world object rendering |

`ForestGenerator.cs` exists, but the live forest path in `TileMap` loads the TMX
map; do not assume the procedural generator is used at runtime.

## Technical constraints and conventions

- The game uses a 32-pixel tile size and currently creates 36 × 28 world maps
  in `Game1`.
- The forest map dimensions come from the TMX file. Its `foothill` and
  `foothill grass` layers define walkable cells; `foothill trees` supplies
  blocked tree-base anchors.
- Village map edits must preserve the expected dimensions and required spawn
  and gate positions. The generators validate layouts and report bad symbols,
  structure footprints, or unreachable paths; do not silently bypass these
  checks.
- PNG files under `Content` and `Content/forestmap.tmx` are copied to the build
  output by the project file. Textures are loaded directly with
  `TitleContainer.OpenStream` and `Texture2D.FromStream`; there is no
  `Content.mgcb` pipeline.
- Follow existing C# style: nullable reference types are enabled, implicit
  usings are disabled, and MonoGame types are used directly.
- Keep changes focused. Preserve current movement, collision, travel,
  rendering, and validation behavior unless the task explicitly changes it.
- When adding gameplay systems, distinguish module-level DSA code from code
  actually wired into `Game1` and the player experience.

## Build and smoke-check

From the project directory:

```sh
dotnet restore
dotnet build
dotnet run
```

`dotnet run` prints DSA and map validation checks before opening the window.
There is no separate test project in this directory; use the build and startup
checks as the baseline, then add focused tests when introducing logic that can
be tested independently.
