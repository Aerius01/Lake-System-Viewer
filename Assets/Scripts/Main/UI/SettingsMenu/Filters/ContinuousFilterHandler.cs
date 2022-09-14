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
    
    private int counter = 0;

    private void Awake()
    {
        Main.fishDictAssembled += this.SetScales;
    }

    private void Start()
    {
        if (this.length) { this.header.text =  "Length";}
        else { this.header.text = "Weight";}
    }

    private void SetScales()
    {
        if (this.length) slider.SetRange(FishManager.minLength, FishManager.maxLength);
        else slider.SetRange(FishManager.minWeight, FishManager.maxWeight);
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