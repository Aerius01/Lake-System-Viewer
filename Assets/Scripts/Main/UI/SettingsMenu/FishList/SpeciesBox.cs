using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;
using System.Linq;

public class SpeciesBox : ListBox
{
    // Game objects and components
    [SerializeField] private GameObject fishBoxes, countField;
    [SerializeField] private GameObject colorButton, removeButton;
    [SerializeField] private GameObject contentWindow;

    public List<FishBox> components {get; private set;}
    private int totalIndividuals = 0, activeIndividuals = 0;
    public string speciesName {get; private set;}

    public float contentSize
    {
        get
        {
            float totalSize = 0f;
            foreach (FishBox box in components) { totalSize += box.open ? box.contentSize : 40f; }
            totalSize += 298f;
            return totalSize;
        }
    }

    public void Clear()
    {
        foreach (FishBox fishBox in components) { Destroy(fishBox.gameObject); }
        Destroy(this.gameObject);
    }

    public void ReorderBoxes()
    {
        List<FishBox> activeObjects = new List<FishBox>();
        List<FishBox> inactiveObjects = new List<FishBox>();

        foreach (FishBox fishBox in this.components)
        {
            if (!fishBox.greyedOut) activeObjects.Add(fishBox);
            else inactiveObjects.Add(fishBox);
        }

        List<FishBox> sortedActive = activeObjects.OrderBy(o=>o.fish.id).ToList();
        List<FishBox> sortedInactive = inactiveObjects.OrderBy(o=>o.fish.id).ToList();

        int index = 0;
        foreach (FishBox fishBox in sortedActive)
        {
            fishBox.transform.SetSiblingIndex(index);
            index++;
        }
        foreach (FishBox fishBox in sortedInactive)
        {
            fishBox.transform.SetSiblingIndex(index);
            index++;
        }

        this.RecountIndividuals();
    }

    // METHODS
    public void SetUpBox(string name)
    {
        this.speciesName = name;

        this.headerText = this.transform.Find("Canvas").transform.Find("Header").transform.Find("SpeciesName").GetComponent<TextMeshProUGUI>();
        this.headerText.text = name;

        components = new List<FishBox>();
        this.rect = this.GetComponent<RectTransform>();
        this.parentRect = this.transform.parent.GetComponent<RectTransform>();

        this.contentWindow.SetActive(false);
    }

    public void AddFish(Fish fish, FishBox box)
    {
        // rank also serves as counter of individuals
        this.totalIndividuals += 1;
        this.components.Add(box);
        box.transform.SetParent(fishBoxes.transform, worldPositionStays: false);
        box.SetUpBox(fish);

        countField.GetComponent<TextMeshProUGUI>().text = string.Format("Count: {0}", this.totalIndividuals);
    }

    public void OpenCloseBox()
    {
        // Only register the toggle if the box isn't currently mid-animation
        if (this.opening == null)
        {
            if (this.open) this.opening = false;
            else this.opening = true;

            StartCoroutine(AnimateChange(60f, this.contentSize, this.contentWindow));
        }
    }
            
    public void ExpandMenu(float size)
    {
        this.rect.sizeDelta += new Vector2(0, size);
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    }

    public void Activate()
    { 
        this.removeButton.GetComponent<Image>().color = this.colorButton.GetComponent<Image>().color =
            this.activeToggle.isOn ? ColorPicker.standardButtonColor : ColorPicker.disabledColor;
        this.removeButton.GetComponent<Button>().interactable = this.colorButton.GetComponent<Button>().interactable =
            this.activeToggle.isOn;

        foreach (FishBox box in components)
        {
            box.Activate(this.activeToggle.isOn);
            this.ToggleInteractable(this.activeToggle.isOn);
        }
    }

    private void RecountIndividuals()
    {
        this.activeIndividuals = 0;
        foreach (FishBox fishBox in this.components) { if (!fishBox.greyedOut) this.activeIndividuals++; }

        countField.GetComponent<TextMeshProUGUI>().text = string.Format("Total: {0}; Active: {1}", this.totalIndividuals, this.activeIndividuals);
        this.headerText.text = string.Format("{0} ({1})", this.speciesName, this.activeIndividuals);
    }

    public void SetSpeciesColor()
    {
        ColorPicker.ShowMenu(true);
        foreach (FishBox box in components) { if (!box.greyedOut) { ColorPicker.colorAcceptedEvent += box.SetIndividualColor; } }
    }


    public void RemoveSpeciesColor() { foreach (FishBox box in components) { box.RemoveIndividualColor(); } }
    public void ActivateTags() { foreach (FishBox box in components) { if (box.tagStatus != this.tagToggle.isOn) box.ToggleTag(this.tagToggle.isOn); box.ActivateTag(); } }
    public void ActivateDepths() { foreach (FishBox box in components) { if (box.depthStatus != this.depthToggle.isOn) box.ToggleDepthLine(this.depthToggle.isOn); box.ActivateDepthLine(); } }
    public void ActivateTrails() { foreach (FishBox box in components) { if (box.trailStatus != this.trailToggle.isOn) box.ToggleTrail(this.trailToggle.isOn); box.ActivateTrail(); } }
}