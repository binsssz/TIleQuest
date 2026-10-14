# TileQuest — Forest Village RPG

TileQuest is a 2D, grid-based exploration game and Data Structures & Algorithms (DSA) course project built with C# and MonoGame. The current playable prototype includes a village, a forest, a dungeon, map travel, resource gathering, basic melee combat, a five-night village hearth-defense loop, a village trader who crafts weapons with undo, a food vendor, a linked-list quest log, and Victory and Defeat ending screens. Startup checks cover the DSA modules: Queue, Stack, Linked List (quest log), Binary Search, Insertion Sort, and Linear Search.

The game is in a solid playable state for a course project and is ready for demo and review. Save/load lets players continue a run across sessions.

## Current Implementation

### Start menu

- The game opens on a title screen drawn over the dimmed village (the world is frozen behind it). `W`/`S` or the arrows move, `Enter` / `E` / `Space` confirm, `Esc` quits. **New Game** starts a fresh run, **Load Game** restores saved progress when a save file exists, **How to Play** shows the controls and goal, and **Quit** exits. The menus use `GameScreen` (`Title`, `HowToPlay`, `Playing`), separate from the Day/Night `GameState`.

### World and navigation

- **Village:** A 48 × 36 collision grid (`CollisionMapLayouts.cs`) rendered with `Assets/village_ground.png` as the base, `Assets/village_objects_ground_decor.png` behind actors, and `Assets/village_objects_main.png` over actors. Buildings, props, and the pond block movement; the wizard is the forest travel point.
- **Forest:** A 36 × 29 collision grid from `forestlayout.cs`, rendered with `Assets/forest_ground.png` and `Assets/forest_canopy.png`. The canopy is drawn over the player. The dungeon entrance is the three walkable tiles at x=30–32, y=15. The forest exit to the village is at `ForestLayout.Gate` (18, 17).
- **Dungeon:** A 36 × 28 collision grid (`DungeonCollisionLayout` in `CollisionMapLayouts.cs`) displayed over `Assets/dungeon.png`, with a dark screen-space vignette. Its exit returns to the forest entrance.
- **Movement:** Four-directional tile-locked movement with smooth slides, collision, animation, depth sorting, and camera follow.
- **Travel:** Stand beside the village wizard and press `E` to request forest travel. Stepping on a map exit or entrance requests travel. Confirm with `Y` or `Enter`; cancel with `N` or `Escape`. Prompts appear at the bottom center of the screen.
- **Harvesting and inventory:** Press `Space` or `J` to swing at resource nodes and enemies. Harvested resources, enemy loot, crafted weapons, and food are stored in the Dictionary-based inventory (one count per item type) and shown in a two-row HUD at the top-left: weapons and food on the top row, resources on the bottom row, each slot an icon plus its quantity (only owned items appear). Press `I` to open the full inventory menu with every row sorted by market value (highest first; press `I` again inside the menu to return to the default order, and any inventory change also returns to it), or `E` to open it in the default order when no nearby interaction is available. Use `W`/`S` or the arrow keys to select an item, `Enter` to equip a crafted weapon, `D` to discard one unit, and `Esc` to close. Food items can be consumed to restore energy.
- **Quest log:** The objective tracks gathering 10 Wood, gathering 5 Stone, crafting a weapon, and surviving Nights 1–5. Talk to the wizard before Night 1, then complete the gather-and-craft objectives to unlock the first night. A five-enemy bonus quest is inserted after surviving Night 2. Press `Q` to show or hide the ordered quest list; it does not pause gameplay.
- **Combat:** Swings damage and knock back enemies. Night goblins and skeletons march toward the village hearth and attack it, or the player if they end up beside them. Each attack is a visible wind-up, then one impact, then a recovery, once per second per enemy, and the damage only lands if the target is still adjacent at the impact moment, so you can dodge a swing by stepping away in time (hitting an enemy interrupts its swing); the forest and dungeon enemies (including the dungeon's 32 skeletons) stay put until you come within about seven tiles, then chase you and attack with the same dodgeable swing; if you fall there you wake up in the village at full health.
- **Night defense:** Stand beside the village hearth and press `E`, then confirm with `Y` or `Enter` to start the next wave. Fade-to-black cutscenes mark sunset and dawn, with the night wave starting at full black and the day returning under a **DAY** title. A FIFO queue releases a mix of goblins and armed skeletons over time (skeletons are a growing share of each night, with new variants joining on later nights); skeletons carry a bone spear and use a BFS distance field to find the hearth. The hearth has 100 HP that persists between nights. Clear the wave to advance the day and regrow resource nodes. The HUD shows the day/night, remaining foes, and hearth HP. The night is lost if the hearth is destroyed or the player falls; either opens the Defeat screen. Clearing Night 5 opens the Victory screen (see **Endings** below).
- **Endings:** Clearing Night 5 opens the Victory screen. A destroyed hearth, or the player falling during a night, opens the Defeat screen. Both show a summary (nights survived, enemies defeated, quests completed, hearth HP) and offer Play Again, Return to Title, or Quit. Play Again and Return to Title both reset the run to a fresh world. Input is ignored for the first second so the screen is not dismissed by accident. There is no score or leaderboard.
- **Food vendor:** Stand beside the food vendor in the village (tile (32, 29)) and press `E` to open her menu. `W`/`S` select, `E` or `Enter` buys with Gold, and `Esc` closes. Bread costs 1 Gold (+20 energy), Stew 2 (+40), Roast Meat 3 (+65), and Pie 4 (+90). Food is eaten from the inventory menu: select it and press `Enter`.
- **Trader and crafting:** In the day, stand beside the trader (the tavern woman in the forge area) and press `E` to open her menu. `W`/`S` select a recipe, `E` crafts it (spending materials from the inventory), and `Esc` closes. Weapon sprites appear in the recipe list and inventory; crafted weapons add swing damage and the strongest weapon is shown above the inventory.
- **Undo:** Press `U` (in the day, with or without the trader menu open) to undo the most recent craft: the gear is removed and its materials refunded. Undo history is cleared at dawn.
- **Player health and energy:** The lower-left bars are live (`PlayerStats.cs`). Energy is a hunger-style bar: it drains slowly over time, is not spent on swings, and fuels healing. While health is missing and energy remains, health regenerates (a few seconds after the last hit) and burns energy; at 0 energy there is no healing. Health and energy carry over at dawn; only a respawn or a new run refills them. If the player falls in the forest or dungeon, they respawn at the village spawn with full health and energy; a fall during a night is a defeat. Night wave enemies standing next to the player hit the player instead of the hearth (their hearth damage ×2).
- **HUD:** The two-row inventory HUD, current objective and expandable quest log, hearth status, and player health/energy bars are functional.
- **Debugging:** Press `F3` to toggle hitbox overlays.
- **Save/load:** Press `Esc` to open the pause menu (Resume, Save Game, How to Play, Return to Title, Quit) and choose **Save Game**. **Load Game** on the title screen resumes that save, including inventory, quests, player stats and location, world resources and chests, enemies, and active night-wave progress. The save is stored at `%LOCALAPPDATA%\TileQuest\savegame.json` on Windows.
- **Startup checks:** `TileGraph` checks map connectivity; `DsaDemo` self-checks Queue, Stack, Linked List (quest log), Binary Search, Insertion Sort, and Linear Search.

### DSA modules

| Topic | Module | Current use |
| :--- | :--- | :--- |
| Queue | `WaveSpawner.cs`, `EnemySpawnInfo.cs` | Feeds the night wave: goblins and skeletons dequeue in order and spawn at the village edge |
| Stack | `ActionHistory.cs`, `PlayerAction.cs`, `Crafting.cs` | LIFO undo of crafting: `U` pops the last craft, removes the gear and refunds its materials |
| Linked List (quest log) | `Quest.cs`, `QuestLog.cs` | Stores the ordered objectives and advances the current quest as gameplay events occur |
| Binary Search | `BinarySearchUtil.cs`, `RecipeBook.cs`, `CraftingRecipe.cs`, `Foodbook.cs` | The trader resolves a recipe's cost with binary search over the cost-sorted recipe book; the food vendor does the same for food prices |
| Insertion Sort | `InsertionSortUtil.cs` | Sorts inventory and other item snapshots in descending value order |
| Linear Search | `LinearSearchUtil.cs` | Finds the living enemy on a swing tile and adjacent interactables |

The Dictionary-backed inventory remains the game's item storage; graph/BFS logic remains responsible for map connectivity and night enemy pathfinding.

## Building and Running

**Prerequisite:** .NET 8 SDK.

From the project directory:

```sh
dotnet restore
dotnet build
dotnet run
```

The game directly loads PNG assets from the top-level `Assets` folder with `TitleContainer.OpenStream`; subfolders are preserved to organize the sprites and other files. No MonoGame content pipeline build is required. The project copies PNG assets and the forest TMX file from `Assets` to the build output.