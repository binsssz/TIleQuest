# TileQuest — Forest Village RPG

TileQuest is a 2D, grid-based exploration game and Data Structures & Algorithms (DSA) course project built with C# and MonoGame. The current playable prototype includes a village, a forest, a dungeon, map travel, resource gathering, basic melee combat, and a five-night village hearth-defense loop. Startup checks demonstrate the DSA modules; the queue and graph/BFS modules also support night-defense gameplay.

## Current Implementation

### World and navigation

- **Village:** A 48 × 36 collision grid (`CollisionMapLayouts.cs`) rendered with `Assets/village_ground.png` as the base, `Assets/village_objects_ground_decor.png` behind actors, and `Assets/village_objects_main.png` over actors. Buildings, props, and the pond block movement; the wizard is the forest travel point.
- **Forest:** A 36 × 29 collision grid from `forestlayout.cs`, rendered with `Assets/forest_ground.png` and `Assets/forest_canopy.png`. The canopy is drawn over the player. The dungeon entrance is the three walkable tiles at x=30–32, y=15. The forest exit to the village is at `ForestLayout.Gate` (18, 17).
- **Dungeon:** A 36 × 28 collision grid (`DungeonCollisionLayout` in `CollisionMapLayouts.cs`) displayed over `Assets/dungeon.png`, with a dark screen-space vignette. Its exit returns to the forest entrance.
- **Movement:** Four-directional tile-locked movement with smooth slides, collision, animation, depth sorting, and camera follow.
- **Travel:** Stand beside the village wizard and press `E` to request forest travel. Stepping on a map exit or entrance requests travel. Confirm with `Y` or `Enter`; cancel with `N` or `Escape`. Prompts appear at the bottom center of the screen.
- **Harvesting and inventory:** Press `Space` or `J` to swing at resource nodes and enemies. Harvested resources and enemy loot are stored in the linked-list inventory and displayed in the inventory HUD.
- **Combat:** Swings damage and knock back stationary training enemies near the maps' spawn points. Night goblins march toward and attack the village hearth; enemies do not attack the player.
- **Night defense:** Stand beside the village hearth and press `E`, then confirm with `Y` or `Enter` to start the next wave. A FIFO queue releases goblins over time; they use a BFS distance field to find the hearth. The hearth has 100 HP that persists between nights. Clear the wave to advance the day and regrow resource nodes. If the hearth is destroyed, that night is cleared for a retry. The HUD shows the day/night, remaining goblins, and hearth HP.
- **HUD:** The inventory and hearth status are functional. The player health and energy bars are visual-only; player health and energy mechanics are not implemented.
- **Debugging:** Press `F3` to toggle hitbox overlays.
- **Startup checks:** `TileGraph` checks map connectivity; `DsaDemo` runs self-checks for the standalone DSA modules.

### DSA modules

| Topic | Module | Current use |
| :--- | :--- | :--- |
| Queue | `WaveSpawner.cs`, `EnemySpawnInfo.cs` | Feeds the night wave: goblins dequeue in order and spawn at the village edge |
| Stack | `ActionHistory.cs`, `PlayerAction.cs` | Standalone LIFO action-history module; no in-game undo |
| Linked list | `Inventory.cs`, `Item.cs` | Stores gathered resources and loot shown in the inventory HUD |
| Binary search | `BinarySearchUtil.cs`, `CraftingRecipe.cs` | Standalone sorted recipe lookup |
| Insertion sort | `InsertionSortUtil.cs` | Standalone descending sort utility |
| Graph / BFS | `TileGraph.cs` | Startup connectivity checks, plus `HearthPathField`, the BFS distance field goblins follow to the hearth |

## Not Yet Implemented

- Crafting, trading, and in-game undo using the recipe search and action-history modules.
- Dungeon puzzles or other player-facing binary-search gameplay.
- Functional player health/energy, enemies that attack the player, and boss encounters.
- Story endings and high scores (nights 1-5 are playable, but there is no victory/defeat screen yet).

## Building and Running

**Prerequisite:** .NET 8 SDK.

From the project directory:

```sh
dotnet restore
dotnet build
dotnet run
```

The game directly loads PNG assets from the top-level `Assets` folder with `TitleContainer.OpenStream`; subfolders are preserved to organize the sprites and other files. No MonoGame content pipeline build is required. The project copies PNG assets and the forest TMX file from `Assets` to the build output.
