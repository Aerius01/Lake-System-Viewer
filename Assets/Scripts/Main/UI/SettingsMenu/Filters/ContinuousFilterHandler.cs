using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using System;

public class ContinuousFilterHandler : MonoBehaviour
{
    [SerializeField] private bool length;
    [SerializeField] private Button applyButton;
    [SerializeField] private TwoEndedSlider slider;
    private float rangeMin = float.MaxValue, rangeMax = float.MinValue;

    private void Start()
    {
        Main.fishDictAssembled += this.GetScales;
    }

    private void GetScales()
    {
        foreach (var key in FishManager.fishDict.Keys)
        {
            if (length)
            {
                if (FishManager.fishDict[key].length != null)
                {
                    rangeMin = Mathf.Min((float)FishManager.fishDict[key].length, rangeMin);
                    rangeMax = Mathf.Max((float)FishManager.fishDict[key].length, rangeMax);
                }
            }
            else
            {
                if (FishManager.fishDict[key].weight != null)
                {
                    rangeMin = Mathf.Min((float)FishManager.fishDict[key].weight, rangeMin);
                    rangeMax = Mathf.Max((float)FishManager.fishDict[key].weight, rangeMax);
                }
            }
        }

        slider.SetRange(rangeMin, rangeMax);
    }

    public void ApplyFilter()
    {
        FilterManager.AddContFilter(new ContinuousFilter(slider.currentMax, slider.currentMin, slider.inverted, this.length));
    }

    public void ClearFilter()
    {
        FilterManager.ClearFilters(typeof(ContinuousFilter), this.length);
    }
}