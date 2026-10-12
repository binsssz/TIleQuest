using System;
using System.Collections.Generic;

namespace TileQuest
{
    // DSA note: Dictionary (hash table) piece. The inventory is a count per
    // item type, so add / remove / count are all O(1) average — no walking a
    // list, and wood picked up ten times is one entry with a count of 10.
    //
    // Invariants:
    //  * The dictionary is private; a type is present only while its quantity
    //    is > 0 (reaching 0 removes the key).
    //  * Dictionary enumeration order is NOT reliable after removals, so the
    //    inventory never enumerates the dictionary for display. GetItems walks
    //    ItemCatalog (fixed definition order) and looks each type up instead.
    //
    // Display order: by default fixed ItemCatalog order within each category.
    // SortByValue() switches to "highest market value first" within each
    // category (InsertionSortUtil.SortDescending, stable, so ties keep catalog
    // order). That sorted order lasts until the next inventory change (any
    // successful Add / TryRemove / TrySpend) or until ResetOrder() — the 'I'
    // key toggles between the two. Quantities are never affected.
    public class Inventory
    {
        private readonly Dictionary<string, int> _counts = new(StringComparer.Ordinal);
        private bool _sortedByValue;

        public bool IsSortedByValue => _sortedByValue;

        // Adds qty (> 0) of an item type, stacking onto what is already held.
        // Throws for an undefined item type or a non-positive quantity.
        public void Add(string type, int qty = 1)
        {
            if (qty <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(qty), qty, "Quantity must be positive.");
            }
            ItemCatalog.Get(type);

            _counts[type] = Count(type) + qty;
            _sortedByValue = false;
        }

        // All-or-nothing: removes qty if at least that many are held, deleting
        // the key when it reaches zero. Returns false and changes nothing otherwise.
        public bool TryRemove(string type, int qty = 1)
        {
            if (qty <= 0 || Count(type) < qty)
            {
                return false;
            }

            int remaining = _counts[type] - qty;
            if (remaining == 0)
            {
                _counts.Remove(type);
            }
            else
            {
                _counts[type] = remaining;
            }
            _sortedByValue = false;
            return true;
        }

        public int Count(string type) => _counts.TryGetValue(type, out int count) ? count : 0;

        public bool Has(string type, int qty = 1) => qty > 0 && Count(type) >= qty;

        // Atomic payment: every cost entry is checked first (entries for the
        // same type are added together), and only if all can be paid is
        // anything deducted. A failed craft or trade changes nothing.
        public bool TrySpend(IEnumerable<ItemStack> cost)
        {
            var totals = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (ItemStack entry in cost)
            {
                if (entry.Quantity < 0)
                {
                    return false;
                }
                if (entry.Quantity == 0)
                {
                    continue;
                }
                totals[entry.Type] = totals.TryGetValue(entry.Type, out int sum) ? sum + entry.Quantity : entry.Quantity;
            }

            foreach (KeyValuePair<string, int> total in totals)
            {
                if (Count(total.Key) < total.Value)
                {
                    return false;
                }
            }

            foreach (KeyValuePair<string, int> total in totals)
            {
                TryRemove(total.Key, total.Value);   // cannot fail: checked above
            }
            return true;
        }

        // Owned items (qty > 0) of one category, in display order (see class
        // comment). Returns a snapshot, so callers may change the inventory
        // while looping over it.
        public IEnumerable<(string type, int qty)> GetItems(ItemCategory category)
        {
            var items = new List<(string type, int qty)>();
            foreach (ItemDefinition definition in ItemCatalog.InCategory(category))
            {
                int qty = Count(definition.Name);
                if (qty > 0)
                {
                    items.Add((definition.Name, qty));
                }
            }

            if (_sortedByValue)
            {
                InsertionSortUtil.SortDescending(items, entry => ItemCatalog.ValueOf(entry.type));
            }
            return items;
        }

        // Empties the bag (a replay starts with nothing).
        public void Clear()
        {
            _counts.Clear();
            _sortedByValue = false;
        }

        // Switches display to highest-market-value-first within each category.
        public void SortByValue() => _sortedByValue = true;

        // Back to the fixed catalog order.
        public void ResetOrder() => _sortedByValue = false;

        // Snapshot of every owned stack, highest value first (ties in catalog
        // order). Independent of the display-order mode. Reuses
        // InsertionSortUtil — the same sort the leaderboard uses.
        public List<Item> GetSortedByValue()
        {
            var snapshot = new List<Item>();
            foreach (ItemCategory category in ItemCatalog.Categories)
            {
                foreach (ItemDefinition definition in ItemCatalog.InCategory(category))
                {
                    int qty = Count(definition.Name);
                    if (qty > 0)
                    {
                        snapshot.Add(new Item(definition.Name, qty));
                    }
                }
            }

            InsertionSortUtil.SortDescending(snapshot, item => item.Value);
            return snapshot;
        }
    }
}