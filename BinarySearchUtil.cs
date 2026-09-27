using System;
using System.Collections.Generic;

namespace TileQuest
{
    // DSA note: Binary Search piece. Repeatedly halves the search range
    // instead of scanning linearly — O(log n) instead of O(n) — but only
    // works because the input is sorted ascending by the key being searched.
    // Intended for RecipeBook.SortedByCost (or any similarly sorted shop/
    // recipe list): looking up "can I afford exactly this recipe?" or
    // finding a recipe by its known cost.
    public static class BinarySearchUtil
    {
        // Returns the index of the first item whose key matches targetKey,
        // or -1 if none does. sortedItems must already be sorted ascending
        // by keySelector — this does not sort for you.
        public static int FindByKey<T>(IReadOnlyList<T> sortedItems, int targetKey, Func<T, int> keySelector)
        {
            int low = 0;
            int high = sortedItems.Count - 1;

            while (low <= high)
            {
                int mid = low + (high - low) / 2; // avoids overflow vs (low + high) / 2
                int midKey = keySelector(sortedItems[mid]);

                if (midKey == targetKey)
                {
                    return mid;
                }
                if (midKey < targetKey)
                {
                    low = mid + 1;
                }
                else
                {
                    high = mid - 1;
                }
            }

            return -1;
        }
    }
}
