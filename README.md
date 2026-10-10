# TileQuest — Forest Village RPG

TileQuest is a 2D, grid-based exploration game and Data Structures & Algorithms (DSA) course project built with C# and MonoGame. The current playable prototype includes a village, a forest, a dungeon, map travel, and basic melee combat. The DSA modules are demonstrated by startup checks, but most are not yet part of player-facing gameplay.

## Current Implementation

### World and navigation

- **Village:** A 48 × 36 collision grid (`CollisionMapLayouts.cs`) rendered over `Content/village_ground.png` and `Content/village_objects.png`. Buildings, props, and the pond block movement; the wizard is the forest travel point.
- **Forest:** A 36 × 29 collision grid from `forestlayout.cs`, rendered with `Content/forest_ground.png` and `Content/forest_canopy.png`. The canopy is drawn over the player. The dungeon entrance is the three walkable tiles at x=30–32, y=15. The forest exit to the village is at `ForestLayout.Gate` (18, 17).
- **Dungeon:** A 36 × 28 collision grid (`DungeonCollisionLayout` in `CollisionMapLayouts.cs`) displayed over `Content/dungeon.png`, with a dark screen-space vignette. Its exit returns to the forest entrance.
- **Movement:** Four-directional tile-locked movement with smooth slides, collision, animation, depth sorting, and camera follow.
- **Travel:** Stand beside the village wizard and press `E` to request forest travel. Stepping on a map exit or entrance requests travel. Confirm with `Y` or `Enter`; cancel with `N` or `Escape`. Prompts appear at the bottom center of the screen.
- **Combat:** Press `Space` or `J` to swing. Swings can damage and knock back stationary training enemies near the maps' spawn points. Enemies do not pursue or attack the player.
- **Debugging:** Press `F3` to toggle hitbox overlays.
- **Startup checks:** `TileGraph` checks map connectivity; `DsaDemo` runs self-checks for the standalone DSA modules.

### DSA modules

| Topic | Module | Current use |
| :--- | :--- | :--- |
| Queue | `WaveSpawner.cs`, `EnemySpawnInfo.cs` | Standalone FIFO spawn-sequence module; not wired to live enemy spawning |
| Stack | `ActionHistory.cs`, `PlayerAction.cs` | Standalone LIFO action-history module; no in-game undo |
| Linked list | `Inventory.cs`, `Item.cs` | Standalone inventory structure; no resource gathering or inventory HUD |
| Binary search | `BinarySearchUtil.cs`, `CraftingRecipe.cs` | Standalone sorted recipe lookup |
| Insertion sort | `InsertionSortUtil.cs` | Standalone descending sort utility |
| Graph / BFS | `TileGraph.cs` | Used for map walkability connectivity checks at startup |

## Not Yet Implemented

- Resource gathering, crafting, and player-facing inventory.
- Dungeon puzzles or other player-facing binary-search gameplay.
- Moving or attacking enemies, waves, player health, and boss encounters.
- Day/night progression, story endings, and high scores.

## Building and Running

**Prerequisite:** .NET 8 SDK.

From the project directory:

```sh
dotnet restore
dotnet build
dotnet run
```

The game directly loads PNG assets from `Content` with `TitleContainer.OpenStream`; no MonoGame content pipeline build is required. The project file copies content PNGs and the forest TMX file to the output directory.
