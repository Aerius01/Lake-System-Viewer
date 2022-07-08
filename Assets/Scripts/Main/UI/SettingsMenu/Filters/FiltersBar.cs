using System.Collections.Generic;
using System;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FiltersBar : MonoBehaviour
{
    private Transform rootTransform;
    private RectTransform rootRect;
    [SerializeField] private GameObject continuousPrefabTile, categoricalPrefabTile;

    private List<GameObject> contFilterTiles;
    private List<GameObject> catFilterTiles;

    // Singleton framework
    private static FiltersBar _instance;
    [HideInInspector]
    public static FiltersBar instance {get { return _instance; } set {_instance = value; }}

    private void Awake()
    {
        // Singleton framework
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }

        rootTransform = instance.GetComponent<Transform>();
        rootRect = instance.GetComponent<RectTransform>();
        contFilterTiles = new List<GameObject>();
        catFilterTiles = new List<GameObject>();
    }

    public ContinuousFilterTile AddCont(ContinuousFilter filter)
    {
        GameObject newTile = Instantiate (continuousPrefabTile, new Vector3(0,0,0), rootTransform.rotation) as GameObject;
        newTile.transform.SetParent(rootTransform, false);
        newTile.GetComponent<ContinuousFilterTile>().SetFilter(filter);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);

        contFilterTiles.Add(newTile);
        return newTile.GetComponent<ContinuousFilterTile>();
    }

    public CategoricalFilterTile AddCat(CategoricalFilter filter)
    {
        GameObject newTile = Instantiate (categoricalPrefabTile, new Vector3(0,0,0), rootTransform.rotation) as GameObject;
        newTile.transform.SetParent(rootTransform, false);
        newTile.GetComponent<CategoricalFilterTile>().SetFilter(filter);
        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);

        catFilterTiles.Add(newTile);
        return newTile.GetComponent<CategoricalFilterTile>();
    }

    public void RemoveCont(ContinuousFilterTile tile)
    {
        for (int i = contFilterTiles.Count - 1; i >= 0; i--)
        {
            if (contFilterTiles[i].GetComponent<ContinuousFilterTile>() == tile)
            {
                GameObject toDie = contFilterTiles[i];
                contFilterTiles.RemoveAt(i);
                Destroy(toDie);
                break;
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
    }

    public void RemoveCat(CategoricalFilterTile tile)
    {
        for (int i = catFilterTiles.Count - 1; i >= 0; i--)
        {
            if (catFilterTiles[i].GetComponent<CategoricalFilterTile>() == tile)
            {
                GameObject toDie = catFilterTiles[i];
                catFilterTiles.RemoveAt(i);
                Destroy(toDie);
                break;
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
    }
}