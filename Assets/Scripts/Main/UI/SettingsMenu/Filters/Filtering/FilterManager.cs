using UnityEngine;

public class FilterManager : MonoBehaviour
{
    public static ContinuousFilterHandler lengthFilterHandler { get; private set; }
    public static ContinuousFilterHandler weightFilterHandler { get; private set; }
    public static CategoricalFilterHandler sexFilterHandler { get; private set; }
    public static CategoricalFilterHandler captureTypeFilterHandler { get; private set; }

    [SerializeField] Transform contentTransform;

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

    public static bool PassesAllFilters(Fish fish)
    {
        // Running check of all active filters
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