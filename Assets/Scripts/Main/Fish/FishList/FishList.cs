using UnityEngine;
using TMPro;

public class FishList : MonoBehaviour
{
    public GameObject headerPrefab;

    public void PopulateList()
    {
        // Populate the list
        foreach (int id in LocalPositionData.uniquefishIDs)
        {
            GameObject obj = (Instantiate (headerPrefab) as GameObject);
            //obj.transform.parent = this.gameObject.transform;
            obj.transform.SetParent(this.gameObject.transform, worldPositionStays: false);
            obj.transform.Find("Header").transform.Find("FishID").GetComponent<TextMeshProUGUI>().text = id.ToString();

            (bool? male, string speciesName, int? weight, int? length, float currentDepth) = FishManager.GetFishStats(id);

            obj.transform.Find("Content").transform.Find("FishDetails").GetComponent<TextMeshProUGUI>().text = 
                string.Format("Sex: {0}\nSpecies: {1}\nWeight: {2}g\nSize: {3}mm\nDepth: {4:0.00}m",
                male == null ? "?" : (male == true ? "M" : "F"), 
                string.IsNullOrEmpty(speciesName) ? "?" : speciesName,
                weight == null ? "?" : ((int)weight).ToString(),
                length == null ? "?" : ((int)length).ToString(),
                currentDepth
            );
        }

        // Set the initial content box size
        Vector2 currentSize = this.gameObject.GetComponent<RectTransform>().sizeDelta;
        currentSize.y = LocalPositionData.uniquefishIDs.Length * 60 + (LocalPositionData.uniquefishIDs.Length - 1) * 5;
        this.gameObject.GetComponent<RectTransform>().sizeDelta = currentSize;
    }

    private void FixedUpdate()
    {
        foreach (Transform child in this.gameObject.transform)
        {
            string id = child.Find("Header").transform.Find("FishID").GetComponent<TextMeshProUGUI>().text;
            (bool? male, string speciesName, int? weight, int? length, float currentDepth) = FishManager.GetFishStats(int.Parse(id));

            child.Find("Content").transform.Find("FishDetails").GetComponent<TextMeshProUGUI>().text = 
                string.Format("Sex: {0}\nSpecies: {1}\nWeight: {2}g\nSize: {3}mm\nDepth: {4:0.00}m",
                male == null ? "?" : (male == true ? "M" : "F"), 
                string.IsNullOrEmpty(speciesName) ? "?" : speciesName,
                weight == null ? "?" : ((int)weight).ToString(),
                length == null ? "?" : ((int)length).ToString(),
                currentDepth
            );
        }
    }
}
