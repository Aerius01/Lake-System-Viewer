using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class FilterBar : MonoBehaviour
{
    public static ButtonTriggerAnimation menuPanel { get; private set; }
    private static Transform rootTransform;
    private static RectTransform rootRect;
    private static List<FiltersRow> filterRows;
    [SerializeField] private GameObject continuousPrefabTile, categoricalPrefabTile, filterRowPrefab;
    private bool triggerRecalculate = false, calculating = false;

    // Singleton framework
    private static FilterBar _instance;
    [HideInInspector]
    public static FilterBar instance {get { return _instance; } set {_instance = value; }}

    private void Awake()
    {
        // Singleton framework
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }

        FilterBar.rootTransform = this.gameObject.GetComponent<Transform>();
        FilterBar.rootRect = this.gameObject.GetComponent<RectTransform>();
        FilterBar.filterRows = new List<FiltersRow>();

        // Subscribe to animation menu close/open for refreshing filter positions
        FilterBar.menuPanel = this.transform.parent.transform.parent.GetComponent<ButtonTriggerAnimation>();
        menuPanel.menuChange += RecalculateRows;
    }

    private void Update() { if (triggerRecalculate && !calculating) RecalculateRows(); }

    public void AddCont(ContinuousFilter filter)
    {
        // Create the tile
        GameObject newTile = Instantiate (continuousPrefabTile, new Vector3(0,0,0), FilterBar.rootTransform.rotation) as GameObject;
        ContinuousFilterTile filterTile = newTile.GetComponent<ContinuousFilterTile>();
        filterTile.SetFilter(filter);

        // See if the tile fits in any existing rows
        bool allocated = false;
        if (filterRows.Any())
        {
            foreach (FiltersRow row in filterRows)
            {
                if (filterTile.width <= row.remainingSpace)
                {
                    row.AddContTile(newTile);
                    allocated = true;
                    break;
                }
            }
        }

        if (!allocated) AddNewRow(newTile); 
    }

    public CategoricalFilterTile AddCat(CategoricalFilter filter)
    {
        // Create the tile
        GameObject newTile = Instantiate (categoricalPrefabTile, new Vector3(0,0,0), FilterBar.rootTransform.rotation) as GameObject;
        CategoricalFilterTile filterTile = newTile.GetComponent<CategoricalFilterTile>();
        filterTile.SetFilter(filter);

        // See if the tile fits in any existing rows
        bool allocated = false;
        if (filterRows.Any())
        {
            foreach (FiltersRow row in filterRows)
            {
                if (filterTile.width <= row.remainingSpace)
                {
                    row.AddCatTile(newTile);
                    allocated = true;
                    break;
                }
            }
        }

        if (!allocated)
        {
            // Create a new row
            GameObject newRow = Instantiate (filterRowPrefab, new Vector3(0,0,0), rootTransform.rotation) as GameObject;
            newRow.transform.SetParent(rootTransform, false);
            FiltersRow filterRow = newRow.GetComponent<FiltersRow>();
            filterRow.AddCatTile(newTile);

            LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
            filterRows.Add(filterRow);
        }

        return filterTile;
    }

    public void DeleteCont(bool clearLength)
    {
        foreach (FiltersRow row in filterRows) { row.DeleteAllContinuousTiles(clearLength); }

        // Raise flag, as game objects are only destroyed at the end of the current frame
        // Cannot immediately continue in the current action frame
        triggerRecalculate = true;
    }

    public void DeleteCat(bool clearSex)
    {
        foreach (FiltersRow row in filterRows) { row.DeleteAllCategoricalTiles(clearSex); }

        // Raise flag, as game objects are only destroyed at the end of the current frame
        // Cannot immediately continue in the current action frame
        triggerRecalculate = true;
    }

    private void AddNewRow(GameObject newTile)
    {
        GameObject newRow = Instantiate (filterRowPrefab, new Vector3(0,0,0), rootTransform.rotation) as GameObject;
        newRow.transform.SetParent(rootTransform, false);
        FiltersRow filterRow = newRow.GetComponent<FiltersRow>();

        if (newTile.GetComponent<ContinuousFilterTile>() != null) filterRow.AddContTile(newTile);
        else filterRow.AddCatTile(newTile);

        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        filterRows.Add(filterRow);
    }

    private void RecalculateRows()
    {
        calculating = true;
        triggerRecalculate = false;

        // Reassess spacing without deleting rows (for now)
        for (int r = 0; r < filterRows.Count; r++)
        {
            FiltersRow baseRow = filterRows[r];
            if (baseRow.remainingSpace < 0)
            {
                // Extrude rows, this row is too long
                int count = baseRow.transform.childCount;
                for(int t = count - 1; t >= 0; t--)
                {
                    if (baseRow.remainingSpace >= 0) break;
                    else
                    {
                        bool allocated = false;
                        Transform child = baseRow.transform.GetChild(t);
                        for (int i = filterRows.IndexOf(baseRow) + 1; i < filterRows.Count; i++)
                        {
                            if (allocated) break;

                            FiltersRow targetRow = filterRows[i];
                            if (child.GetComponent<ContinuousFilterTile>() != null)
                            {
                                // ContinuousFilter
                                if (child.GetComponent<ContinuousFilterTile>().width <= targetRow.remainingSpace)
                                {
                                    targetRow.AddContTile(child.gameObject);
                                    baseRow.RemoveContinuousTile(child.GetComponent<ContinuousFilterTile>());
                                    allocated = true;
                                }
                                else continue;  
                            }
                            else
                            {
                                // CategoricalFilter
                                if (child.GetComponent<CategoricalFilterTile>().width <= targetRow.remainingSpace)
                                {
                                    targetRow.AddCatTile(child.gameObject);
                                    baseRow.RemoveCategoricalTile(child.GetComponent<CategoricalFilterTile>());
                                    allocated = true;
                                }
                                else continue;
                            }
                        }

                        if (!allocated) 
                        {
                            AddNewRow(child.gameObject);
                            if (child.GetComponent<ContinuousFilterTile>() != null) baseRow.RemoveContinuousTile(child.GetComponent<ContinuousFilterTile>());
                            else baseRow.RemoveCategoricalTile(child.GetComponent<CategoricalFilterTile>());
                        }
                    }
                }
            }
            else
            {
                // Consolidate rows, this row is too short
                bool baseRowMaxed = false;
                for (int i = filterRows.IndexOf(baseRow) + 1; i < filterRows.Count; i++)
                {
                    FiltersRow targetRow = filterRows[i];

                    int count = targetRow.transform.childCount;
                    if (count == 0) continue;
                    for(int t = count - 1; t >= 0; t--)
                    {
                        Transform child = targetRow.transform.GetChild(t);

                        // Does it fit in the base row?
                        if (child.GetComponent<ContinuousFilterTile>() != null)
                        {
                            // ContinuousFilter
                            if (child.GetComponent<ContinuousFilterTile>().width <= baseRow.remainingSpace)
                            {
                                baseRow.AddContTile(child.gameObject);
                                targetRow.RemoveContinuousTile(child.GetComponent<ContinuousFilterTile>());
                            }
                            else
                            {
                                baseRowMaxed = true;
                                break;
                            }
                        }
                        else
                        {
                            // CategoricalFilter
                            if (child.GetComponent<CategoricalFilterTile>().width <= baseRow.remainingSpace)
                            {
                                baseRow.AddCatTile(child.gameObject);
                                targetRow.RemoveCategoricalTile(child.GetComponent<CategoricalFilterTile>());
                            }
                            else
                            {
                                baseRowMaxed = true;
                                break;
                            }
                        }
                    }

                    if (baseRowMaxed) break;
                }
            }
        }

        // Remove rows if they're empty
        for (int r = filterRows.Count - 1; r > 0; r--)
        {
            if (filterRows[r].isEmpty)
            {
                filterRows[r].Delete();
                filterRows.RemoveAt(r);
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(rootRect);
        calculating = false;
    }
}