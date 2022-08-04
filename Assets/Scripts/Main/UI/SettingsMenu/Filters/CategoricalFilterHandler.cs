using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System.Linq;
using System;

public class CategoricalFilterHandler : MonoBehaviour
{
    [SerializeField] private bool sex;
    [SerializeField] private TextMeshProUGUI header;
    [SerializeField] private GameObject togglePrefab;
    [SerializeField] private RectTransform contentPanel;

    private List<Toggle> toggles;
    private int counter = 0;

    private void Awake()
    {
        Main.fishDictAssembled += this.GetOptions;
    }

    private void Start()
    {
        if (this.sex) { this.header.text = "Sex"; }
        else { this.header.text = "Capture Type"; }

        contentPanel.position = new Vector3(0,0,0);
    }

    private void GetOptions()
    {
        List<String> optionsList = new List<String>();

        if (this.sex) { optionsList = new List<String>() {"Undefined", "Male", "Female"}; }
        else
        {
            foreach (var key in FishManager.fishDict.Keys)
            {
                string newString = FishManager.fishDict[key].captureType;
                if (String.IsNullOrEmpty(newString)) { newString = "Undefined"; }

                if (!optionsList.Any(s => s.Contains(newString)))
                { optionsList.Add(newString); }
            }
        }

        // Populate the toggle list with discovered options
        int toggleCount = 0;
        toggles = new List<Toggle>();
        foreach (String item in optionsList)
        {
            toggleCount += 1;
            Transform rootTransform = this.transform.Find("Body").transform.Find("ToggleGroup");

            GameObject newToggle = Instantiate (togglePrefab, transform.position, transform.rotation) as GameObject;
            newToggle.transform.SetParent(rootTransform, false);
            newToggle.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = item;

            toggles.Add(newToggle.GetComponent<Toggle>());
        }

        RectTransform rect = this.GetComponent<RectTransform>();
        float diff = (75 + toggleCount * 35 + (toggleCount - 1) * 10 + 30 + 15) - rect.rect.height;
        rect.sizeDelta += new Vector2(0, diff);
    }

    public void ApplyFilter()
    {
        this.counter += 1;

        List<string> chosenOptions = new List<string>();
        foreach (Toggle toggle in toggles)
        {
            if (toggle.isOn) { chosenOptions.Add(toggle.transform.Find("Text").GetComponent<TextMeshProUGUI>().text); }
        }

        if (this.sex) { this.header.text =  string.Format("Sex ({0})", counter); }
        else { this.header.text = string.Format("Capture Type ({0})", counter); }

        FilterManager.AddCatFilter(new CategoricalFilter(chosenOptions, this.sex));
    }

    public void ClearFilter()
    {
        this.counter = 0;

        FilterManager.ClearFilters(typeof(CategoricalFilter), this.sex);

        if (this.sex) { this.header.text = "Sex"; }
        else { this.header.text = "Capture Type"; }
    }
}