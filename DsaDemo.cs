using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace TileQuest
{
    // Phase 1 proof that each DSA module works on its own, before any of them
    // is wired into the game. Run() prints one PASS/FAIL line per check and a
    // summary. Nothing here touches MonoGame graphics.
    public static class DsaDemo
    {
        private static int _passed;
        private static int _failed;

        public static void Run()
        {
            _passed = 0;
            _failed = 0;

            Console.WriteLine("=== DSA module self-check ===");
            DemoQueue();
            DemoStack();
            DemoCraftingUndo();
            DemoLinkedList();
            DemoBinarySearch();
            DemoInsertionSort();
            DemoPathField();
            Console.WriteLine($"=== {_passed} passed, {_failed} failed ===");
        }

        private static void Check(string label, bool ok)
        {
            Console.WriteLine($"  [{(ok ? "PASS" : "FAIL")}] {label}");
            if (ok) _passed++; else _failed++;
        }

        // Queue: enemies must come out in the order they were enqueued.
        private static void DemoQueue()
        {
            Console.WriteLine("Queue (WaveSpawner)");
            var spawner = new WaveSpawner();
            spawner.QueueWave(nightNumber: 1);

            int expected = 3 + 1 * 2;
            Check($"night 1 queues {expected} enemies", spawner.RemainingInWave == expected);

            int spawned = 0;
            int goblins = 0;
            int skeletons = 0;
            void Tally(EnemySpawnInfo info)
            {
                if (info.Kind == EnemyKind.Skeleton)
                {
                    skeletons++;
                }
                else
                {
                    goblins++;
                }
            }

            EnemySpawnInfo? first = spawner.Update(0f);
            if (first != null)
            {
                spawned++;
                Tally(first);
            }
            Check("first enemy spawns immediately", spawned == 1 && spawner.RemainingInWave == expected - 1);

            bool waitedForDelay = spawner.Update(1f) == null && spawner.Update(0.49f) == null;
            Check("next enemy waits for its 1.5 second delay", waitedForDelay);

            EnemySpawnInfo? second = spawner.Update(0.02f);
            if (second != null)
            {
                spawned++;
                Tally(second);
            }
            Check("next enemy spawns after the delay", spawned == 2 && spawner.RemainingInWave == expected - 2);

            // 2s per step is longer than the 1.5s spawn delay, so each call
            // releases exactly one remaining enemy.
            while (!spawner.IsWaveComplete)
            {
                var enemy = spawner.Update(2f);
                if (enemy != null)
                {
                    spawned++;
                    Tally(enemy);
                    Console.WriteLine($"    spawn #{spawned}: {enemy.EnemyType} (HP {enemy.Health}, speed {enemy.Speed:0.0})");
                }
            }
            Check("every queued enemy spawned, then the wave is complete", spawned == expected && spawner.IsWaveComplete);
            Check($"night 1 mixes goblins and skeletons ({goblins} + {skeletons})", goblins > 0 && skeletons > 0);

            spawner.QueueWave(nightNumber: 3);
            Check("later nights are bigger (night 3 > night 1)", spawner.RemainingInWave > expected);

            spawner.QueueWave(nightNumber: 5);
            Check("night 5 queues 13 enemies", spawner.RemainingInWave == 3 + 5 * 2);
            spawner.ClearWave();
            Check("ClearWave empties the queue", spawner.IsWaveComplete && spawner.Update(2f) == null);
        }

        // Stack: undo must remove the most recent action first (LIFO).
        private static void DemoStack()
        {
            Console.WriteLine("Stack (ActionHistory)");
            var history = new ActionHistory();
            history.Record(new PlayerAction(PlayerActionType.PlaceDefense, "Wooden Spike", 5, new Point(3, 4)));
            history.Record(new PlayerAction(PlayerActionType.PlaceDefense, "Stone Wall", 10, new Point(5, 4)));
            history.Record(new PlayerAction(PlayerActionType.PurchaseUpgrade, "Iron Axe", 20));

            Check("three actions recorded", history.Count == 3);

            var undone = history.Undo();
            Check("first undo returns the LAST action (Iron Axe)", undone?.Description == "Iron Axe");
            Check("second undo returns Stone Wall", history.Undo()?.Description == "Stone Wall");
            Check("one action left to undo", history.CanUndo && history.Count == 1);

            history.ClearForNewDay();
            Check("new day clears history", !history.CanUndo && history.Undo() == null);
        }

        // Trader crafting + undo: materials come out of the linked-list
        // inventory, the transaction goes on the stack, and U pops it again.
        private static void DemoCraftingUndo()
        {
            Console.WriteLine("Crafting + Undo (Inventory, RecipeBook, ActionHistory)");
            var inventory = new Inventory();
            var history = new ActionHistory();
            var club = RecipeBook.FindByName("Wooden Sword")!;
            var mace = RecipeBook.FindByName("Stone Sword")!;

            // Wood arrives as two separate pickups, like it does in the game.
            inventory.AddItem(new Item("Wood", "Resource", 3, 2));
            inventory.AddItem(new Item("Wood", "Resource", 2, 2));
            inventory.AddItem(new Item("Stone", "Resource", 3, 3));
            Check("CountOf totals wood across separate pickups (5)", inventory.CountOf("Wood") == 5);
            var discardInventory = new Inventory();
            discardInventory.AddItem(new Item("Wood", "Resource", 3, 2));
            Check("discarding one wood leaves the rest of the stack",
                discardInventory.RemoveOne("Wood", "Resource") && discardInventory.CountOf("Wood") == 2);
            Check("discarding from the wrong category changes nothing",
                !discardInventory.RemoveOne("Wood", "Loot") && discardInventory.CountOf("Wood") == 2);

            Check("Wooden Sword (4 wood) crafts", Crafting.TryCraft(inventory, history, club));
            Check("4 wood spent, 1 left", inventory.CountOf("Wood") == 1);
            Check("wooden sword is equipped", Crafting.GetBestWeapon(inventory)?.Name == "Wooden Sword");

            Check("Stone Sword fails with only 1 wood", !Crafting.TryCraft(inventory, history, mace));
            Check("a failed craft changes nothing", inventory.CountOf("Wood") == 1 &&
                inventory.CountOf("Stone") == 3 && history.Count == 1);

            inventory.AddItem(new Item("Wood", "Resource", 1, 2));
            Check("Stone Sword crafts once the wood is there", Crafting.TryCraft(inventory, history, mace));
            Check("stone sword (stronger) is now equipped", Crafting.GetBestWeapon(inventory)?.Name == "Stone Sword");
            Check("materials spent (0 wood, 0 stone)", inventory.CountOf("Wood") == 0 && inventory.CountOf("Stone") == 0);

            var undone = Crafting.UndoLast(inventory, history);
            Check("first undo reverses the LAST craft (Stone Sword)", undone?.Description == "Stone Sword");
            Check("stone sword removed and materials refunded", inventory.Find("Stone Sword") == null &&
                inventory.CountOf("Wood") == 2 && inventory.CountOf("Stone") == 3);
            Check("equipment falls back to the wooden sword", Crafting.GetBestWeapon(inventory)?.Name == "Wooden Sword");

            Check("second undo reverses the wooden sword", Crafting.UndoLast(inventory, history)?.Description == "Wooden Sword");
            Check("wooden sword removed, all 6 wood back", inventory.Find("Wooden Sword") == null && inventory.CountOf("Wood") == 6);
            Check("nothing left to undo", Crafting.UndoLast(inventory, history) == null);

            Check("RemoveQuantity refuses to overspend", !inventory.RemoveQuantity("Wood", 99) && inventory.CountOf("Wood") == 6);
        }

        // LinkedList: add, remove from the middle, then read back.
        private static void DemoLinkedList()
        {
            Console.WriteLine("LinkedList (Inventory)");
            var inventory = new Inventory();
            inventory.AddItem(new Item("Wood", "Resource", quantity: 10, value: 2));
            inventory.AddItem(new Item("Stone", "Resource", quantity: 5, value: 3));
            inventory.AddItem(new Item("Iron Axe", "Tool", quantity: 1, value: 20));
            inventory.AddItem(new Item("Torch", "Tool", quantity: 2, value: 8));

            Check("four items added", inventory.Items.Count == 4);
            Check("Find locates an item by name", inventory.Find("Stone")?.Quantity == 5);
            Check("Find returns null for a missing item", inventory.Find("Diamond") == null);

            Check("RemoveItem removes a middle item", inventory.RemoveItem("Stone"));
            Check("removed item is gone, others remain", inventory.Find("Stone") == null && inventory.Items.Count == 3);
            Check("RemoveItem on a missing name returns false", !inventory.RemoveItem("Stone"));

            var order = inventory.Items.Select(i => i.Name).ToList();
            Check("insertion order preserved (Wood, Iron Axe, Torch)",
                order.SequenceEqual(new[] { "Wood", "Iron Axe", "Torch" }));
        }

        // Binary search: only valid on data sorted ascending by the key.
        private static void DemoBinarySearch()
        {
            Console.WriteLine("Binary Search (BinarySearchUtil + RecipeBook)");
            var recipes = RecipeBook.SortedByCost;

            bool sorted = true;
            for (int i = 1; i < recipes.Count; i++)
            {
                if (recipes[i - 1].Cost > recipes[i].Cost) sorted = false;
            }
            Check("RecipeBook is sorted ascending by cost", sorted);

            bool unique = true;
            for (int i = 1; i < recipes.Count; i++)
            {
                if (recipes[i - 1].Cost == recipes[i].Cost) unique = false;
            }
            Check("every recipe cost is unique (one match per key)", unique);

            Check("cost 22 found at index 2 (Iron Sword)",
                BinarySearchUtil.FindByKey(recipes, 22, r => r.Cost) == 2);
            Check("the trader has four weapon recipes", recipes.Count == 4);
            Check("first element found (cost 8 -> index 0)",
                BinarySearchUtil.FindByKey(recipes, 8, r => r.Cost) == 0);
            Check("last element found (cost 46 -> index 3)",
                BinarySearchUtil.FindByKey(recipes, 46, r => r.Cost) == 3);
            Check("missing cost returns -1",
                BinarySearchUtil.FindByKey(recipes, 999, r => r.Cost) == -1);
            Check("empty list returns -1",
                BinarySearchUtil.FindByKey(new List<CraftingRecipe>(), 5, r => r.Cost) == -1);

            Check("RecipeBook.FindByCost(36) is null", RecipeBook.FindByCost(36) == null);
            Check("RecipeBook.FindByCost(14) is null", RecipeBook.FindByCost(14) == null);
            Check("recipe cost equals the value of its ingredients",
                recipes.All(r => r.Cost == r.Ingredients.Sum(i => i.Quantity * i.UnitValue)));
        }

        // Graph + BFS: the distance field goblins follow to the hearth. 5x3 yard,
        // a wall across x=2 with a gap at y=2, and the hearth at (4,1).
        private static void DemoPathField()
        {
            Console.WriteLine("Graph + BFS (HearthPathField)");
            var blocked = new HashSet<Point> { new(2, 0), new(2, 1), new(4, 1) };
            var field = new HearthPathField(
                new[] { new Point(4, 1) },
                tile => !blocked.Contains(tile),
                width: 5,
                height: 3);

            Check("tiles beside the hearth are distance 0", field.DistanceTo(new Point(3, 1)) == 0);
            Check("BFS counts the walk through the gap (2,2) = 2", field.DistanceTo(new Point(2, 2)) == 2);
            Check("walls are unreachable (-1)", field.DistanceTo(new Point(2, 1)) == -1);
            Check("NextStep moves to the closer tile",
                field.NextStep(new Point(1, 1), _ => false) == new Point(1, 2));
            Check("NextStep waits when every closer tile is taken",
                field.NextStep(new Point(1, 1), tile => tile == new Point(1, 2)) == null);
            Check("NextStep returns null once at the hearth",
                field.NextStep(new Point(3, 1), _ => false) == null);
        }

        // Insertion sort: highest value first.
        private static void DemoInsertionSort()
        {
            Console.WriteLine("Insertion Sort (InsertionSortUtil)");
            var scores = new List<int> { 30, 90, 10, 60, 60 };
            InsertionSortUtil.SortDescending(scores, s => s);
            Check("scores sorted descending (90, 60, 60, 30, 10)",
                scores.SequenceEqual(new[] { 90, 60, 60, 30, 10 }));

            var inventory = new Inventory();
            inventory.AddItem(new Item("Wood", "Resource", 10, 2));
            inventory.AddItem(new Item("Iron Axe", "Tool", 1, 20));
            inventory.AddItem(new Item("Torch", "Tool", 2, 8));
            var byValue = inventory.GetSortedByValue().Select(i => i.Name).ToList();
            Check("inventory sorted by value (Iron Axe, Torch, Wood)",
                byValue.SequenceEqual(new[] { "Iron Axe", "Torch", "Wood" }));

            var empty = new List<int>();
            InsertionSortUtil.SortDescending(empty, s => s);
            Check("empty list sorts without error", empty.Count == 0);
        }
    }
}
