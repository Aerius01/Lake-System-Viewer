using System.Collections.Generic;
using System.Linq;
using System;

public abstract class Filter { public abstract bool PassesFilter(Fish fish); }
 
public class CategoricalFilter : Filter
{
    public bool isSexFilter { get; private set;}
    public List<string> validOptions { get; private set;}

    public CategoricalFilter(List<string> validOptions, bool isSexFilter)
    {
        this.validOptions = validOptions;
        this.isSexFilter = isSexFilter;

        FilterBar.instance.AddCat(this);
    }

    public override bool PassesFilter(Fish fish)
    {
        if (this.isSexFilter)
        {
            string comparer = fish.male == true ? "Male" : fish.male == false ? "Female" : "Undefined";
            if (validOptions.Any(s => s.Contains(comparer))) { return true; }
        }
        else
        {
            string comparer = String.IsNullOrEmpty(fish.captureType) ? "Undefined" : fish.captureType;
            if (validOptions.Any(s => s.Contains(comparer))) { return true; }
        }

        return false;
    }
}

