# TileQuest — Forest Village RPG

TileQuest is a 2D, grid-based exploration game and Data Structures & Algorithms (DSA) course project built with C# and MonoGame. The current playable prototype includes a village, a forest, a dungeon, map travel, resource gathering, basic melee combat, a five-night village hearth-defense loop, and a village trader who crafts weapons with undo. Startup checks demonstrate the DSA modules; the queue, graph/BFS, binary-search, and stack modules also support gameplay.

## Current Implementation

### World and navigation

- **Village:** A 48 × 36 collision grid (`CollisionMapLayouts.cs`) rendered with `Assets/village_ground.png` as the base, `Assets/village_objects_ground_decor.png` behind actors, and `Assets/village_objects_main.png` over actors. Buildings, props, and the pond block movement; the wizard is the forest travel point.
- **Forest:** A 36 × 29 collision grid from `forestlayout.cs`, rendered with `Assets/forest_ground.png` and `Assets/forest_canopy.png`. The canopy is drawn over the player. The dungeon entrance is the three walkable tiles at x=30–32, y=15. The forest exit to the village is at `ForestLayout.Gate` (18, 17).
- **Dungeon:** A 36 × 28 collision grid (`DungeonCollisionLayout` in `CollisionMapLayouts.cs`) displayed over `Assets/dungeon.png`, with a dark screen-space vignette. Its exit returns to the forest entrance.
- **Movement:** Four-directional tile-locked movement with smooth slides, collision, animation, depth sorting, and camera follow.
- **Travel:** Stand beside the village wizard and press `E` to request forest travel. Stepping on a map exit or entrance requests travel. Confirm with `Y` or `Enter`; cancel with `N` or `Escape`. Prompts appear at the bottom center of the screen.
- **Harvesting and inventory:** Press `Space` or `J` to swing at resource nodes and enemies. Harvested resources, enemy loot, and crafted weapons are stored in the linked-list inventory and displayed in the inventory HUD. Press `I` to open the full inventory menu, or `E` when no nearby interaction is available. Use `W`/`S` or the arrow keys to select an item, `Enter` to equip a crafted weapon, `D` to discard one unit, and `Esc` to close. Food use is deferred until food items exist (they would refill energy).
- **Combat:** Swings damage and knock back enemies. Night goblins and skeletons march toward the village hearth and attack it, or the player if they end up beside them. Each attack is a visible wind-up, then one impact, then a recovery, once per second per enemy, and the damage only lands if the target is still adjacent at the impact moment, so you can dodge a swing by stepping away in time (hitting an enemy interrupts its swing); the forest and dungeon enemies (including the dungeon's 32 skeletons) stay put until you come within about seven tiles, then chase you and attack with the same dodgeable swing; if you fall there you wake up in the village at full health.
- **Night defense:** Stand beside the village hearth and press `E`, then confirm with `Y` or `Enter` to start the next wave. A FIFO queue releases a mix of goblins and armed skeletons over time (skeletons are a growing share of each night, with new variants joining on later nights); skeletons carry a bone spear and use a BFS distance field to find the hearth. The hearth has 100 HP that persists between nights. Clear the wave to advance the day and regrow resource nodes. If the hearth is destroyed, that night is cleared for a retry. The HUD shows the day/night, remaining foes, and hearth HP.
- **Trader and crafting:** In the day, stand beside the trader (the tavern woman in the forge area) and press `E` to open her menu. `W`/`S` select a recipe, `E` crafts it (spending materials from the inventory), and `Esc` closes. Weapon sprites appear in the recipe list and inventory; crafted weapons add swing damage and the strongest weapon is shown above the inventory.
- **Undo:** Press `U` (in the day, with or without the trader menu open) to undo the most recent craft: the gear is removed and its materials refunded. Undo history is cleared at dawn.
- **Player health and energy:** The lower-left bars are live (`PlayerStats.cs`). Energy is a hunger-style bar: it drains slowly over time, is not spent on swings, and fuels healing. While health is missing and energy remains, health regenerates (a few seconds after the last hit) and burns energy; at 0 energy there is no healing. Health and energy carry over at dawn, and losing the hearth does not refill them. If the player falls, they respawn at the village spawn with full health and energy. Night wave enemies standing next to the player hit the player instead of the hearth (their hearth damage ×2).
- **HUD:** The inventory, hearth status, and player health/energy bars are functional.
- **Debugging:** Press `F3` to toggle hitbox overlays.
- **Startup checks:** `TileGraph` checks map connectivity; `DsaDemo` runs self-checks for the standalone DSA modules.

### DSA modules

| Topic | Module | Current use |
| :--- | :--- | :--- |
| Queue | `WaveSpawner.cs`, `EnemySpawnInfo.cs` | Feeds the night wave: goblins and skeletons dequeue in order and spawn at the village edge |
| Stack | `ActionHistory.cs`, `PlayerAction.cs`, `Crafting.cs` | LIFO undo of crafting: `U` pops the last craft, removes the gear and refunds its materials |
| Linked list | `Inventory.cs`, `Item.cs` | Stores gathered resources, loot, and crafted gear; materials are spent from and refunded to it |
| Binary search | `BinarySearchUtil.cs`, `RecipeBook.cs`, `CraftingRecipe.cs` | The trader resolves a recipe's cost to the recipe with a binary search over the cost-sorted recipe book |
| Insertion sort | `InsertionSortUtil.cs` | Standalone descending sort utility |
| Graph / BFS | `TileGraph.cs` | Startup connectivity checks, plus `HearthPathField`, the BFS distance field the night enemies follow to the hearth |

## Not Yet Implemented

- Food items that refill energy (`PlayerStats.RestoreEnergy` is ready for them).
- Dungeon puzzles or other player-facing binary-search gameplay.
- Boss encounters.
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