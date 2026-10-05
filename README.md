> **This is the sprite-experiment copy**, branched off the main project to
> test real sprite rendering without risking the base build. Textures are
> loaded directly at runtime (`TitleContainer.OpenStream` +
> `Texture2D.FromStream`) rather than through the Content Pipeline, so there is
> no `Content.mgcb` and no `dotnet-mgcb` tool to install. The PNGs in `Content/`
> and its category subfolders are copied next to the executable at build time
> (see `TileQuest.csproj`).
>
> **Content organization:** top-level loose assets are grouped into folders such
> as `Buildings/`, `Fences/`, `Trees/`, `Vegetation/`, `Rocks/`, and
> `Ground/` (with `Details/`, `Terrain/`, and `Tiles/`). Existing collections
> such as `Entities/`, `Environment/`, `Icons/`, `MockUps/`, and `Weapons/`
> keep their existing structure.
>
> **Sprite mapping:** `TileSprites.cs` holds source-rectangle coordinates for
> the tree and rock atlases. Ground and player textures are in `Ground/` and
> `Characters/`, respectively.

# TileQuest — Forest Village Defense
A 2D grid-based action RPG built in MonoGame (C#) for a Data Structures &
Algorithms (DSA) course project. Set across a 5-night story arc, the player awakens
in a quiet forest village, gathers resources, crafts gear, and defends the central
Village Hearth from escalating nightly monster waves — leading up to a confrontation
with a traitorous Village Elder and a final assault on the forest lair.
---
## Story & Narrative Progression (5-Night Arc)
- **Day 1 / Night 1 — Arrival & First Attack:** Wake up in the village after getting lost. Kind villagers take you in; you repay them by defending the Village Hearth during the initial monster raid.
- **Day 2 / Night 2 — Investigation & Forest Patrol:** Venture into the surrounding forest to gather wood/stone, scout monster spawn points, and clear out stray forest beasts.
- **Day 3 / Night 3 — The Monster Hideout:** Discover the monsters' forest lair. Fight through an intense wave, but realize your current gear is insufficient and fall back to the village.
- **Day 4 / Night 4 — The Traitorous Elder:** Uncover secret documents revealing the Village Elder has been orchestrating the attacks to keep the villagers dependent on his power. Defeat the Elder in a 1-on-1 boss battle in the village square to unlock his **Arcane Burst** skill.
- **Day 5 / Night 5 — The Final Stand:** Launch a final assault against the monster horde and lair using your newly acquired Arcane Burst skill to determine the fate of the village.
---
## Village Map Layout
The starting area is generated as a village map rather than a village embedded
inside the forest. Its north side is anchored by the church, four homes sit in
two rows to the east and west, and a central Village Hearth gives the village
square a focal point. A broad dirt crossroads links the southern arrival road
to the church and each home's frontage; small garden beds soften the edges of
the residential rows. Forest generation is kept separate in `ForestGenerator.cs`.

**Editing the forest:** `ForestLayout.cs` is the hand-editable 36x28 forest
grid. `ForestGenerator.cs` reads and validates its symbols at startup; hill and
cave cells are blocked, while the trail and plant decorations remain walkable.
The forest art is rendered from the wall tileset and vegetation sheet.

**Editing the village:** the village is a hand-editable text grid in `VillageLayout.cs` (one character per tile, 36x28, legend and coordinate ruler included). `VillageGenerator.cs` reads and validates it on every launch: layout mistakes stop the game with a list of problems and coordinates, and a `[VillageLayout] OK` line is printed when it passes.

Village ground defaults to grass. Use `=` for cracked roads and `P` for
walkable stone paving; a `P` beside a road draws as a thin sidewalk strip,
while other `P` tiles remain fully paved. Building footprints (`C`, `1`, `2`,
and `H`) are blocked, with stone automatically drawn only under their bottom
row. Building collision covers the solid lower wall sections rather than the
entire roof silhouette. House 2 uses separate colliders for its front and
side-wall sections.
---
## Endings & Game State Machine
The game features three distinct endings based on player actions and survival:
1. **Ending A — True Victory (Hero of the Forest):** Defeat the final boss wave on Night 5. The village is saved, and your final score is recorded on the leaderboard.
2. **Ending B — Defeat (Fall of the Hearth):** The Village Hearth's health drops to 0 during any night phase.
3. **Ending C — Secret Ending (The Deserter):** Choose to step onto the `ForestExit` boundary tile during Day 4 or Day 5 and abandon the village to save yourself.
---
## Required DSA Topics (Grading Checklist)

| # | Topic | File(s) | Status | Game Implementation |
| :--- | :--- | :--- | :--- | :--- |
| 1 | **Queue** — Wave Spawner | `EnemySpawnInfo.cs`, `WaveSpawner.cs` | ✅ Module built | FIFO queue managing nightly enemy spawn order & Elder's summoned adds |
| 2 | **Stack** — Undo System | `PlayerAction.cs`, `ActionHistory.cs` | ✅ Module built | LIFO stack to reverse/undo crafting and defense placement during the Day phase |
| 3 | **LinkedList** — Inventory | `Item.cs`, `Inventory.cs` | ✅ Module built | Dynamic insertion and removal of collected resources and equipment nodes |
| 4 | **Binary Search** — Shop / Recipes | `BinarySearchUtil.cs`, `CraftingRecipe.cs` | ✅ Module built | $O(\log n)$ lookup for items and crafting costs at the Village Trader |
| 5 | **Insertion Sort** — Inventory / Scores | `InsertionSortUtil.cs` | ✅ Module built | Sorting inventory items by value (done); ranking final scores on the leaderboard (Phase 8) |

_"Module built" = the data structure is implemented and self-checked in isolation; wiring it into gameplay happens in the phase listed below._

---
## Project Phases (Expanded Development Roadmap)

| Phase | Scope & Feature Focus | Status |
| :--- | :--- | :--- |
| **Phase 1** | Engine foundation, grid movement, sprite mapping (`TileSprites.cs`), and isolated DSA modules (verified by `DsaDemo.cs`) | ✅ Complete |
| **Phase 2** | Fixed Village/Forest map layout, Day/Night `TimeSystem`, and `VillageHearth` state | Not Started |
| **Phase 3** | Resource harvesting (wood/stone) & `LinkedList` inventory integration | Not Started |
| **Phase 4** | Barricade crafting & `Stack<Action>` Undo building system | Not Started |
| **Phase 5** | `Queue` wave spawner integration (Nights 1–3), melee combat, & Hearth HP tracking | Not Started |
| **Phase 6** | **Night 4 Traitorous Elder Boss fight** & unlocking the **Arcane Burst** spell | Not Started |
| **Phase 7** | Village Trader shop (`BinarySearch`), Night 5 Lair Finale, & 3 Narrative Endings | Not Started |
| **Phase 8** | High score leaderboard (`InsertionSort`), HUD polish, & final presentation cleanup | Not Started |

---
## Building & Running
**Prerequisites:** .NET SDK (8.0 or later).
```bash
# Restore dependencies and launch the game
dotnet restore
dotnet run
```

On startup the console prints a `=== DSA module self-check ===` report
(`DsaDemo.cs`) and a `[TileGraph]` map connectivity check before the game
window opens. Every line should read `PASS` / `OK`. Controls: WASD or arrow keys
to move. Walk onto the marked gate at the south edge of the village or forest
to get a travel prompt; press Y or Enter to travel, N or Esc to stay. Esc
outside a travel prompt quits the game.