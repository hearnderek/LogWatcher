using System.Collections.Generic;
using System.Linq;

namespace LogWatcher
{
    public static class EnumerableExtensions
    {
        public static IEnumerable<T> Tail<T>(this IEnumerable<T> source, int count)
        {
            return source.Reverse().Take(count).Reverse();
        }
    }
}
