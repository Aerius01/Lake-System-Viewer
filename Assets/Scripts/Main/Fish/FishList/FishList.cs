using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class FishList : MonoBehaviour
{
    public GameObject headerPrefab;
    private List<FishListElement> elements;

    public void PopulateList()
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

    private void FixedUpdate()
    {
        foreach (FishListElement element in elements)
        {
            if (element.fishActive)
            {
                element.UpdateText();
                if (element.greyedOut) { element.RestoreColor(); }
                // if header greyed out, restore
                // re-enable all functionality
            }
            else
            {
                element.UpdateText(active:false);
                if (!element.greyedOut) { element.Greyout(); }
                // disable color button
                // disable double click zoom
                // grey out header
            }

            // use fishActive to determine whether to grey out or not, ColoringButton SetNewColor() method
        }
    }
}
