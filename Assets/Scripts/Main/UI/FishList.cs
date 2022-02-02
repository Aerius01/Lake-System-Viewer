using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class FishList : MonoBehaviour
{
    private Dictionary<int, MetaData> metaDict;
    public GameObject headerPrefab;

    public void PopulateList()
    {
        // Populate the list
        metaDict = FishGeneratorNew.GetFishMetaData();
        foreach (var key in metaDict.Keys)
        {
            GameObject obj = (Instantiate (headerPrefab) as GameObject);
            //obj.transform.parent = this.gameObject.transform;
            obj.transform.SetParent(this.gameObject.transform, worldPositionStays: false);
            obj.transform.Find("Header").transform.Find("FishID").GetComponent<TextMeshProUGUI>().text = key.ToString();
            obj.transform.Find("Content").transform.Find("FishDetails").GetComponent<TextMeshProUGUI>().text = 
                string.Format("Sex: M\nSpecies: Perch\nAge: ?\nSize: 360mm\nDepth: {0:0.00}m", metaDict[key].depth);
        }

        // Set the initial content box size
        Vector2 currentSize = this.gameObject.GetComponent<RectTransform>().sizeDelta;
        currentSize.y = metaDict.Keys.Count * 60 + (metaDict.Keys.Count - 1) * 5;
        this.gameObject.GetComponent<RectTransform>().sizeDelta = currentSize;
    }

    private void FixedUpdate()
    {
        foreach (Transform child in this.gameObject.transform)
        {
            child.Find("Content").transform.Find("FishDetails").GetComponent<TextMeshProUGUI>().text = 
                string.Format("Sex: M\nSpecies: Perch\nAge: ?\nSize: 360mm\nDepth: {0:0.00}m",
                FishGeneratorNew.CurrentFishDepth(int.Parse(child.Find("Header").transform.Find("FishID").GetComponent<TextMeshProUGUI>().text)));
        }
    }
}
