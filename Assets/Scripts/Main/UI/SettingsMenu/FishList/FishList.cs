using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;
using System.Linq;

public class FishList : MonoBehaviour
{
    private bool listPopulated = false;

    private Dictionary<string, SpeciesBox> speciesDict;
    [SerializeField] private GameObject speciesBoxTemplate, fishBoxTemplate, speciesListParent;

    private static FishList _instance;
    [HideInInspector] public static FishList instance {get { return _instance; } set {_instance = value; }}

    public void WakeUp()
    {
        // Destroy duplicate instances
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }

        speciesDict = new Dictionary<string, SpeciesBox>();

        // Populate the list
        foreach (Fish fish in FishManager.fishDict.Values)
        {
            if (speciesDict.ContainsKey(fish.speciesName))
            {
                GameObject obj = (Instantiate (fishBoxTemplate) as GameObject);
                FishBox fishBox = obj.GetComponent<FishBox>();

                // sets parent, rank, and sets up the FishBox
                speciesDict[fish.speciesName].AddFish(fish, fishBox);
            }
            else // create the species box if it doesn't already exist
            {
                GameObject obj = (Instantiate (speciesBoxTemplate) as GameObject);
                obj.transform.SetParent(speciesListParent.transform, worldPositionStays: false);

                SpeciesBox newSpeciesBox = obj.GetComponent<SpeciesBox>();
                newSpeciesBox.SetUpBox(fish.speciesName);
                speciesDict[fish.speciesName] = newSpeciesBox;

                // Add the fish to the new SpeciesBox
                GameObject fishBoxObj = (Instantiate (fishBoxTemplate) as GameObject);
                FishBox fishBox = fishBoxObj.GetComponent<FishBox>();
                speciesDict[fish.speciesName].AddFish(fish, fishBox);
            }
        }

        listPopulated = true;
        this.ChangeGreyouts(FishManager.fishDict.Values.ToList(), TimeManager.instance.currentTime);
    }

    public void Clear()
    {
        this.listPopulated = false;
        if (this.speciesDict != null)
        { 
            foreach (SpeciesBox speciesBox in this.speciesDict.Values) { speciesBox.Clear(); }
            this.speciesDict = null; 
        }
    }

    public void ChangeGreyouts(List<Fish> changedFish, DateTime updateTime)
    {
        if (this.listPopulated)
        {
            List<string> speciesList = new List<string>();
            foreach (Fish fish in changedFish)
            {
                if (!speciesList.Contains(fish.speciesName)) { speciesList.Add(fish.speciesName); }
                FishBox fishBox = this.speciesDict[fish.speciesName].fishBoxes[fish.id];

                if (fish.FishShouldExist(updateTime)) { if (fishBox.greyedOut) { fishBox.RestoreColor(); } }
                else { if (!fishBox.greyedOut) { fishBox.Greyout(); } }
            }

            foreach (string changedSpecies in speciesList) { this.speciesDict[changedSpecies].ReorderBoxes(); }
        }
    }

    public void UpdateText(Fish fish, DateTime updateTime) { this.speciesDict[fish.speciesName].fishBoxes[fish.id].UpdateText(fish.FishShouldExist(updateTime)); }

    public IEnumerator FocusBox(Fish fish)
    {
        SpeciesBox speciesBox = this.speciesDict[fish.speciesName];
        FishBox fishBox = speciesBox.fishBoxes[fish.id];
        
        // Open species box if not already open
        if (!speciesBox.open) speciesBox.OpenCloseBox();
        yield return new WaitForSeconds(0.2f); // to let the SpeciesBox open fully if it was closed

        // Check if the fish box is currently framed by the viewport
        float lower = Math.Abs(
            fishBox.GetComponent<RectTransform>().localPosition.y +
            fishBox.transform.parent.GetComponent<RectTransform>().localPosition.y +
            fishBox.transform.parent.transform.parent.GetComponent<RectTransform>().localPosition.y +
            fishBox.transform.parent.transform.parent.transform.parent.GetComponent<RectTransform>().localPosition.y +
            speciesBox.GetComponent<RectTransform>().localPosition.y + 
            speciesBox.transform.parent.GetComponent<RectTransform>().localPosition.y
        );
        
        float upper = lower + 40f; // adding the size of the header

        RectTransform rectTransform = this.GetComponent<RectTransform>();
        float windowPos = rectTransform.localPosition.y;

        if (windowPos > lower || windowPos < upper - 1020f)
        {
            // Eclipsed by the viewport bounds, shift the content window so that the header is in view
            if (windowPos > lower)
            {
                float diff = windowPos - lower;
                if (rectTransform.localPosition.y - diff >= 510f) rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y - diff - 510f, rectTransform.localPosition.z);
                else rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, 0f, rectTransform.localPosition.z);
            }
            if (windowPos < upper - 1020f)
            {
                float diff = upper - 1020f - windowPos;
                if (rectTransform.rect.height - (rectTransform.localPosition.y + diff) >= 510f) rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.localPosition.y + diff + 510f, rectTransform.localPosition.z);
                else rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, rectTransform.rect.height, rectTransform.localPosition.z);
            }
        }

        // Make the box flash
        fishBox.FlashHeader();
}
}
