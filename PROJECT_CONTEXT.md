# Project Context

## Required Data Structures and Algorithms Topics

Choosing 5 integrated DSA topics for evaluation:

1. **Stack (`ActionHistory.cs`, `PlayerAction.cs`):** Crafting, trading, and upgrade undo history (LIFO).
2. **Queue (`WaveSpawner.cs`, `EnemySpawnInfo.cs`):** Turn-based combat processing and encounter queueing (FIFO).
3. **Linked List (`Inventory.cs`, `Item.cs`):** Dynamic player inventory node storage with $O(1)$ node removals.
4. **Binary Search (`BinarySearchUtil.cs`, `CraftingRecipe.cs`):** $O(\log n)$ recipe and shop item search sorted by resource cost.
5. **Insertion Sort (`InsertionSortUtil.cs`):** In-place descending sort for inventory value views and high-score ranking.
6. **Graph + BFS (`TileGraph.cs`):** Map walkability graph and BFS connectivity checks.

## Project at a Glance

- **Name:** TileQuest — Forest Village RPG
- **Purpose:** C# / MonoGame DSA course project.
- **Design Shift:** Scope simplified from tower defense to a focused **Resource Gathering, Crafting & Simple Turn-Based Combat RPG** loop to ensure clean, bug-free implementation of all rubric requirements.
- **Target Framework:** .NET 8 (`net8.0`).
- **Game Engine:** `MonoGame.Framework.DesktopGL` 3.8.x.

## Code Architecture Map

| File | Responsibility |
| :--- | :--- |
| `Program.cs` | Executes startup DSA verification (`DsaDemo`), launches `Game1`, catches startup errors |
| `Game1.cs` | Main game loop, texture loading, map travel transitions, input, depth-sorted rendering |
| `Player.cs` | Grid-locked tile movement, smooth pixel interpolation, facing direction, animation state |
| `TileMap.cs` | Tile grid storage (`Dictionary<Point, TileType>`), walkability checks, travel points |
| `VillageGenerator.cs` | Reads and validates `villagelayout.cs` grid, building footprints, and connectivity |
| `villagelayout.cs` | Text grid definition for village map |
| `ForestTmxMap.cs` | TMX map and tileset parser for `Content/forestmap.tmx` |
| `TileGraph.cs` | Graph representation of walkable tiles and BFS connectivity validation |
| `DsaDemo.cs` | Automated test runner verifying all 5 DSA modules at application startup |
| `WaveSpawner.cs`, `EnemySpawnInfo.cs` | Timed FIFO queue module for combat turn order / encounter processing |
| `ActionHistory.cs`, `PlayerAction.cs` | LIFO stack module for recording and undoing player actions |
| `Inventory.cs`, `Item.cs` | Linked-list inventory system with node management and sorted views |
| `BinarySearchUtil.cs`, `CraftingRecipe.cs` | Binary search algorithm and sorted recipe data structure |
| `InsertionSortUtil.cs` | Generic descending insertion sort implementation |
| `TileSprites.cs`, `pathcorners.cs`, `Draw*.cs` | Texture source rectangles and specialized depth-sorted visual renderers |

## Key Technical Constraints & Rules

- **Tile Size:** 32 pixels per tile.
- **World Map Dimensions:** Village is 36 × 28 tiles; Forest is defined by `forestmap.tmx`.
- **Texture Loading:** Textures are loaded directly via `TitleContainer.OpenStream` and `Texture2D.FromStream`. No MGCB pipeline is required.
- **Layout Validation:** `VillageGenerator` strictly enforces spawn (`S`), gate (`X`), and walkable path connectivity. Do not bypass these checks when modifying map layouts.