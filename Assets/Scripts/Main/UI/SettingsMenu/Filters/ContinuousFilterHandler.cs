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
    [SerializeField] private TextMeshProUGUI header;
    
    private float rangeMin = float.MaxValue, rangeMax = float.MinValue;
    private int counter = 0;

    private void Awake()
    {
        Main.fishDictAssembled += this.GetScales;
    }

    private void Start()
    {
        if (this.length) { this.header.text =  "Length";}
        else { this.header.text = "Weight";}
    }

    private void GetScales()
    {
        foreach (var key in FishManager.fishDict.Keys)
        {
            if (this.length)
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
        counter += 1;

        if (this.length) { this.header.text =  string.Format("Length ({0})", counter); }
        else { this.header.text = string.Format("Weight ({0})", counter); }
    }

    public void ClearFilter()
    {
        this.counter = 0;
        FilterManager.ClearFilters(typeof(ContinuousFilter), this.length);

        if (this.length) { this.header.text =  "Length"; }
        else { this.header.text = "Weight"; }
    }
}