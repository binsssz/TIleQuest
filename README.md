# TileQuest — Forest Village Defense

TileQuest is a 2D, grid-based exploration game and Data Structures & Algorithms
(DSA) course project built with C# and MonoGame. The current playable build is
an overworld prototype: explore a hand-authored village and a Tiled forest map,
move between them, and inspect the DSA modules through their startup
self-check. The planned defense campaign is not yet implemented.

## Current Progress

### Implemented and playable

- **Village map:** a validated 36 × 28 text-grid layout with buildings, props,
  paths, gardens, and a player spawn.
- **Forest map:** loaded from [`Content/forestmap.tmx`](Content/forestmap.tmx),
  including its tilesets, walkable layers, and tree collision anchors.
- **Movement and camera:** four-direction, tile-locked movement with a smooth
  slide, facing and walking animation state, collision, and camera follow.
- **Map travel:** walk to the connected gate and accept or cancel the prompt to
  move between the village and forest.
- **Rendering and debugging:** sprite-based scenery, depth sorting around the
  player, and an F3 hitbox overlay.
- **Map checks:** layout validation and walkability/connectivity checks run as
  maps are created.
- **DSA demonstrations:** the queue, stack, linked-list inventory, binary
  search, and insertion sort modules are exercised by a console self-check
  before the game window opens.

### Implemented as modules, not wired into gameplay

The DSA classes are real implementations with self-checks, but they do not yet
power player-facing defense, crafting, shop, or score systems. `Inventory` can
add, find, remove, and sort items; `WaveSpawner` can queue timed enemy spawn
records; `ActionHistory` can record and undo actions; recipe lookup and
insertion sorting are also demonstrated in isolation.

### Not implemented yet

- Day/night progression, harvesting, resource collection, or an in-game
  inventory UI.
- Building/crafting gameplay and a player-triggered undo flow.
- Live enemies, combat, Village Hearth health, or playable enemy waves.
- A trader/shop, boss encounters, narrative choices, endings, or leaderboard.

These are goals in the project outline, not features available in the current
build.

## DSA Progress

| Topic | Module | Current status |
| --- | --- | --- |
| Queue | [`WaveSpawner.cs`](WaveSpawner.cs), `EnemySpawnInfo.cs` | Implemented and self-checked; not connected to live enemies |
| Stack | [`ActionHistory.cs`](ActionHistory.cs), `PlayerAction.cs` | Implemented and self-checked; not connected to building or purchases |
| Linked list | [`Inventory.cs`](Inventory.cs), `Item.cs` | Implemented and self-checked; not connected to a gameplay inventory |
| Binary search | [`BinarySearchUtil.cs`](BinarySearchUtil.cs), `CraftingRecipe.cs` | Implemented and self-checked; no in-game shop |
| Insertion sort | [`InsertionSortUtil.cs`](InsertionSortUtil.cs) | Implemented and self-checked; used for inventory snapshots, no leaderboard |
| Graph / BFS | [`TileGraph.cs`](TileGraph.cs) | Used for map walkability and connectivity validation |

## Roadmap

The following is the intended direction; statuses reflect the current code, not
the original proposal.

| Area | Status |
| --- | --- |
| MonoGame foundation, sprite rendering, movement, and camera | Complete |
| Village/forest maps, collision, validation, and gate travel | Complete |
| DSA modules and startup self-check | Complete as standalone modules |
| Day/night loop, harvesting, and inventory gameplay | Not started |
| Building, crafting, and gameplay undo | Not started |
| Enemy combat, live wave defense, and Hearth health | Not started |
| Trader, bosses, narrative endings, and leaderboard | Not started |

The design goal is a five-night village-defense story, but that campaign should
not be considered implemented until its systems are added to the game loop.

## Building and Running

**Prerequisite:** .NET 8 SDK.

From this directory:

```sh
dotnet restore
dotnet run
```

The project targets `net8.0` and uses `MonoGame.Framework.DesktopGL`. PNG assets
and the forest TMX are copied to the output directory and loaded directly at
runtime; there is no MonoGame content-pipeline build step. On startup, check
the console for the DSA self-check and map validation/connectivity output.

## Controls

| Input | Action |
| --- | --- |
| WASD / Arrow keys | Move |
| Y / Enter | Accept map travel |
| N / Escape | Cancel map travel |
| Escape | Quit when not choosing map travel |
| F3 | Toggle hitbox overlay |

## Map Authoring

- Edit the village grid and its legend in [`villagelayout.cs`](villagelayout.cs).
  `VillageGenerator` validates its dimensions, symbols, structures, spawn, gate,
  and connectivity at startup.
- The live forest is [`Content/forestmap.tmx`](Content/forestmap.tmx). Its
  `foothill` and `foothill grass` layers define walkable tiles; tree-base
  anchors in `foothill trees` block movement.
- Keep the two maps' connected gate and arrival positions walkable when
  changing either layout.

For more detailed codebase orientation and implementation constraints, see
[`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md).
