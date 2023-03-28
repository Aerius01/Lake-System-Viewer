using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using System;

public class FishList : MonoBehaviour
{
    private bool listPopulated = false;

    private List<SpeciesBox> speciesList;
    [SerializeField] private GameObject speciesBoxTemplate, fishBoxTemplate, speciesListParent;

    private static FishList _instance;
    [HideInInspector] public static FishList instance {get { return _instance; } set {_instance = value; }}

    public void WakeUp()
    {
        // Destroy duplicate instances
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }

        speciesList = new List<SpeciesBox>();

        // Populate the list
        foreach (Fish fish in FishManager.fishDict.Values)
        {
            bool attributed = false;
            foreach (SpeciesBox speciesBox in speciesList)
            {
                if (fish.speciesName == speciesBox.speciesName)
                {
                    GameObject obj = (Instantiate (fishBoxTemplate) as GameObject);
                    FishBox fishBox = obj.GetComponent<FishBox>();

                    // sets parent, rank, and sets up the FishBox
                    speciesBox.AddFish(fish, fishBox);
                    attributed = true;
                }

                if (attributed) break;
            }

            if (!attributed) // create the species box if it doesn't exist
            {
                GameObject obj = (Instantiate (speciesBoxTemplate) as GameObject);
                obj.transform.SetParent(speciesListParent.transform, worldPositionStays: false);

                SpeciesBox newSpeciesBox = obj.GetComponent<SpeciesBox>();
                newSpeciesBox.SetUpBox(fish.speciesName);
                speciesList.Add(newSpeciesBox);

                // Add the fish to the new SpeciesBox
                GameObject fishBoxObj = (Instantiate (fishBoxTemplate) as GameObject);
                FishBox fishBox = fishBoxObj.GetComponent<FishBox>();
                newSpeciesBox.AddFish(fish, fishBox);
            }
        }

        listPopulated = true;
    }

    public void Clear()
    {
        this.listPopulated = false;
        if (this.speciesList != null) { foreach (SpeciesBox speciesBox in this.speciesList) { speciesBox.Clear(); }; this.speciesList = null; }
    }

    private void Update()
    {
        if (this.listPopulated)
        {
            // Harmonize time across entire update time window
            DateTime updateTime = TimeManager.instance.currentTime;

            foreach (SpeciesBox speciesBox in speciesList)
            {
                bool reorder = false;
                foreach (FishBox fishBox in speciesBox.components)
                {
                    // If at least one fish box has changed active/inactive status, re-order
                    if (fishBox.fish.FishShouldExist(updateTime))
                    {
                        fishBox.UpdateText();
                        if (fishBox.greyedOut) { fishBox.RestoreColor(); reorder = true; }
                    }
                    else
                    {
                        fishBox.UpdateText(active:false);
                        if (!fishBox.greyedOut) { fishBox.Greyout(); reorder = true; }
                    }
                }

                if (reorder) speciesBox.ReorderBoxes();
            }
        }
    }

    public IEnumerator FocusBox(int id)
    {
        foreach (SpeciesBox speciesBox in this.speciesList) 
        { 
            foreach (FishBox fishBox in speciesBox.components) 
            { 
                if (fishBox.fish.id == id) 
                {
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
        }
    }
}
