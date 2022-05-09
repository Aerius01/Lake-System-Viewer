using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using TMPro;

public class SpeciesBox : ListBox
{
    // Game objects and components
    [SerializeField] private GameObject fishBoxes, countField;

    public List<FishBox> components {get; private set;}
    private int currentRank = 0;
    public string speciesName {get; private set;}

    public float contentSize
    {
        get
        {
            float totalSize = 0f;
            foreach (FishBox box in components) { totalSize += box.open ? box.contentSize : 30f; }
            totalSize += 298f;
            return totalSize;
        }
    }

    // METHODS
    public void SetUpBox(string name, int rank)
    {
        this.speciesName = name;
        this.rank = rank;

        this.headerText = this.transform.Find("Header").transform.Find("SpeciesName").GetComponent<TextMeshProUGUI>();
        headerText.text = name;

        components = new List<FishBox>();
        this.rect = this.GetComponent<RectTransform>();
        this.parentRect = this.transform.parent.GetComponent<RectTransform>();
    }

    public void AddFish(Fish fish, FishBox box)
    {
        // rank also serves as counter of individuals
        box.SetRank(this.currentRank);
        this.currentRank += 1;
        this.components.Add(box);
        box.transform.SetParent(fishBoxes.transform, worldPositionStays: false);
        box.SetUpBox(fish);

        countField.GetComponent<TextMeshProUGUI>().text = string.Format("Count: {0}", this.currentRank);
    }

    private void OpenCloseBox()
    {
        // Only register the toggle if the box isn't currently mid-animation
        if (this.opening == null)
        {
            if (this.open) this.opening = false;
            else this.opening = true;

            StartCoroutine(AnimateChange(60f, this.contentSize));
        }
    }
            
    public void ExpandMenu(float size)
    {
        this.rect.sizeDelta += new Vector2(0, size);
        LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
    }

    public void Activate()
    { 
        foreach (FishBox box in components) { box.Activate(this.activeToggle.isOn); }
        // grey out gameobject so toggle is still selectable and components interactable
    }

    public void SetSpeciesColor()
    {
        FlexibleColorPickerUtils.SetNewColor();
        foreach (FishBox box in components) { if (!box.greyedOut) { FlexibleColorPickerUtils.colorAcceptedEvent += box.SetIndividualColor; } }
    }

    public void RemoveSpeciesColor() { foreach (FishBox box in components) { box.RemoveIndividualColor(); } }
    public void ActivateTags() { foreach (FishBox box in components) { if (box.tagStatus != this.tagToggle.isOn) box.ToggleTag(this.tagToggle.isOn); box.ActivateTag(); } }
    public void ActivateDepths() { foreach (FishBox box in components) { if (box.depthStatus != this.depthToggle.isOn) box.ToggleDepthLine(this.depthToggle.isOn); box.ActivateDepthLine(); } }
    public void ActivateTrails() { foreach (FishBox box in components) { if (box.trailStatus != this.trailToggle.isOn) box.ToggleTrail(this.trailToggle.isOn); box.ActivateTrail(); } }
}