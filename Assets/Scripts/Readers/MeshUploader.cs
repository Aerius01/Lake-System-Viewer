using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;


public class MeshUploader : MonoBehaviour
{
    public GameObject prefabObject, contentPanel;
    public List<GameObject> listOfObjects;
    private int columnCount, rowCount;
    private List<int> clickList;

    // Start is called before the first frame update
    void Start()
    {
        VisualizeData();
    }

    // Update is called once per frame
    void Update()
    {
        ClickGreying();
        MouseOverGreying();
    }

    void VisualizeData()
    {
        // Get the uploaded file as a DataTable
        NewCSVReader reader = GameObject.Find("MeshReader").GetComponent<NewCSVReader>();
        columnCount = Math.Min(reader.stringTable.Columns.Count, 20);
        rowCount = Math.Min(reader.stringTable.Rows.Count, 20);

        // Adjust layout parameters to simulate the table
        GridLayoutGroup layout = contentPanel.GetComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = columnCount;

        // Set the content window to the size of the displayed data, or the window boundaries (whichever is bigger)
        RectTransform parentTransform = contentPanel.transform.parent.gameObject.GetComponent<RectTransform>();
        parentTransform.sizeDelta = new Vector2(Math.Max(parentTransform.rect.width, columnCount * (layout.spacing.x + layout.cellSize.x)), Math.Max(parentTransform.rect.height, rowCount * (layout.spacing.y + layout.cellSize.y)));

        for (int i = 0; i < rowCount; i++)
        {
            for (int j = 0; j < columnCount; j++)
            {
                GameObject go = (Instantiate (prefabObject) as GameObject);
                go.transform.SetParent(contentPanel.transform);
                go.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = string.Format("{0}", reader.stringTable.Rows[i].ItemArray[j]);

                listOfObjects.Add(go);
            }
        }
    }

    void MouseOverGreying()
    {
        if (contentPanel.transform.parent.gameObject.transform.parent.gameObject.GetComponent<SCUtils>().mouse_over)
        {
            for (int i = 0; i < listOfObjects.Count; i++)
            {
                if (listOfObjects[i].GetComponent<SCUtils>().mouse_over)
                {
                    List<int> greyIndices = DetermineGreySquares(i);
                    for (int j = 0; j < listOfObjects.Count; j++)
                    {
                        if (greyIndices.Contains(j))
                        {
                            listOfObjects[j].GetComponent<Image>().color = new Color32(172,172,172,255);
                        }
                        else
                        {
                            FallbackColors(j);
                        }
                    }

                    break;
                }
            }
        }
        else
        {
            for (int j = 0; j < listOfObjects.Count; j++)
            {
                FallbackColors(j);
            }
        }
    }

    void FallbackColors(int j)
    {
        if (clickList != null)
        {
            if (clickList.Contains(j))
            {
                listOfObjects[j].GetComponent<Image>().color = new Color32(200,200,200,255);
            }
            else
            {
                listOfObjects[j].GetComponent<Image>().color = new Color32(255,255,255,255);
            }
        }
        else
        {
            listOfObjects[j].GetComponent<Image>().color = new Color32(255,255,255,255);
        }
    }

    void ClickGreying()
    {
        for (int i = 0; i < listOfObjects.Count; i++)
        {
            if (listOfObjects[i].GetComponent<SCUtils>().clicked)
            {
                clickList = DetermineGreySquares(i);
                listOfObjects[i].GetComponent<SCUtils>().clicked = false;

                break;
            }
        }

        // TODO: fetch standard data to update parameters text
    }

    List<int> DetermineGreySquares(int idNumber)
    {
        // Determine the cells above and behind the cell at the passed ID number
        List<int> greySquares = new List<int>();
        int refValue = idNumber - (int)Math.Floor((double)idNumber/(double)columnCount) * columnCount;

        for (int i = 0; i < columnCount * rowCount; i++)
        {
            if (i < idNumber || i - (int)Math.Floor((double)i/(double)columnCount) * columnCount < refValue)
            {
                greySquares.Add(i);
            }
        }

        return greySquares;
    }
}
