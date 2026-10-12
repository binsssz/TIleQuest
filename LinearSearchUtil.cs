using System;
using System.Collections.Generic;

namespace TileQuest
{
    public static class LinearSearchUtil
    {
        public static int FindIndex<T>(IReadOnlyList<T> items, Predicate<T> match)
        {
            if (items == null)
            {
                throw new ArgumentNullException(nameof(items));
            }
            if (match == null)
            {
                throw new ArgumentNullException(nameof(match));
            }

            for (int index = 0; index < items.Count; index++)
            {
                if (match(items[index]))
                {
                    return index;
                }
            }
            return -1;
        }

        public static T? Find<T>(IReadOnlyList<T> items, Predicate<T> match)
        {
            int index = FindIndex(items, match);
            return index < 0 ? default : items[index];
        }
    }
}
