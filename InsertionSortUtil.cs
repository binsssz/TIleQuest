using System;
using System.Collections.Generic;

namespace TileQuest
{
    // DSA note: Insertion Sort piece. Builds up a sorted result one element
    // at a time by sliding each new element left into its correct position
    // among the already-sorted elements before it. O(n^2) worst case, but
    // simple and genuinely efficient for the small lists this project uses
    // it on (an inventory, a handful of leaderboard entries) — especially
    // when re-sorting after just one new entry is added, since the rest of
    // the list is already sorted and insertion sort does very little work
    // in that case.
    public static class InsertionSortUtil
    {
        // Sorts in place, descending by the given numeric key (highest
        // first) — e.g. highest score first on the leaderboard, or highest
        // value first in an inventory-by-value view.
        public static void SortDescending<T>(IList<T> items, Func<T, int> keySelector)
        {
            for (int i = 1; i < items.Count; i++)
            {
                T current = items[i];
                int currentKey = keySelector(current);
                int j = i - 1;

                while (j >= 0 && keySelector(items[j]) < currentKey)
                {
                    items[j + 1] = items[j];
                    j--;
                }
                items[j + 1] = current;
            }
        }
    }
}
