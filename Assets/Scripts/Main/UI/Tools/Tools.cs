using System.Collections.Generic;
using System.Linq;

public static class Tools
{
    public static List<List<T>> ChunkList<T>(IEnumerable<T> data, int size)
    {
        // https://www.programing.io/c-chunk-list-or-enumerable-to-smaller-list-of-lists.html
        return data
        .Select((x, i) => new { Index = i, Value = x })
        .GroupBy(x => x.Index / size)
        .Select(x => x.Select(v => v.Value).ToList())
        .ToList();
    }
}