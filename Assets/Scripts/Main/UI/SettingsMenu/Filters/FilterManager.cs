using System.Collections.Generic;
using System;

public static class FilterManager 
{
    public static List<ContinuousFilter> continuousFilters;
    public static List<CategoricalFilter> categoricalFilters;

    public static void AddContFilter(ContinuousFilter filter) { continuousFilters.Add(filter); }
    public static void AddCatFilter(CategoricalFilter filter) { categoricalFilters.Add(filter); }

    public static void ClearFilters(Type type, bool element)
    {
        if (type == typeof(ContinuousFilter))
        {
            for (int i = continuousFilters.Count - 1; i >= 0; i--) { if (continuousFilters[i].length == element) { continuousFilters.RemoveAt(i); } }
        }
        else
        {
            for (int i = categoricalFilters.Count - 1; i >= 0; i--) { if (categoricalFilters[i].sex == element) { categoricalFilters.RemoveAt(i); } }
        }

        RunFilters();
    }

    public static void RunFilters() {;}
}