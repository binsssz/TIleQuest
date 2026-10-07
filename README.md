# TileQuest — Forest Village RPG

TileQuest is a 2D, grid-based exploration game and Data Structures & Algorithms (DSA) course project built with C# and MonoGame. Its current playable prototype has a hand-authored village and forest; planned gameplay centers on gathering resources, solving simple DSA-inspired puzzles, and exploring a dungeon.

## Planned Scope & Core Gameplay

The gameplay scope is intentionally small:
1. **Explore and gather:** Travel between the village, forest, and planned dungeon to collect a small set of resources.
2. **Manage resources:** Show collected items in a simple inventory; use the linked list to store them and insertion sort to display them in a useful order.
3. **Solve a dungeon puzzle:** Place a resource cache behind a simple puzzle that demonstrates a DSA topic. A binary-search puzzle is a candidate: use higher/lower clues to narrow down a hidden value. Start with one puzzle type and add more only if time permits.
4. **Keep the dungeon purposeful:** Use it as another gathering location, with the puzzle providing a small challenge and making DSA visible in play. The dungeon map and puzzles are planned, not implemented features.

The existing queue, stack, and recipe-search modules can support additional gameplay later, but combat, trading, a leaderboard, and multiple endings are not required for this reduced first-pass scope.

## DSA Integration Matrix

| Topic | Module | Role in Gameplay |
| :--- | :--- | :--- |
| **Queue** | `WaveSpawner.cs`, `EnemySpawnInfo.cs` | Existing FIFO module; possible future encounter or event sequence |
| **Stack** | `ActionHistory.cs`, `PlayerAction.cs` | Existing LIFO module; possible future undo for player actions |
| **Linked List** | `Inventory.cs`, `Item.cs` | Intended storage for gathered resources |
| **Binary Search** | `BinarySearchUtil.cs`, `CraftingRecipe.cs` | Existing sorted lookup; candidate for a simple search puzzle |
| **Insertion Sort** | `InsertionSortUtil.cs` | Intended sorting for the inventory display |
| **Graph / BFS** | `TileGraph.cs` | Map walkability and BFS connectivity validation at startup |

## Current Implementation Status

### Implemented & Fully Playable
- **Village Map:** Validated 36 × 28 text-grid layout (`villagelayout.cs`) with buildings, props, paths, and player spawn.
- **Forest Map:** Tiled TMX map (`Content/forestmap.tmx`) with walkable terrain layers and tree-trunk collision anchors.
- **Movement & World Navigation:** 4-directional tile-locked movement with smooth pixel slides, directional animations, depth sorting, and camera follow.
- **Map Travel:** Inter-map gate transition with interactive `Y/N` travel prompt.
- **Startup Diagnostics:** BFS connectivity checks (`TileGraph`) and automated DSA module verification (`DsaDemo`).

### Standalone DSA Modules (To Be Wired to UI)
- **`Queue`:** Queueing and dequeuing turn/spawn sequence records.
- **`Stack`:** Recording player actions and performing pop-based undo operations.
- **`LinkedList`:** Adding, searching, removing, and iterating inventory items.
- **`BinarySearch`:** Logarithmic lookup over cost-sorted recipe collections.
- **`InsertionSort`:** In-place descending sort for item value lists.

## Roadmap

- [x] MonoGame foundation, sprite rendering, tile movement, and camera
- [x] Village & forest maps, collision, validation, and gate transitions
- [x] Standalone DSA modules and startup self-checks
- [ ] Add a dungeon map and connect it to the existing map-travel flow
- [ ] Add a simple dungeon puzzle guarding a resource cache (start with one puzzle type)
- [ ] Connect interact key (`Space`/`E`) to resource gathering in the forest and dungeon
- [ ] Connect `Inventory` linked list to an on-screen HUD string
- [ ] Use `InsertionSortUtil` to order the displayed inventory
- [ ] Integrate `BinarySearchUtil` into a player-facing puzzle or lookup
- [ ] Consider queue/stack integrations only after the gathering, inventory, and puzzle loop works

## Building and Running

**Prerequisite:** .NET 8 SDK.

From the project directory:

```sh
dotnet restore
dotnet run