using UnityEngine;
using TMPro;
using System.Collections.Generic;
using UnityEngine.UI;
using System;

public class CategoricalFilterHandler : MonoBehaviour
{
    [SerializeField] private bool _isSexHandler;
    [SerializeField] private TextMeshProUGUI header;
    [SerializeField] private GameObject togglePrefab;
    [SerializeField] private RectTransform contentPanel;

    public float contentSize { get { return this.gameObject.GetComponent<RectTransform>().rect.height; } }

    private List<Toggle> toggles;
    private int counter = 0;
    private float initialStartSize;
    public bool isSexHandler { get { return _isSexHandler; } }
    private List<CategoricalFilter> filterList;


    private void Awake()
    {
        filterList = new List<CategoricalFilter>();
        FilterManager.AddCatHandler(this);

        List<string> optionsList = isSexHandler ? FishManager.listOfSexes : FishManager.listOfCaptureTypes;

        // Populate the toggle list with discovered options
        int toggleCount = 0;
        toggles = new List<Toggle>();
        foreach (String item in optionsList)
        {
            toggleCount += 1;
            Transform rootTransform = this.transform.Find("Body").transform.Find("ToggleGroup");

            GameObject newToggle = Instantiate (togglePrefab, transform.position, transform.rotation) as GameObject;
            newToggle.transform.SetParent(rootTransform, false);

            TextMeshProUGUI textObject = newToggle.transform.Find("Text").GetComponent<TextMeshProUGUI>();
            textObject.enableWordWrapping = false;
            textObject.overflowMode = TextOverflowModes.Ellipsis;
            textObject.text = item;

            toggles.Add(newToggle.GetComponent<Toggle>());
        }

        RectTransform rect = this.GetComponent<RectTransform>();
        float diff = (75 + toggleCount * 35 + (toggleCount - 1) * 10 + 30 + 15) - rect.rect.height;
        rect.sizeDelta += new Vector2(0, diff);

        this.contentPanel.sizeDelta += new Vector2(0, this.contentSize - this.initialStartSize);
        LayoutRebuilder.ForceRebuildLayoutImmediate(this.contentPanel);
    }

    private void Start()
    {
        if (this.isSexHandler) { this.header.text = "Sex"; }
        else { this.header.text = "Capture Type"; }

        contentPanel.position = new Vector3(0,0,0);
        initialStartSize = this.contentSize;
    }

    public bool PassesFilters(Fish fish)
    {
        foreach (CategoricalFilter filter in filterList)
        {
            if (filter.PassesFilter(fish)) { continue; }
            else return false;
        }
        return true;
    }

    // Called by in-game button
    public void ApplyFilter()
    {
        this.counter += 1;

        List<string> chosenOptions = new List<string>();
        foreach (Toggle toggle in toggles) { if (toggle.isOn) { chosenOptions.Add(toggle.transform.Find("Text").GetComponent<TextMeshProUGUI>().text); } }

        if (this.isSexHandler) { this.header.text =  string.Format("Sex ({0})", counter); }
        else { this.header.text = string.Format("Capture Type ({0})", counter); }

        filterList.Add(new CategoricalFilter(chosenOptions, this.isSexHandler));
    }

    // Called by in-game button
    public void ClearFilter()
    {
        this.counter = 0;
        FilterBar.instance.DeleteCat(this.isSexHandler);
        filterList = new List<CategoricalFilter>();

        if (this.isSexHandler) { this.header.text = "Sex"; }
        else { this.header.text = "Capture Type"; }
    }
}