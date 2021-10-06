using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Data;
using System.Linq;
using UnityEngine.SceneManagement;

public class MeshUploader : MonoBehaviour
{
    public GameObject prefabObject, contentPanel, paramsPanel;
    public List<GameObject> listOfObjects;
    private int totalColumnCount, totalRowCount, viewPortColumns, viewPortRows;
    private List<int> currentClickList;
    private UploadData parameters;

    // Start is called before the first frame update
    private void Start()
    {
        FillViewPort();
        currentClickList = new List<int>();
    }

    // Update is called once per frame
    private void Update()
    {
        ApplyDynamicColoring();
    }

    private void FillViewPort()
    {
        // Get the uploaded file as a DataTable
        MeshClass reader = GameObject.Find("MeshReader").GetComponent<MeshClass>();

        totalColumnCount = reader.stringTable.Columns.Count;
        totalRowCount = reader.stringTable.Rows.Count;

        viewPortColumns = Math.Min(totalColumnCount, 20);
        viewPortRows = Math.Min(totalRowCount, 20);

        parameters = new UploadData(reader.stringTable.Copy(), this.paramsPanel, this.contentPanel);

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

    private void ApplyDynamicColoring()
    {
        if (contentPanel.transform.parent.gameObject.transform.parent.gameObject.GetComponent<SCUtils>().mouse_over)
        {
            List<int> greyIndices = new List<int>();
            List<int> newClickList = new List<int>();

            // Find out where the mouse is and whether something has been clicked
            for (int i = 0; i < listOfObjects.Count; i++)
            {
                if (listOfObjects[i].GetComponent<SCUtils>().mouse_over)
                {
                    greyIndices = DetermineGreySquares(i);
                }

                if (listOfObjects[i].GetComponent<SCUtils>().clicked)
                {
                    newClickList = DetermineGreySquares(i);
                    listOfObjects[i].GetComponent<SCUtils>().clicked = false;

                    parameters.ResetParams(i, totalColumnCount, totalRowCount);
                    paramsPanel.transform.Find("StatisticsFrame").transform.
                        Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = parameters.text;
                }

                if (greyIndices.Any() && newClickList.Any())
                {
                    break;
                }
            }

            // Change nothing if we're on a gap between cells
            if (greyIndices.Any())
            {
                // Store new click if there was one
                if (newClickList.Any())
                {
                    currentClickList = newClickList;
                }

                // Allocate cell color
                if (currentClickList.Any())
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
    }

    List<int> DetermineGreySquares(int idNumber)
    {
        // Determine the cells above and behind the cell at the passed ID number
        List<int> greySquares = new List<int>();
        int refVal = idNumber - (int)Math.Floor((double)idNumber/(double)viewPortColumns) * viewPortColumns;

        for (int i = 0; i < viewPortColumns * viewPortRows; i++)
        {
            if (i < idNumber || i - (int)Math.Floor((double)i/(double)viewPortColumns) * viewPortColumns < refVal)
            {
                greySquares.Add(i);
            }
        }

        return greySquares;
    }

    public void ExitChecks()
    {
        GameObject instructionPanel = contentPanel.transform.parent.transform.parent.Find("InstructionPanel").gameObject;
                            
        if (!currentClickList.Any())
        {
            instructionPanel.transform.Find("Image").transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "No data has been selected in the viewport. Please select only the heightmap data, omitting header rows and ID columns";
            instructionPanel.GetComponent<FadeCanvasGroup>().Fade(6f);
        }
        else if (String.IsNullOrEmpty(paramsPanel.transform.Find("ParametersFrame").transform.Find("WaterLevelFrame").transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text))
        {
            instructionPanel.transform.Find("Image").transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "No water level has been specified. Please specify a water level (you can change this later)";
            instructionPanel.GetComponent<FadeCanvasGroup>().Fade(6f);
        }
        else
        {   
            // Trim the MeshClass table
            MeshClass mesh = GameObject.Find("MeshReader").GetComponent<MeshClass>();

            int maxRow = 0;
            int maxCol = 0;
            foreach (int idNumber in currentClickList)
            {
                maxRow = Mathf.Max(idNumber - (int)Math.Floor((double)idNumber/(double)viewPortColumns) * viewPortColumns, maxRow);
                maxCol = Mathf.Max(idNumber - (int)Math.Floor((double)idNumber/(double)viewPortRows) * viewPortRows, maxCol);
            }

            mesh.stringTable = SetTable(mesh.stringTable, maxCol, maxRow);

            mesh.waterLevel = float.Parse(paramsPanel.transform.Find("ParametersFrame").transform.Find("WaterLevelFrame").transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text);
            mesh.meshUploaded = true;
            SceneManager.LoadScene("StartMenu");
        }
    }

    private DataTable SetTable(DataTable uploadTable, int columnsToRemove, int rowsToRemove)
    {
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

        // TODO: IF INTERP, else replace vals
        foreach (int[] entry in parameters.nullList)
        {
            // check boundary conditions
            List<float> boundingValues = new List<float>();
            
            if (entry[0] - 1 > 0)
            {
                boundingValues.Add(float.Parse(uploadTable.Rows[entry[0] - 1][entry[1]].ToString()));
            }

            if (entry[0] + 1 < uploadTable.Rows.Count)
            {
                boundingValues.Add(float.Parse(uploadTable.Rows[entry[0] + 1][entry[1]].ToString()));
            }

            if (entry[1] - 1 > 0)
            {
                 float.Parse(uploadTable.Rows[entry[0]][entry[1] - 1].ToString());
            }

            if (entry[1] + 1 < uploadTable.Columns.Count)
            {
                float.Parse(uploadTable.Rows[entry[0]][entry[1] + 1].ToString());
            }

            uploadTable.Rows[entry[0]][entry[1]] = boundingValues.Sum() / boundingValues.Count;
        }

        return uploadTable;
    }
}

public class UploadData
{
    public float maxDepth, minDepth;
    public int columnCount, rowCount, nullCount;
    public DataTable uploadTable;
    public String text;
    public GameObject paramsPanel, contentPanel;
    public List<int[]> nullList;

    public UploadData(DataTable uploadTable, GameObject paramsPanel, GameObject contentPanel)
    {
        this.uploadTable = uploadTable; 
        this.paramsPanel = paramsPanel; 
        this.contentPanel = contentPanel; 
    }

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

        bool exceptionThrown = false;

        for (int row = rowsToRemove; row < maxRows; row++)
        {
            for (int column = columnsToRemove; column < maxColumns; column++)
            {
                string stringValue = uploadTable.Rows[row][column].ToString().Trim();
                if (string.IsNullOrEmpty(stringValue))
                {
                    nullList.Add(new int[] {row, column});
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
                        if (!exceptionThrown)
                        {
                            GameObject instructionPanel = contentPanel.transform.parent.transform.parent.Find("InstructionPanel").gameObject;
                            instructionPanel.transform.Find("Image").transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "Format Exception: At least one entry in your selection could not be converted into a decimal number" +
                                ", are you sure you've removed all headers and columns in your selection?";
                            instructionPanel.GetComponent<FadeCanvasGroup>().Fade(6f);
                        }
                    
                        nullList.Add(new int[] {row, column});
                    }
                }
            }
        }

        nullCount = nullList.Count;

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

    // public DataTable SetTable(DataTable uploadTable, int columnsToRemove, int rowsToRemove)
    // {
    //     // Trim non-data rows/columns
    //     for (int r = rowsToRemove; r > 0; r--)
    //     {
    //         uploadTable.Rows[r].Delete();
    //     }
    //     uploadTable.AcceptChanges();

    //     for (int c = columnsToRemove; c > 0; c--)
    //     {
    //         uploadTable.Columns.RemoveAt(c);
    //     }
    //     uploadTable.AcceptChanges();

    //     columnCount = uploadTable.Columns.Count;
    //     rowCount = uploadTable.Rows.Count;

    //     // Find counts
    //     minDepth = int.MaxValue;
    //     maxDepth = int.MinValue;
    //     nullCount = 0;

    //     return uploadTable;

    //     foreach (DataRow row in uploadTable.Rows)
    //     {
    //         for (int column = 0; column < columnCount; column++)
    //         {
    //             //test for null here
    //             if (row[uploadTable.Columns[column]] == DBNull.Value)
    //             {
    //                 nullCount++;
    //             }
    //             else
    //             {
    //                 float value = (float)row[column];
    //                 minDepth = Math.Min(minDepth, value);
    //                 maxDepth = Math.Max(maxDepth, value);
    //             }
    //         }
    //     }
    // }
}

