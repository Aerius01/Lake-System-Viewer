using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System;
using System.Linq;

public class ViewPort
{
    public List<int> greyIndices, currentClickList;
    public int Columns, Rows;
    private GameObject viewPortPanel;
    public List<GameObject> listOfObjects, listOfDropdowns;
    public UploadTable uploadedTable;
    private bool rootClicked, rootMouseOver;

    public ViewPort(UploadTable uploadTable, GameObject viewPortPanel)
    {
        this.viewPortPanel = viewPortPanel;
        this.uploadedTable = uploadTable;

        this.Columns = Math.Min(uploadedTable.totalColumnCount, 20);
        this.Rows = Math.Min(uploadedTable.totalRowCount, 20);

        currentClickList = new List<int>();
    }

    public void SetGridParams()
    {
        GridLayoutGroup layout = viewPortPanel.GetComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = Columns;

        // Set the content window to the size of the displayed data, or the window boundaries (whichever is bigger)
        RectTransform parentTransform = viewPortPanel.transform.parent.gameObject.GetComponent<RectTransform>();
        parentTransform.sizeDelta = new Vector2(Math.Max(parentTransform.rect.width, Columns * (layout.spacing.x +
            layout.cellSize.x)), Math.Max(parentTransform.rect.height, Rows * (layout.spacing.y + layout.cellSize.y)));
    }

    public void ApplyOutOfBoxColoring()
    {
        for (int j = 0; j < listOfObjects.Count; j++)
        {
            if (currentClickList.Contains(j))
            {
                listOfObjects[j].GetComponent<Image>().color = new Color32(160,160,160,255);
            }
            else
            {
                listOfObjects[j].GetComponent<Image>().color = new Color32(255,255,255,255);
            }
        }
    }

    public void ApplyColoring()
    {
        List<int> newClickList = new List<int>();
        greyIndices = new List<int>();
        bool newRun = true;
        
        // Find out where the mouse is and whether something has been clicked
        for (int i = 0; i < listOfObjects.Count; i++)
        {
            if (listOfObjects[i].GetComponent<SCUtils>().mouse_over)
            {
                if (i == 0)
                {
                    rootMouseOver = true;
                }
                else
                {
                    rootMouseOver = false;
                    greyIndices = DetermineGreySquares(i);
                }
            }

            if (listOfObjects[i].GetComponent<SCUtils>().clicked)
            {
                if (i == 0)
                {
                    // root clicked, empty list but viz needs to change
                    rootClicked = true;
                    newRun = false;
                }
                else
                {
                    rootClicked = false;
                    newClickList = DetermineGreySquares(i);
                }
                
                listOfObjects[i].GetComponent<SCUtils>().clicked = false;
                uploadedTable.ResetParams(i);
            }

            if ((greyIndices.Any() || rootMouseOver) && (newClickList.Any() || (rootClicked && !newRun)))
            {
                break;
            }
        }

        // Change nothing if we're on a gap between cells
        if (greyIndices.Any() || rootMouseOver)
        {
            // Store new click if there was one
            if (newClickList.Any())
            {
                currentClickList = newClickList;
            }
            else if (rootClicked)
            {
                currentClickList = new List<int>();
            }

            // Allocate cell color
            if (currentClickList.Any() || rootClicked)
            {
                // If a click has ever been registered
                for (int j = 0; j < listOfObjects.Count; j++)
                {
                    if (greyIndices.Contains(j) && currentClickList.Contains(j))
                    {
                        listOfObjects[j].GetComponent<Image>().color = new Color32(160,160,160,255);
                    }
                    else if (greyIndices.Contains(j) && !currentClickList.Contains(j))
                    {
                        listOfObjects[j].GetComponent<Image>().color = new Color32(200,200,200,255);
                    }
                    else if (!greyIndices.Contains(j) && currentClickList.Contains(j))
                    {
                        listOfObjects[j].GetComponent<Image>().color = new Color32(180,180,180,255);
                    }
                    else
                    {
                        listOfObjects[j].GetComponent<Image>().color = new Color32(255,255,255,255);
                    }
                }
            }
            else
            {
                // Just apply mouse greying otherwise
                for (int j = 0; j < listOfObjects.Count; j++)
                {
                    if (greyIndices.Contains(j))
                    {
                        listOfObjects[j].GetComponent<Image>().color = new Color32(200,200,200,255);
                    }
                    else
                    {
                        listOfObjects[j].GetComponent<Image>().color = new Color32(255,255,255,255);
                    }
                }
            }
        }
    }

    List<int> DetermineGreySquares(int idNumber)
    {
        // Determine the cells above and behind the cell at the passed ID number
        List<int> greySquares = new List<int>();
        int refVal = idNumber - (int)Math.Floor((double)idNumber/(double)Columns) * Columns;

        for (int i = 0; i < Columns * Rows; i++)
        {
            if (i < idNumber || i - (int)Math.Floor((double)i/(double)Columns) * Columns < refVal)
            {
                greySquares.Add(i);
            }
        }

        return greySquares;
    }
}