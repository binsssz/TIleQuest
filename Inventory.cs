using System.Collections.Generic;

namespace TileQuest
{
    // DSA note: LinkedList<Item> piece. Adding or removing an item is O(1)
    // once you have the node — no shifting every subsequent element down one
    // slot the way List<T>.RemoveAt(middleIndex) or a plain array would need.
    // That fits an inventory that's constantly gaining drops and losing items
    // to crafting/building, often from the middle of the collection.
    public class Inventory
    {
        private readonly LinkedList<Item> _items = new();

        public IReadOnlyCollection<Item> Items => _items;

        // O(1): appends to the tail, no shifting of existing items.
        public void AddItem(Item item)
        {
            _items.AddLast(item);
        }

        // O(n) to find the node by name, but O(1) to unlink it once found —
        // the remaining items don't need to move.
        public bool RemoveItem(string name)
        {
            var node = FindNode(name);
            if (node == null)
            {
                return false;
            }
            _items.Remove(node);
            return true;
        }

        public Item? Find(string name)
        {
            return FindNode(name)?.Value;
        }

        private LinkedListNode<Item>? FindNode(string name)
        {
            for (var node = _items.First; node != null; node = node.Next)
            {
                if (node.Value.Name == name)
                {
                    return node;
                }
            }
            return null;
        }

        // A snapshot sorted highest-value-first, for the inventory display.
        // Reuses InsertionSortUtil — the same sort the leaderboard uses.
        public List<Item> GetSortedByValue()
        {
            var snapshot = new List<Item>(_items);
            InsertionSortUtil.SortDescending(snapshot, item => item.Value);
            return snapshot;
        }
    }
}
