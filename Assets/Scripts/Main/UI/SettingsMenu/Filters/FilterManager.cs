using System.Collections.Generic;
using System;

public static class FilterManager 
{
    public static List<ContinuousFilter> continuousFilters;
    public static List<CategoricalFilter> categoricalFilters;

    static FilterManager()
    {
        continuousFilters = new List<ContinuousFilter>();
        categoricalFilters = new List<CategoricalFilter>();
    }

    public static void AddContFilter(ContinuousFilter filter) { continuousFilters.Add(filter); }
    public static void AddCatFilter(CategoricalFilter filter) { categoricalFilters.Add(filter); }

    public static void ClearFilters(Type type, bool element)
    {
        if (type == typeof(ContinuousFilter))
        {
            for (int i = continuousFilters.Count - 1; i >= 0; i--)
            {
                if (continuousFilters[i].length == element)
                {
                    ContinuousFilter filter = continuousFilters[i];
                    FiltersBar.instance.RemoveCont(filter.tile);
                    continuousFilters.RemoveAt(i);
                }
            }
        }
        else
        {
            for (int i = categoricalFilters.Count - 1; i >= 0; i--)
            {
                if (categoricalFilters[i].sex == element)
                {
                    CategoricalFilter filter = categoricalFilters[i];
                    FiltersBar.instance.RemoveCat(filter.tile);
                    categoricalFilters.RemoveAt(i);
                }
            }
        }
    }

    public static bool PassesAllFilters(Fish fish)
    {
        foreach (ContinuousFilter filter in continuousFilters)
        {
            if (filter.PassesFilter(fish)) { continue; }
            else return false;
        }

        foreach (CategoricalFilter filter in categoricalFilters)
        {
            if (filter.PassesFilter(fish)) { continue; }
            else return false;
        }

        return true;
    }
}