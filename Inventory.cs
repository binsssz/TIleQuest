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

        // Total quantity across every node with this name (pickups are stored
        // as separate nodes, so a "stack" of wood is really several nodes).
        public int CountOf(string name)
        {
            int total = 0;
            foreach (Item item in _items)
            {
                if (item.Name == name)
                {
                    total += item.Quantity;
                }
            }
            return total;
        }

        // Removes quantity units of name, draining nodes from the front and
        // unlinking any that empty. All-or-nothing: returns false and changes
        // nothing if the inventory holds fewer than quantity.
        public bool RemoveQuantity(string name, int quantity)
        {
            if (quantity <= 0 || CountOf(name) < quantity)
            {
                return false;
            }

            int remaining = quantity;
            var node = _items.First;
            while (node != null && remaining > 0)
            {
                var next = node.Next;
                if (node.Value.Name == name)
                {
                    if (node.Value.Quantity <= remaining)
                    {
                        remaining -= node.Value.Quantity;
                        _items.Remove(node);
                    }
                    else
                    {
                        node.Value.Quantity -= remaining;
                        remaining = 0;
                    }
                }
                node = next;
            }
            return true;
        }

        public bool RemoveOne(string name, string category)
        {
            for (var node = _items.First; node != null; node = node.Next)
            {
                if (node.Value.Name != name || node.Value.Category != category)
                {
                    continue;
                }

                if (node.Value.Quantity <= 1)
                {
                    _items.Remove(node);
                }
                else
                {
                    node.Value.Quantity--;
                }
                return true;
            }

            return false;
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
