using System.Collections.Generic;
using System.Linq;
using System;

public static class Tools
{
    private static Random random = new Random();

    public static List<List<T>> ChunkList<T>(IEnumerable<T> data, int size)
    {
        // https://www.programing.io/c-chunk-list-or-enumerable-to-smaller-list-of-lists.html
        return data
        .Select((x, i) => new { Index = i, Value = x })
        .GroupBy(x => x.Index / size)
        .Select(x => x.Select(v => v.Value).ToList())
        .ToList();
    }

    public static string RandomString(int length)
    {
        const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789";
        return new string(Enumerable.Repeat(chars, length)
            .Select(s => s[random.Next(s.Length)]).ToArray());
    }
}