using System;
using System.Collections.Generic;
using System.Linq;

namespace VirtoCommerce.Platform.Core.Common
{
    public static class EnumerableExtensions
    {
        /// <summary>Indicates whether the specified enumerable is null or has a length of zero.</summary>
        /// <param name="data">The data to test.</param>
        /// <returns>true if the array parameter is null or has a length of zero; otherwise, false.</returns>
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> data)
        {
            return data == null || !data.Any();
        }

        public static bool ContainsIgnoreCase(this IEnumerable<string> values, string value)
        {
            return values.Contains(value, StringComparer.OrdinalIgnoreCase);
        }

        public static IEnumerable<string> DistinctIgnoreCase(this IEnumerable<string> values)
        {
            return values.Distinct(StringComparer.OrdinalIgnoreCase);
        }

        public static IEnumerable<IList<T>> Paginate<T>(this IEnumerable<T> items, int pageSize)
        {
            var page = new List<T>();

            foreach (var item in items ?? [])
            {
                page.Add(item);
                if (page.Count >= pageSize)
                {
                    yield return page;
                    page = new List<T>();
                }
            }

            if (page.Count > 0)
            {
                yield return page;
            }
        }

        /// <summary>
        /// Performs the indicated action on each item.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="action">The action to be performed.</param>
        /// <remarks>If an exception occurs, the action will not be performed on the remaining items.</remarks>
        public static void Apply<T>(this IEnumerable<T> items, Action<T> action)
        {
            foreach (var item in items ?? [])
            {
                action(item);
            }
        }

        /// <summary>
        /// Performs the indicated action on each item. Boxing free for <c>List+Enumerator{T}</c>.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="action">The action to be performed.</param>
        /// <remarks>If an exception occurs, the action will not be performed on the remaining items.</remarks>
        public static void Apply<T>(this List<T> items, Action<T> action)
        {
            foreach (var item in items ?? [])
            {
                action(item);
            }
        }

        /// <summary>
        /// Performs the indicated action on each key-value pair.
        /// </summary>
        /// <param name="items"></param>
        /// <param name="action">The action to be performed.</param>
        /// <remarks>If an exception occurs, the action will not be performed on the remaining items.</remarks>
        public static void Apply(this System.Collections.IDictionary items, Action<object, object> action)
        {
            if (items is null)
            {
                return;
            }

            foreach (var key in items.Keys)
            {
                action(key, items[key]);
            }
        }

        // PT-1663: Add IComparable constraint
        public static int GetOrderIndependentHashCode<T>(this IEnumerable<T> source)
        {
            var hash = 0;

            //Need to force order to get  order independent hash code
            foreach (var element in source.OrderBy(x => x, Comparer<T>.Default))
            {
                hash ^= EqualityComparer<T>.Default.GetHashCode(element);
            }

            return hash;
        }

        public static IDictionary<TKey, TValue> ToIDictionary<TKey, TValue>(this IEnumerable<TValue> source, Func<TValue, TKey> keySelector)
        {
            return source.ToDictionary(keySelector);
        }

        public static IDictionary<TKey, TValue> ToIDictionary<TKey, TValue>(this IEnumerable<TValue> source, Func<TValue, TKey> keySelector, IEqualityComparer<TKey> comparer)
        {
            return source.ToDictionary(keySelector, comparer);
        }
    }
}
