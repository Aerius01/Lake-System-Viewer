using System.Collections.Generic;
using System;
using UnityEngine;

public class FilterManager : MonoBehaviour
{
    public static ContinuousFilterHandler lengthFilterHandler { get; private set; }
    public static ContinuousFilterHandler weightFilterHandler { get; private set; }
    public static CategoricalFilterHandler sexFilterHandler { get; private set; }
    public static CategoricalFilterHandler captureTypeFilterHandler { get; private set; }

    public static void AddContHandler(ContinuousFilterHandler handler)
    {
        if (handler.isLengthHandler) lengthFilterHandler = handler;
        else weightFilterHandler = handler;
    }

    public static void AddCatHandler(CategoricalFilterHandler handler)
    {
        if (handler.isSexHandler) sexFilterHandler = handler;
        else captureTypeFilterHandler = handler;
    }








    // public static void ClearContFilter(bool isLengthFilter)
    // {
    //     for (int i = continuousFilters.Count; i >= 0; i--)
    //     {
    //         ContinuousFilter filter = continuousFilters[i];
    //         if (isLengthFilter == filter.isLengthFilter)
    //         {
    //             continuousFilters.RemoveAt(continuousFilters.IndexOf(filter));
    //             FilterBar.instance.RemoveCont(filter);
    //             filter.SelfDestruct();
    //         }
    //     }
    // }

    // public static void ClearCatFilter(bool isSexFilter)
    // {
    //     for (int i = categoricalFilters.Count; i >= 0; i--)
    //     {
    //         CategoricalFilter filter = categoricalFilters[i];
    //         if (isSexFilter == filter.isSexFilter)
    //         {
    //             categoricalFilters.RemoveAt(categoricalFilters.IndexOf(filter));
    //             FilterBar.instance.RemoveCat(filter);
    //             filter.SelfDestruct();
    //         }
    //     }
    // }

    // public static void ClearFilters(Type type, bool element)
    // {
    //     if (type == typeof(ContinuousFilter))
    //     {
    //         for (int i = continuousFilters.Count - 1; i >= 0; i--)
    //         {
    //             if (continuousFilters[i].length == element)
    //             {
    //                 ContinuousFilter filter = continuousFilters[i];
    //                 FilterBar.instance.RemoveCont(filter.tile);
    //                 continuousFilters.RemoveAt(i);
    //             }
    //         }
    //     }
    //     else
    //     {
    //         for (int i = categoricalFilters.Count - 1; i >= 0; i--)
    //         {
    //             if (categoricalFilters[i].sex == element)
    //             {
    //                 CategoricalFilter filter = categoricalFilters[i];
    //                 FilterBar.instance.RemoveCat(filter.tile);
    //                 categoricalFilters.RemoveAt(i);
    //             }
    //         }
    //     }
    // }

    public static bool PassesAllFilters(Fish fish)
    {
        // Running check of all filters
        return lengthFilterHandler.PassesFilters(fish) ? 
        (
            weightFilterHandler.PassesFilters(fish) ? 
            (
                sexFilterHandler.PassesFilters(fish) ? captureTypeFilterHandler.PassesFilters(fish) : false
            )
            : false
        )
        : false;
    }
}