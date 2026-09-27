# TileQuest — Village Survival RPG (working title)

A Pokemon-style top-down game built in MonoGame for a DSA (Data Structures &
Algorithms) course project. Current direction: a village survival/defense
game — gather and build by day, defend the VillageHearth from waves of
enemies by night, across 5 required DSA topics and a multi-ending state
machine.

Rename the project any time by renaming `TileQuest.csproj`, the folder, and
the `RootNamespace`/`namespace TileQuest` references.

## Required DSA topics (grading checklist)

| # | Topic | File(s) | Status |
|---|---|---|---|
| 1 | Queue — wave spawner | `EnemySpawnInfo.cs`, `WaveSpawner.cs` | ✅ built, not yet wired into `Game1` |
| 2 | Stack — day-phase undo | `PlayerAction.cs`, `ActionHistory.cs` | ✅ built, not yet wired into `Game1` |
| 3 | LinkedList — inventory | `Item.cs`, `Inventory.cs` | ✅ built, not yet wired into `Game1` |
| 4 | Binary Search — recipes/shop lookup | `BinarySearchUtil.cs`, `CraftingRecipe.cs` | ✅ built, not yet wired into `Game1` |
| 5 | Insertion Sort — inventory/leaderboard | `InsertionSortUtil.cs` | ✅ built, already used by `Inventory.GetSortedByValue()` |

Each of these five is a standalone, self-contained class right now —
correct in isolation, but not yet hooked up to the game loop. That wiring is
Phase 3 (combat/wave loop) and Phase 4 (state machine) below.

## Project phases (current plan)

| Phase | Covers | Status |
|---|---|---|
| 1 | Data structures setup (this batch) | ✅ done |
| 2 | World refactor to village theme + day/night `TimeSystem` | not started |
| 3 | Wave spawning, combat, Hearth HP, wiring the undo stack | not started |
| 4 | `GameState` machine + 3 endings (Victory/Defeat/Secret Escape) | not started |
| 5 | Save/load, HUD, leaderboard wiring, presentation comments | not started |

## Carried over from the earlier forest-exploration build

These aren't on the 5 required topics above, but they still work and don't
conflict with anything, so they're staying rather than being thrown away:

- **`Camera2D.cs`** — target-following camera with world-bounds clamping.
  Needs no changes.
- **`Player.cs`** — Pokemon-style grid movement. Needs no changes for Phase 1;
  Phase 3 will likely extend it for attacking.
- **`ForestGenerator.cs`** (recursion) and **`TileGraph.cs`** (graph + BFS) —
  these generate/verify the current forest map. Phase 2 will retheme the
  tile set to the village (`Grass`, `Tree` as a hard wall, `VillageHearth`,
  `ShopNPC`, `ForestExit`), at which point these get adapted rather than
  removed — the underlying "scatter obstacles, then verify reachability"
  logic still applies to a village bordered by trees.
- **`TileMap.cs`** (hash table) — same deal; the `Dictionary<Point, TileType>`
  storage stays, the `TileType` enum gets replaced in Phase 2.

## Phase 1 details

**`WaveSpawner` (Queue)** — `QueueWave(nightNumber)` fills a
`Queue<EnemySpawnInfo>` with that night's enemies (count/health/speed scale
with night number). `Update(deltaSeconds)` dequeues one enemy at a time as
each one's `SpawnDelaySeconds` elapses — FIFO order, exactly matching "wave
composition is authored in a specific order."

**`ActionHistory` (Stack)** — `Record(action)` pushes a `PlayerAction` when
the player places a defense or buys an upgrade. `Undo()` pops the most
recent one so the caller can remove the placement and refund
`ResourceCost` — LIFO, so undo always affects the *last* action, not the
first.

**`Inventory` (LinkedList)** — `AddItem`/`RemoveItem` operate on a
`LinkedList<Item>` for O(1) add/remove-once-found, instead of a `List<T>`
that has to shift every following element on a middle removal.
`GetSortedByValue()` returns a sorted snapshot via `InsertionSortUtil`.

**`BinarySearchUtil` (Binary Search)** — `FindByKey(sortedItems, target,
keySelector)` is a hand-rolled O(log n) search over any list sorted
ascending by an int key. `RecipeBook.SortedByCost` is a small hardcoded,
cost-sorted `CraftingRecipe` list to exercise it against; the real shop UI
(Phase 2/3) can extend or replace that list as long as it stays sorted.

**`InsertionSortUtil` (Insertion Sort)** — `SortDescending(items,
keySelector)` is a hand-rolled insertion sort, generic over any key. Used by
`Inventory.GetSortedByValue()` now; Phase 4's leaderboard will call the same
method on score entries.

## Building & running

You'll need the .NET SDK (8.0 or later) and internet access once, to restore
the MonoGame NuGet package:

```
dotnet restore
dotnet run
```

Or open the folder in Visual Studio / Rider / VS Code with the C# extension
and run from there.

**Controls:** WASD or arrow keys to move, Esc to quit.

## Swapping in real sprites

Everything currently draws as tinted rectangles via a single 1x1 pixel
texture (see `LoadContent()` in `Game1.cs`). When you're ready for real art:

1. Grab a free, license-clear top-down tile/character pack — Kenney.nl and
   OpenGameArt.org both have CC0 packs that fit this style well.
2. Add a `Content.mgcb` file and reference `MonoGame.Content.Builder.Task`
   in the `.csproj` (the standard MonoGame content pipeline).
3. Replace the `_pixel` draws in `Game1.cs` with `Texture2D` sprites loaded
   via `Content.Load<Texture2D>(...)`.

This is deliberately left out for now so the build has zero external
dependencies beyond the MonoGame package itself.
