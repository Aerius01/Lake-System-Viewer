using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Data;


public class MeshUploader : MonoBehaviour
{
    public GameObject prefabObject, contentPanel, paramsPanel;
    public List<GameObject> listOfObjects;
    private int totalColumnCount, totalRowCount, viewPortColumns, viewPortRows;
    private List<int> clickList;
    private UploadData parameters;

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

        totalColumnCount = reader.stringTable.Columns.Count;
        totalRowCount = reader.stringTable.Rows.Count;

        viewPortColumns = Math.Min(totalColumnCount, 20);
        viewPortRows = Math.Min(totalRowCount, 20);

        parameters = new UploadData();
        parameters.uploadTable = reader.stringTable.Copy();
        parameters.paramsPanel = this.paramsPanel;
        parameters.contentPanel = this.contentPanel;

        // Adjust layout parameters to simulate the table
        GridLayoutGroup layout = contentPanel.GetComponent<GridLayoutGroup>();
        layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
        layout.constraintCount = viewPortColumns;

        // Set the content window to the size of the displayed data, or the window boundaries (whichever is bigger)
        RectTransform parentTransform = contentPanel.transform.parent.gameObject.GetComponent<RectTransform>();
        parentTransform.sizeDelta = new Vector2(Math.Max(parentTransform.rect.width, viewPortColumns * (layout.spacing.x +
            layout.cellSize.x)), Math.Max(parentTransform.rect.height, viewPortRows * (layout.spacing.y + layout.cellSize.y)));

        for (int i = 0; i < viewPortRows; i++)
        {
            for (int j = 0; j < viewPortColumns; j++)
            {
                GameObject go = (Instantiate (prefabObject) as GameObject);
                go.transform.SetParent(contentPanel.transform);
                go.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = string.Format("{0}", reader.stringTable.Rows[i].ItemArray[j]);

                listOfObjects.Add(go);
            }
        }

        GameObject instructionPanel = contentPanel.transform.parent.transform.parent.Find("InstructionPanel").gameObject;
        instructionPanel.transform.Find("Image").transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "Please select the upper left corner of your data, excluding headers and ID columns.";
        instructionPanel.GetComponent<FadeCanvasGroup>().Fade(4f);
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

                parameters.ResetParams(i, totalColumnCount, totalRowCount);
                paramsPanel.transform.Find("StatisticsFrame").transform.
                    Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = parameters.text;

                break;
            }
        }
    }

    List<int> DetermineGreySquares(int idNumber)
    {
        // Determine the cells above and behind the cell at the passed ID number
        List<int> greySquares = new List<int>();
        int refValue = idNumber - (int)Math.Floor((double)idNumber/(double)viewPortColumns) * viewPortColumns;

        for (int i = 0; i < viewPortColumns * viewPortRows; i++)
        {
            if (i < idNumber || i - (int)Math.Floor((double)i/(double)viewPortColumns) * viewPortColumns < refValue)
            {
                greySquares.Add(i);
            }
        }

        return greySquares;
    }
}

public class UploadData
{
    public float maxDepth, minDepth;

    public int columnCount, rowCount, nullCount;

    public DataTable uploadTable;

    public String text;

    public GameObject paramsPanel, contentPanel;

    public void ResetParams(int i, int maxColumns, int maxRows)
    {
        int viewPortColumns = Math.Min(maxColumns, 20);
        int viewPortRows = Math.Min(maxRows, 20);

        int columnsToRemove = i - (int)Math.Floor((double)i/(double)viewPortColumns) * viewPortColumns;
        int rowsToRemove = (int)Math.Floor((double)i/(double)viewPortColumns);

        columnCount = maxColumns - columnsToRemove;
        rowCount = maxRows - rowsToRemove;

        minDepth = int.MaxValue;
        maxDepth = int.MinValue;
        nullCount = 0;

        for (int row = rowsToRemove; row < maxRows; row++)
        {
            for (int column = columnsToRemove; column < maxColumns; column++)
            {
                string stringValue = uploadTable.Rows[row][column].ToString().Trim();
                if (string.IsNullOrEmpty(stringValue))
                {
                    nullCount++;
                }
                else
                {
                    try
                    {
                        float value = float.Parse(stringValue);
                        minDepth = Math.Min(minDepth, value);
                        maxDepth = Math.Max(maxDepth, value);
                    }
                    catch (FormatException)
                    {
                        GameObject instructionPanel = contentPanel.transform.parent.transform.parent.Find("InstructionPanel").gameObject;
                        instructionPanel.transform.Find("Image").transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "Format Exception: At least one entry in your selection could not be converted into a decimal number" +
                            ", are you sure you've removed all headers and columns in your selection?";
                        instructionPanel.GetComponent<FadeCanvasGroup>().Fade(6f);

                        break;
                    }
                }
            }
        }

        text = string.Format("Max Depth: {0: 0.00}\nMin Depth: {1: 0.00}\n# Columns: {2}\n# Rows: {3}\n# Null/NaNs: {4}",
            maxDepth, minDepth, columnCount, rowCount, nullCount);
        
        if (nullCount > 0)
        {
            CanvasGroup nullcontent = paramsPanel.transform.Find("NullFrame").transform.Find("ToggleGroup").gameObject.GetComponent<CanvasGroup>();
            nullcontent.alpha = 1;
            nullcontent.interactable = true;

            TextMeshProUGUI textContent = paramsPanel.transform.Find("NullFrame").transform.Find("WarningText").gameObject.GetComponent<TextMeshProUGUI>();
            textContent.text = "!! Null or NaN values detected";
            textContent.color = new Color(255f, 0f, 0f, 1f);
        }
        else
        {
            CanvasGroup nullcontent = paramsPanel.transform.Find("NullFrame").transform.Find("ToggleGroup").gameObject.GetComponent<CanvasGroup>();
            nullcontent.alpha = 0;
            nullcontent.interactable = false;

            TextMeshProUGUI textContent = paramsPanel.transform.Find("NullFrame").transform.Find("WarningText").gameObject.GetComponent<TextMeshProUGUI>();
            textContent.text = "No Null or NaN values detected";
            textContent.color = new Color(0f, 255f, 0f, 1f);
        }
    }

    public void SetTable(int i, int maxColumns)
    {
        int columnsToRemove = i - (int)Math.Floor((double)i/(double)maxColumns) * maxColumns;
        int rowsToRemove = (int)Math.Floor((double)i/(double)maxColumns);

        // TODO: convert to floats, if failed, raise error.

        // Trim non-data rows/columns
        for (int r = rowsToRemove; r > 0; r--)
        {
            uploadTable.Rows[r].Delete();
        }
        uploadTable.AcceptChanges();

        for (int c = columnsToRemove; c > 0; c--)
        {
            uploadTable.Columns.RemoveAt(c);
        }
        uploadTable.AcceptChanges();

        columnCount = uploadTable.Columns.Count;
        rowCount = uploadTable.Rows.Count;

        // Find counts
        minDepth = int.MaxValue;
        maxDepth = int.MinValue;
        nullCount = 0;

        foreach (DataRow row in uploadTable.Rows)
        {
            for (int column = 0; column < columnCount; column++)
            {
                //test for null here
                if (row[uploadTable.Columns[column]] == DBNull.Value)
                {
                    nullCount++;
                }
                else
                {
                    float value = (float)row[column];
                    minDepth = Math.Min(minDepth, value);
                    maxDepth = Math.Max(maxDepth, value);
                }
            }
        }
    }
}
