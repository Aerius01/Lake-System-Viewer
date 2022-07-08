using UnityEngine;
using System;
using TMPro;

public class CategoricalFilterTile : MonoBehaviour
{
    public CategoricalFilter filter {get; private set;}
    [SerializeField] private TextMeshProUGUI filterText;

    public void SetFilter(CategoricalFilter filter)
    {
        this.filter = filter;

        String descriptor = "";
        if (filter.sex) {descriptor = "SEX";}
        else {descriptor = "CT";}

        filterText.text = string.Format("{0} IN \"{1}\"", descriptor, string.Join( ", ", filter.validOptions));
    }
}