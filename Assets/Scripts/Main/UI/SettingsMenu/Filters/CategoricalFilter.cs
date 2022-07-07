using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;

public abstract class Filter { public abstract bool PassesFilter(Fish fish); }
 
public class CategoricalFilter : Filter
{
    public bool sex { get; private set;}
    private List<string> validOptions;

    public CategoricalFilter(List<string> validOptions, bool sex)
    {
        this.validOptions = validOptions;
        this.sex = sex;
    }

    public override bool PassesFilter(Fish fish)
    {
        if (this.sex)
        {
            string comparer = fish.male == true ? "M" : fish.male == false ? "F" : null;
            if (validOptions.Any(s => s.Contains(comparer))) { return true; }
        }
        else { if (validOptions.Any(s => s.Contains(fish.captureType))) { return true; } } 
        return false;
    }
}

public class CategoricalFilterHandler
{
    [SerializeField] private bool sex;
    private List<Toggle> toggles;

    public void ApplyFilter()
    {
        List<string> chosenOptions = new List<string>();
        foreach (Toggle toggle in toggles)
        {
            if (toggle.isOn)
            {
                chosenOptions.Add(toggle.transform.Find("Label").GetComponent<TextMeshProUGUI>().text);
            }
        }

        FilterManager.AddCatFilter(new CategoricalFilter(chosenOptions, this.sex));
    }
}