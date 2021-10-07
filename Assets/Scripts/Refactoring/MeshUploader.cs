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
    private bool continueColoring = true;

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
            continueColoring = true;

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
        else
        {
            // Avoid looping every update when not necessary
            if (continueColoring)
            {
                continueColoring = false;
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

    public void BackButton()
    {
        MeshClass mesh = GameObject.Find("MeshReader").GetComponent<MeshClass>();
        mesh.backButton = true;

        SceneManager.LoadScene("StartMenu");
    }

    public void ExitChecks()
    {
        GameObject instructionPanel = contentPanel.transform.parent.transform.parent.Find("InstructionPanel").gameObject;
        GameObject activeToggle = paramsPanel.transform.Find("NullFrame").transform.Find("ToggleGroup").GetComponent<ToggleGroup>().ActiveToggles().FirstOrDefault().gameObject;
                            
        if (!currentClickList.Any())
        {
            // Ensure data has been selected
            instructionPanel.transform.Find("Image").transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "No data has been selected in the viewport. Please select only the heightmap data, omitting header rows and ID columns";
            instructionPanel.GetComponent<FadeCanvasGroup>().Fade(6f);
        }
        else if (String.IsNullOrEmpty(paramsPanel.transform.Find("ParametersFrame").transform.Find("WaterLevelFrame").transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text))
        {
            // Ensure a water level has been entered
            instructionPanel.transform.Find("Image").transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "No water level has been specified. Please specify a water level (you can change this later)";
            instructionPanel.GetComponent<FadeCanvasGroup>().Fade(6f);
        }
        else if (parameters.nullCount > 0 && activeToggle.name == "Toggle_2" && string.IsNullOrEmpty(activeToggle.transform.Find("Input").GetComponent<TMP_InputField>().text))
        {
            // Ensure a replacement value has been entered
            instructionPanel.transform.Find("Image").transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "You've selected 'replacement' null handling but specified no value. Please specify a null/NaN replacment value";
            instructionPanel.GetComponent<FadeCanvasGroup>().Fade(6f);
        }
        else
        {   
            // Successful checks
            // Modify the MeshClass table based on selection & nulls
            MeshClass mesh = GameObject.Find("MeshReader").GetComponent<MeshClass>();

            mesh.stringTable = SetTable(mesh.stringTable);
            mesh.waterLevel = float.Parse(paramsPanel.transform.Find("ParametersFrame").transform.Find("WaterLevelFrame").transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text);
            mesh.meshUploaded = true;
            SceneManager.LoadScene("StartMenu");
        }
    }

    private DataTable SetTable(DataTable uploadTable)
    {
        // Trim non-data rows/columns
        int rowsToRemove = 0;
        int columnsToRemove = 0;
        foreach (int idNumber in currentClickList)
        {
            rowsToRemove = Mathf.Max(idNumber - (int)Math.Floor((double)idNumber/(double)viewPortColumns) * viewPortColumns, rowsToRemove);
            columnsToRemove = Mathf.Max(idNumber - (int)Math.Floor((double)idNumber/(double)viewPortRows) * viewPortRows, columnsToRemove);
        }

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

        if (parameters.nullCount > 0)
        {
            ToggleGroup toggleGroup = paramsPanel.transform.Find("NullFrame").transform.Find("ToggleGroup").GetComponent<ToggleGroup>();
            Toggle selectedToggle = toggleGroup.ActiveToggles().FirstOrDefault();

            if (selectedToggle.gameObject.name == "Toggle_1")
            {
                // Interpolate
                foreach (int[] entry in parameters.nullList)
                {
                    // check boundary conditions
                    List<float> boundingValues = new List<float>();
                    
                    if (entry[0] - 1 > rowsToRemove)
                    {
                        // if the entry is within the table limits, check if it is also null
                        if (!parameters.nullList.Any(p => p.SequenceEqual(new int[] {entry[0] - 1, entry[1]})))
                        {
                            boundingValues.Add(float.Parse(uploadTable.Rows[entry[0] - 1][entry[1]].ToString()));
                        }
                    }

                    if (entry[0] + 1 < uploadTable.Rows.Count)
                    {
                        if (!parameters.nullList.Any(p => p.SequenceEqual(new int[] {entry[0] + 1, entry[1]})))
                        {
                            boundingValues.Add(float.Parse(uploadTable.Rows[entry[0] + 1][entry[1]].ToString()));
                        }
                    }

                    if (entry[1] - 1 > columnsToRemove)
                    {
                        if (!parameters.nullList.Any(p => p.SequenceEqual(new int[] {entry[0], entry[1] - 1})))
                        {
                            boundingValues.Add(float.Parse(uploadTable.Rows[entry[0]][entry[1] - 1].ToString()));
                        }
                    }

                    if (entry[1] + 1 < uploadTable.Columns.Count)
                    {
                        if (!parameters.nullList.Any(p => p.SequenceEqual(new int[] {entry[0], entry[1] + 1})))
                        {  
                            boundingValues.Add(float.Parse(uploadTable.Rows[entry[0]][entry[1] + 1].ToString()));
                        }
                    }
                    
                    if (!boundingValues.Any())
                    {
                        // All surrounding values are null
                        Debug.Log(string.Format("Value at [row][column] [{0}][{1}] is surrounded by nulls, interpolation not possible", entry[0], entry[1]));
                        Debug.Log("Replacing this value with a value of 99999");

                        uploadTable.Rows[entry[0]][entry[1]] = 99999;
                    }
                    else
                    {
                        uploadTable.Rows[entry[0]][entry[1]] = boundingValues.Sum() / boundingValues.Count;
                    }
                }
            }
            else
            {
                // Replace
                float replacementVal = float.Parse(selectedToggle.transform.Find("Input").GetComponent<TMP_InputField>().text);
                foreach (int[] entry in parameters.nullList)
                {
                    uploadTable.Rows[entry[0]][entry[1]] = replacementVal;
                }
            }

            uploadTable.AcceptChanges();
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
        nullList = new List<int[]>();

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
}

