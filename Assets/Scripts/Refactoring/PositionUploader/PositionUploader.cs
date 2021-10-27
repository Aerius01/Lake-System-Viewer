using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using System.Data;
using UnityEngine.SceneManagement;

public class PositionUploader : MonoBehaviour
{
    public GameObject cellPreFab, dropdownPrefab, contentPanel, paramsPanel;
    private List<int> currentClickList;
    private Dictionary<string, float> GISBox;
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

        if (uploadedTable.waitingOnReset)
        {
            if (uploadedTable.resetComplete)
            {
                // Update only if we've reset the params successfully
                paramsPanel.transform.Find("StatisticsFrame").transform.Find("TextContainer").transform.
                    Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text =
                    string.Format("# Sel. Rows: {0}\n# Sel. Columns: {1}\n# Null/NaNs: {2}",
                    uploadedTable.adjustedRowCount, uploadedTable.adjustedColumnCount, uploadedTable.nullCount);

                uploadedTable.containerCG.alpha = 1f;
                uploadedTable.loadingIcon.SetActive(false);
                uploadedTable.waitingOnReset = false;
            }
            else if (uploadedTable.stopThread)
            {
                // if we've commanded the thread to stop, reset the UI
                uploadedTable.containerCG.alpha = 1f;
                uploadedTable.loadingIcon.SetActive(false);
                // waitingOnReset is set to false in the UploadTable reset loop
            }
        }
    }

    public void BackButton()
    {
        PositionData positionData = GameObject.Find("PositionReader").GetComponent<PositionData>();
        positionData.backButton = true;

        SceneManager.LoadScene("StartMenu");
    }

    public void ExitChecks()
    {
        // Find toggles
        GameObject paramFrame = paramsPanel.transform.Find("ParametersFrame").gameObject;
        Toggle dateTimeToggle = paramFrame.transform.Find("DateTimeFrame").transform.Find("Toggle_1").GetComponent<Toggle>();
        Toggle GISToggle = paramFrame.transform.Find("GISToggle").transform.Find("Toggle_2").GetComponent<Toggle>();

        // Get GIS inputs
        Dictionary<string, TMP_InputField> GIS = new Dictionary<string, TMP_InputField> {
            {"MinLong", paramFrame.transform.Find("GISToggle").transform.Find("Inputs").transform.Find("MinLong").GetComponent<TMP_InputField>()},
            {"MaxLong", paramFrame.transform.Find("GISToggle").transform.Find("Inputs").transform.Find("MaxLong").GetComponent<TMP_InputField>()},
            {"MinLat", paramFrame.transform.Find("GISToggle").transform.Find("Inputs").transform.Find("MinLat").GetComponent<TMP_InputField>()},
            {"MaxLat", paramFrame.transform.Find("GISToggle").transform.Find("Inputs").transform.Find("MaxLat").GetComponent<TMP_InputField>()}};

        bool error = false;

        // Ensure data has been selected
        if (!viewPort.currentClickList.Any())
        {
            CallDialogBox(instructionPanel, "No data has been selected in the viewport. Please" +
                "\nselect only the actual data, omitting the header row");
            error = true;
        }

        // General column checks
        if (!viewPort.ColumnReqsSatisfied())
        {
            CallDialogBox(instructionPanel, "Either not all required columns are present" +
                "\nin the dropdown selection, or some are duplicated.\n\nPlease verify the dropdowns.");
            error = true;
        }

        // DateTime filter checks
        if (!error)
        {
            if (dateTimeToggle.isOn)
            {
                if (PositionUploadTable.startCutoff == null || PositionUploadTable.endCutoff == null)
                {
                    CallDialogBox(instructionPanel, "Either the start or the end time for" +
                        "\nthe date filter has not been selected.\n\nPlease verify the date selection.");
                        error = true;
                }
                else if (DateTime.Compare(PositionUploadTable.startCutoff, PositionUploadTable.endCutoff) > 0)
                {
                    CallDialogBox(instructionPanel, "The selected start date takes place after the" +
                        "\nselected end date.\n\nPlease ensure that the start date takes place before the end date.");
                        error = true;
                }
                else
                {
                    PositionUploadTable.applyDateFilter = true;
                }
            }
            else
            {
                PositionUploadTable.applyDateFilter = false;
            }
        }

        // GIS conversion checks
        if (!error)
        {
            if (GISToggle.isOn)
            {
                if (String.IsNullOrEmpty(GIS["MinLong"].text) ||
                    String.IsNullOrEmpty(GIS["MaxLong"].text) ||
                    String.IsNullOrEmpty(GIS["MinLat"].text) ||
                    String.IsNullOrEmpty(GIS["MaxLat"].text))
                {
                    CallDialogBox(instructionPanel, "One of the values entered for the GIS bounding" +
                        "\nbox is either null or empty.\n\nPlease verify the GIS coordinates entered.");
                    error = true;
                }
                else if (float.Parse(GIS["MinLong"].text) > float.Parse(GIS["MaxLong"].text))
                {
                    CallDialogBox(instructionPanel, "The minimum longitude specified in the GIS bounding" +
                        "\nbox is larger than the maximum longitude.\n\nPlease verify the GIS coordinates entered.");
                    error = true;
                }
                else if (float.Parse(GIS["MinLat"].text) > float.Parse(GIS["MaxLat"].text))
                {
                    CallDialogBox(instructionPanel, "The minimum latitude specified in the GIS bounding" +
                        "\nbox is larger than the maximum latitude.\n\nPlease verify the GIS coordinates entered.");
                    error = true;
                }
                else
                {
                    PositionUploadTable.applyGISConversion = true;
                }
            }
            else
            {
                PositionUploadTable.applyGISConversion = false;
            }
        }
        
        // Successfully met requirements
        if (!error)
        {
            Dictionary<string, Type> typeRequirements = new Dictionary<string, Type>() {
                {"ID", typeof(string)}, 
                {"x", typeof(float)}, 
                {"y", typeof(float)}, 
                {"D", typeof(float)}, 
                {"Time", typeof(DateTime)}};

            if (PositionUploadTable.applyGISConversion)
            {
                GISBox = new Dictionary<string, float> {
                    {"MinLong", float.Parse(GIS["MinLong"].text)},
                    {"MaxLong", float.Parse(GIS["MaxLong"].text)},
                    {"MinLat", float.Parse(GIS["MinLat"].text)},
                    {"MaxLat", float.Parse(GIS["MaxLat"].text)}};
            }
            
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
        PositionData positionData = GameObject.Find("PositionReader").GetComponent<PositionData>();

        uploadedTable.SetTable(viewPort.currentClickList);

        // Start filling out the PositionData object
        positionData.stringTable = uploadedTable.uploadTable;
        positionData.usingFilterDates = PositionUploadTable.applyDateFilter;
        positionData.usingGIS = PositionUploadTable.applyGISConversion;

        if (positionData.usingFilterDates)
        {
            positionData.filterDates = uploadedTable.dateFilter;
        }

        if (positionData.usingGIS)
        {
            positionData.GISCoords = GISBox;
        }

        positionData.positionsUploaded = true;
        SceneManager.LoadScene("StartMenu");
    }
}

