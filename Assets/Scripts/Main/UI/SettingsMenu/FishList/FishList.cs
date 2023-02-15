using UnityEngine;
using System.Collections.Generic;
using System;

public class FishList : MonoBehaviour
{
    private bool listPopulated = false;

    private List<SpeciesBox> speciesList;
    [SerializeField] private GameObject speciesBoxTemplate, fishBoxTemplate;

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
                obj.transform.SetParent(this.gameObject.transform, worldPositionStays: false);

                SpeciesBox box = obj.GetComponent<SpeciesBox>();
                box.SetUpBox(fish.speciesName);
                speciesList.Add(box);

                // Add the fish to the new SpeciesBox
                GameObject fishBoxObj = (Instantiate (fishBoxTemplate) as GameObject);
                FishBox fishBox = fishBoxObj.GetComponent<FishBox>();
                box.AddFish(fish, fishBox);
            }
        }

        listPopulated = true;
    }

    public void Clear()
    {
        this.listPopulated = false;
        if (this.speciesList != null) { foreach (SpeciesBox speciesBox in this.speciesList) { speciesBox.Clear(); }; this.speciesList = null; }
    }

    private void FixedUpdate()
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
}
