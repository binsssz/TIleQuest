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
            DemoLinkedList();
            DemoBinarySearch();
            DemoInsertionSort();
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
            // 2s per step is longer than the 1.5s spawn delay, so each call
            // releases exactly one enemy.
            while (!spawner.IsWaveComplete)
            {
                var enemy = spawner.Update(2f);
                if (enemy != null)
                {
                    spawned++;
                    Console.WriteLine($"    spawn #{spawned}: {enemy.EnemyType} (HP {enemy.Health}, speed {enemy.Speed:0.0})");
                }
            }
            Check("every queued enemy spawned, then the wave is complete", spawned == expected && spawner.IsWaveComplete);

            spawner.QueueWave(nightNumber: 3);
            Check("later nights are bigger (night 3 > night 1)", spawner.RemainingInWave > expected);
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

            Check("cost 35 found at index 3 (Reinforced Gate)",
                BinarySearchUtil.FindByKey(recipes, 35, r => r.Cost) == 3);
            Check("first element found (cost 5 -> index 0)",
                BinarySearchUtil.FindByKey(recipes, 5, r => r.Cost) == 0);
            Check("last element found (cost 75 -> index 5)",
                BinarySearchUtil.FindByKey(recipes, 75, r => r.Cost) == 5);
            Check("missing cost returns -1",
                BinarySearchUtil.FindByKey(recipes, 999, r => r.Cost) == -1);
            Check("empty list returns -1",
                BinarySearchUtil.FindByKey(new List<CraftingRecipe>(), 5, r => r.Cost) == -1);
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
