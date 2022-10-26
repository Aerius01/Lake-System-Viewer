using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FiltersRow : MonoBehaviour
{
    private Transform rootTransform;
    private RectTransform rootRect;

    public List<GameObject> contFilterTiles { get; private set; }
    public List<GameObject> catFilterTiles { get; private set; }

    public bool isEmpty { get { return (!contFilterTiles.Any() && !catFilterTiles.Any()); } }
    public float remainingSpace { get { return this.gameObject.GetComponentInParent<CanvasScaler>().referenceResolution.x - FilterBar.menuPanel.menuWidth - this.rowWidth; } }
    private float rowWidth 
    { 
        get
        {
            float totalWidth = 15f;
            foreach (GameObject obj in contFilterTiles) totalWidth += (obj.GetComponent<RectTransform>().rect.width + obj.GetComponent<HorizontalLayoutGroup>().spacing); 
            foreach (GameObject obj in catFilterTiles) totalWidth += (obj.GetComponent<RectTransform>().rect.width + obj.GetComponent<HorizontalLayoutGroup>().spacing); 
            return totalWidth;
        }
    }
    
    private void Awake()
    {
        rootTransform = this.gameObject.GetComponent<Transform>();
        rootRect = this.gameObject.GetComponent<RectTransform>();
        contFilterTiles = new List<GameObject>();
        catFilterTiles = new List<GameObject>();
    }

    public void AddContTile(GameObject obj)
    {
        obj.transform.SetParent(rootTransform, false);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        contFilterTiles.Add(obj);
    }

    public void AddCatTile(GameObject obj)
    {
        obj.transform.SetParent(rootTransform, false);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        catFilterTiles.Add(obj);
    }

    public void DeleteAllContinuousTiles(bool clearLength)
    {
        for (int i = contFilterTiles.Count - 1; i >= 0; i--)
        {
            if (contFilterTiles[i].GetComponent<ContinuousFilterTile>().filter.isLengthFilter == clearLength)
            {
                Destroy(contFilterTiles[i]);
                contFilterTiles.RemoveAt(i);
            }
        }
    }

    public void DeleteAllCategoricalTiles(bool clearSex)
    {
        for (int i = catFilterTiles.Count - 1; i >= 0; i--)
        {
            if (catFilterTiles[i].GetComponent<CategoricalFilterTile>().filter.isSexFilter == clearSex)
            {
                Destroy(catFilterTiles[i]);
                catFilterTiles.RemoveAt(i);
            }
        }
    }

    public void RemoveContinuousTile(ContinuousFilterTile filterTile)
    {
        for (int i = contFilterTiles.Count - 1; i >= 0; i--)
        { if (contFilterTiles[i].GetComponent<ContinuousFilterTile>() == filterTile) contFilterTiles.RemoveAt(i); }
    }

    public void RemoveCategoricalTile(CategoricalFilterTile filterTile)
    {
        for (int i = catFilterTiles.Count - 1; i >= 0; i--)
        { if (catFilterTiles[i].GetComponent<CategoricalFilterTile>() == filterTile) catFilterTiles.RemoveAt(i); }
    }

    public void Delete() { Destroy(this.gameObject); }
}