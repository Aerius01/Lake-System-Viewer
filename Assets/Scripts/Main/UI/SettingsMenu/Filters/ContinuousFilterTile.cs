using UnityEngine;
using System;
using TMPro;

public class ContinuousFilterTile : MonoBehaviour
{
    public ContinuousFilter filter {get; private set;}
    [SerializeField] private TextMeshProUGUI filterText;

    public void SetFilter(ContinuousFilter filter)
    {
        this.filter = filter;

        String descriptor = "";
        if (filter.length) {descriptor = "LEN";}
        else {descriptor = "WGT";}

        if (filter.inverted) { filterText.text = string.Format("{0} <= {1:###} && {2} >= {3:###}", descriptor, filter.minVal, descriptor, filter.maxVal); }
        else { filterText.text = string.Format("{0:###} <= {1} <= {2:###}", filter.minVal, descriptor, filter.maxVal); }
    }
}