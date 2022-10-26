using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class ContinuousFilterHandler : MonoBehaviour
{
    [SerializeField] private bool _isLengthHandler;
    [SerializeField] private TwoEndedSlider slider;
    [SerializeField] private TextMeshProUGUI header;

    public bool isLengthHandler { get { return _isLengthHandler; } }
    private int counter = 0;
    private List<ContinuousFilter> filterList;

    private void Awake()
    {
        Main.fishDictAssembled += this.SetScales;
        filterList = new List<ContinuousFilter>();
        FilterManager.AddContHandler(this);
    }

    private void Start()
    {
        if (this.isLengthHandler) { this.header.text =  "Length";}
        else { this.header.text = "Weight";}
    }

    private void SetScales()
    {
        if (this.isLengthHandler) slider.SetRange(FishManager.minLength, FishManager.maxLength);
        else slider.SetRange(FishManager.minWeight, FishManager.maxWeight);
    }

    public bool PassesFilters(Fish fish)
    {
        foreach (ContinuousFilter filter in filterList)
        {
            if (filter.PassesFilter(fish)) { continue; }
            else return false;
        }
        return true;
    }

    // Called by in-game button
    public void ApplyFilter()
    {
        filterList.Add(new ContinuousFilter(slider.currentMax, slider.currentMin, slider.inverted, this.isLengthHandler));
        counter += 1;

        if (this.isLengthHandler) { this.header.text =  string.Format("Length ({0})", counter); }
        else { this.header.text = string.Format("Weight ({0})", counter); }
    }

    // Called by in-game button
    public void ClearFilter()
    {
        this.counter = 0;
        FilterBar.instance.DeleteCont(this.isLengthHandler);
        filterList = new List<ContinuousFilter>();

        if (this.isLengthHandler) { this.header.text =  "Length"; }
        else { this.header.text = "Weight"; }
    }
}