using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Data;
using UnityEngine.SceneManagement;

public class PositionUploader : MonoBehaviour
{
    public GameObject cellPreFab, dropdownPrefab, contentPanel, paramsPanel;
    private List<int> currentClickList;
    private PositionUploadTable uploadedTable;
    private ViewPort viewPort;
    private bool continueColoring = false;

    private GameObject instructionPanel;

    private Toggle dateTimeToggle;

    // Start is called before the first frame update
    private void Start()
    {
        PositionData reader = GameObject.Find("PositionReader").GetComponent<PositionData>();
        instructionPanel = contentPanel.transform.parent.transform.parent.Find("InstructionPanel").gameObject;

        uploadedTable = new PositionUploadTable(reader.stringTable.Copy(), paramsPanel);
        viewPort = new ViewPort(uploadedTable, contentPanel);
        uploadedTable.viewPort = viewPort;

        FillViewPort();
        currentClickList = new List<int>();
    }

    private void FillViewPort()
    {
        // Get the uploaded file as a DataTable
        viewPort.SetGridParams();
        List<GameObject> listOfObjects = new List<GameObject>();
        List<GameObject> listOfDropdowns = new List<GameObject>();

        for (int i = 0; i < viewPort.Rows; i++)
        {
            for (int j = 0; j < viewPort.Columns; j++)
            {
                GameObject go = (Instantiate (cellPreFab) as GameObject);
                go.transform.SetParent(contentPanel.transform);
                go.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = string.Format("{0}", uploadedTable.uploadTable.Rows[i].ItemArray[j]);

                listOfObjects.Add(go);

                if (i == 0)
                {
                    GameObject ddo = (Instantiate (dropdownPrefab) as GameObject);
                    ddo.transform.SetParent(contentPanel.transform.parent.Find("DropDownFrame"));

                    listOfDropdowns.Add(ddo);
                }
            }
        }

        viewPort.listOfObjects = listOfObjects;
        viewPort.listOfDropdowns = listOfDropdowns;

        CallDialogBox(instructionPanel, "Please select the upper left corner of your\ndata, excluding headers and ID columns.");
    }

    // Update is called once per frame
    private void Update()
    {
        if (contentPanel.transform.parent.gameObject.transform.parent.gameObject.GetComponent<SCUtils>().mouse_over)
        {
            continueColoring = true;
            viewPort.ApplyColoring();

            if (uploadedTable.throwException)
            {
                CallDialogBox(instructionPanel, "Format Exception: At least one entry in your selection\ncould not be converted into a decimal number" +
                    ", are you sure\nyou've removed all headers and columns in your selection?");
            }

        // ADJUST HERE
            // ToggleNullFrame();

        //     paramsPanel.transform.Find("StatisticsFrame").transform.
        //         Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text =
        //         string.Format("Max Depth: {0: 0.00}\nMin Depth: {1: 0.00}\n# Columns: {2}\n# Rows: {3}\n# Null/NaNs: {4}",
        //         uploadedTable.maxDepth, uploadedTable.minDepth, uploadedTable.adjustedColumnCount, uploadedTable.adjustedRowCount, uploadedTable.nullCount);
        }
        else
        {
            // Avoid looping every update when not necessary
            if (continueColoring)
            {
                continueColoring = false;
                viewPort.ApplyOutOfBoxColoring();
            }
        }
    }

    // private void ToggleNullFrame()
    // {
    //     CanvasGroup nullcontent = paramsPanel.transform.Find("NullFrame").transform.Find("ToggleGroup").gameObject.GetComponent<CanvasGroup>();
    //     TextMeshProUGUI textContent = paramsPanel.transform.Find("NullFrame").transform.Find("WarningText").gameObject.GetComponent<TextMeshProUGUI>();
    //     if (uploadedTable.nullCount > 0)
    //     {
    //         nullcontent.alpha = 1;
    //         nullcontent.interactable = true;
            
    //         textContent.text = "!! Null or NaN values detected";
    //         textContent.color = new Color(255f, 0f, 0f, 1f);
    //     }
    //     else
    //     {
    //         nullcontent.alpha = 0;
    //         nullcontent.interactable = false;

    //         textContent.text = "No Null or NaN values detected";
    //         textContent.color = new Color(0f, 255f, 0f, 1f);
    //     }
    // }

    public void BackButton()
    {
        PositionData mesh = GameObject.Find("MeshReader").GetComponent<PositionData>();
        mesh.backButton = true;

        SceneManager.LoadScene("StartMenu");
    }

    public void ExitChecks()
    {
        // Find toggles
        GameObject paramFrame = paramsPanel.transform.Find("ParametersFrame").gameObject;
        Toggle dateTimeToggle = paramFrame.transform.Find("DateTimeFrame").transform.Find("Toggle_1").GetComponent<Toggle>();
        Toggle GISToggle = paramFrame.transform.Find("GISToggle").transform.Find("Toggle_2").GetComponent<Toggle>();

        if (!dateTimeToggle.isOn)
        {
            PositionUploadTable.applyDateFilter = false;
        }
        if (!GISToggle.isOn)
        {
            PositionUploadTable.applyGISConversion = false;
        }

        // Approvals gauntlet for exit
        if (!viewPort.ColumnReqsSatisfied())
        {
            CallDialogBox(instructionPanel, "Either not all required columns are present" +
                "\nin the dropdown selection, or some are duplicated.\n\nPlease verify the dropdowns.");
        }
        else if (dateTimeToggle.isOn)
        {
            if (PositionUploadTable.startCutoff == null || PositionUploadTable.endCutoff == null)
            {
                CallDialogBox(instructionPanel, "Either the start or the end time for" +
                    "\nthe date filter has not been selected.\n\nPlease verify the date selection.");
            }
            else if (DateTime.Compare(PositionUploadTable.startCutoff, PositionUploadTable.endCutoff) > 0)
            {
                CallDialogBox(instructionPanel, "The selected start date takes place after the" +
                    "\nselected end date.\n\nPlease ensure that the start date takes place before the end date.");
            }
            else
            {
                PositionUploadTable.applyDateFilter = true;
            }
        }
        else if (GISToggle.isOn)
        {
            // Get GIS inputs
            Dictionary<string, TMP_InputField> GISBox = new Dictionary<string, TMP_InputField> {
                {"MinLong", paramFrame.transform.Find("GISToggle").transform.Find("Inputs").transform.Find("MinLong").GetComponent<TMP_InputField>()},
                {"MaxLong", paramFrame.transform.Find("GISToggle").transform.Find("Inputs").transform.Find("MaxLong").GetComponent<TMP_InputField>()},
                {"MinLat", paramFrame.transform.Find("GISToggle").transform.Find("Inputs").transform.Find("MinLat").GetComponent<TMP_InputField>()},
                {"MaxLat", paramFrame.transform.Find("GISToggle").transform.Find("Inputs").transform.Find("MaxLat").GetComponent<TMP_InputField>()}};

            if (String.IsNullOrEmpty(GISBox["MinLong"].text) ||
                String.IsNullOrEmpty(GISBox["MaxLong"].text) ||
                String.IsNullOrEmpty(GISBox["MinLat"].text) ||
                String.IsNullOrEmpty(GISBox["MaxLat"].text))
            {
                CallDialogBox(instructionPanel, "One of the values entered for the GIS bounding" +
                    "\nbox is either null or empty.\n\nPlease verify the GIS coordinates entered.");
            }
            else if (float.Parse(GISBox["MinLong"].text) > float.Parse(GISBox["MaxLong"].text))
            {
                CallDialogBox(instructionPanel, "The minimum longitude specified in the GIS bounding" +
                    "\nbox is larger than the maximum longitude.\n\nPlease verify the GIS coordinates entered.");
            }
            else if (float.Parse(GISBox["MinLat"].text) > float.Parse(GISBox["MaxLat"].text))
            {
                CallDialogBox(instructionPanel, "The minimum latitude specified in the GIS bounding" +
                    "\nbox is larger than the maximum latitude.\n\nPlease verify the GIS coordinates entered.");
            }
            else
            {
                PositionUploadTable.applyGISConversion = true;
            }
        }
        else
        {
            Dictionary<string, Type> typeRequirements = new Dictionary<string, Type>() {
                {"ID", typeof(string)}, 
                {"x", typeof(float)}, 
                {"y", typeof(float)}, 
                {"D", typeof(float)}, 
                {"Time", typeof(DateTime)}};

            uploadedTable.uploadTable = uploadedTable.AttributeColumnNames(uploadedTable.uploadTable);
            List<int> rowsWithIssues = uploadedTable.CheckColumnFormatting(uploadedTable.uploadTable, typeRequirements);

            if (rowsWithIssues.Count > 0)
            {
                CallDialogBox(instructionPanel, string.Format("There are {0} rows that failed conversions to their" +
                    "\nrequired data types (a string that couldn't be converted due to formatting). Either" + 
                    "\npress OKAY to continue with these rows removed, or correct the source data and re-upload.", rowsWithIssues.Count),
                    fade: false);

                // Call to finalize table happens externally in the UI by clicking the OKAY button
            }
            else
            {
                FinalizeTable();
            }
        }
    }

    private void CallDialogBox(GameObject panel, string message, bool fade = true)
    {
        panel.GetComponent<CanvasGroup>().alpha = 0;
        panel.transform.Find("Image").transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = message;

        // Refresh canvas calculations
        LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());

        if (fade)
        {
            panel.GetComponent<FadeCanvasGroup>().Fade(6f);
        }
        else
        {
            panel.GetComponent<CanvasGroup>().alpha = 1;
            panel.transform.parent.transform.Find("Buttons").GetComponent<CanvasGroup>().alpha = 1;
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(panel.GetComponent<RectTransform>());
    }
        

    public void FinalizeTable()
    {

        uploadedTable.uploadTable = uploadedTable.AttributeColumnNames(uploadedTable.uploadTable, removeUnnamed: true);
        // if
        // {   
        //     // Successful checks
        //     // Modify the PositionData table based on selection & nulls
        //     PositionData positionData = GameObject.Find("PositionReader").GetComponent<PositionData>();

        //     // if (activeToggle.name == "Toggle_1")
        //     // {
        //     //     uploadedTable.SetTable(currentClickList, 1);
        //     // }
        //     // else if (activeToggle.name == "Toggle_2")
        //     // {
        //     //     float replacementVal = float.Parse(activeToggle.transform.Find("Input").GetComponent<TMP_InputField>().text);
        //     //     uploadedTable.SetTable(currentClickList, 2, replacementVal);
        //     // }
        //     // else
        //     // {
        //     //     uploadedTable.SetTable(currentClickList);
        //     // }

        //     positionData.stringTable = uploadedTable.uploadTable;
        //     // positionData.waterLevel = float.Parse(paramsPanel.transform.Find("ParametersFrame").transform.Find("WaterLevelFrame").transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text);
        //     positionData.positionsUploaded = true;
        //     SceneManager.LoadScene("StartMenu");
        // }
    }
}

