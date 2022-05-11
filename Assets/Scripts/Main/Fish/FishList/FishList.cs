using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class FishList : MonoBehaviour
{
    public GameObject headerPrefab;
    private List<FishListElement> elements;

    private float listSize;

    private List<SpeciesBox> speciesList;
    [SerializeField] private GameObject speciesBoxTemplate, fishBoxTemplate;

    public void PopulateListNew()
    {
        elements = new List<FishListElement>();

        // Populate the list
        foreach (int id in LocalPositionData.uniquefishIDs)
        {
            GameObject obj = (Instantiate (headerPrefab) as GameObject);
            obj.transform.SetParent(this.gameObject.transform, worldPositionStays: false);
            obj.transform.Find("Header").transform.Find("FishID").GetComponent<TextMeshProUGUI>().text = id.ToString();
            FishListColoringButton colorHandler = obj.GetComponent<FishListColoringButton>();

            elements.Add(new FishListElement(id, obj, colorHandler));
        }

        // Set the initial content box size
        Vector2 currentSize = this.gameObject.GetComponent<RectTransform>().sizeDelta;
        currentSize.y = LocalPositionData.uniquefishIDs.Length * 60 + (LocalPositionData.uniquefishIDs.Length - 1) * 5;
        this.gameObject.GetComponent<RectTransform>().sizeDelta = currentSize;
    }

    public void PopulateList()
    {
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
    }

    private void FixedUpdate()
    {
        listSize = 0f;
        foreach (SpeciesBox speciesBox in speciesList)
        {
            listSize += speciesBox.open ? speciesBox.contentSize : 60f;
            foreach (FishBox fishBox in speciesBox.components)
            {
                if (fishBox.fish.fishShouldExist)
                {
                    fishBox.UpdateText();
                    if (fishBox.greyedOut) { fishBox.RestoreColor(); }
                }
                else
                {
                    fishBox.UpdateText(active:false);
                    if (!fishBox.greyedOut) { fishBox.Greyout(); }
                }
            }
        }

        RectTransform recter = this.GetComponent<RectTransform>();
        recter.sizeDelta += new Vector2(0f, listSize - recter.rect.height);
    }
}
