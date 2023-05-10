using System;
using System.Collections.Generic;
using System.Linq;

namespace MetaGeek.WiFi.Core.Helpers
{
    public static class CollectionExtentions
    {
        public static void Do<T>(this IEnumerable<T> collection, Action<T> action)
        {
            if (collection == null)
            {
                return;
            }

            action.ThrowIfNull("action");
            foreach (T item in collection)
            {
                action(item);
            }
        }

        public static void Do<T, TE>(this IEnumerable<T> collection, Func<T, TE> func)
        {
            if (collection != null)
            {
                func.ThrowIfNull("action");
                collection.Do(delegate (T e)
                {
                    func(e);
                });
            }
        }

        public static bool IsNullOrEmpty<T>(this IEnumerable<T> collection)
        {
            if (collection != null)
            {
                return !collection.Any();
            }

            return true;
        }
    }
}
