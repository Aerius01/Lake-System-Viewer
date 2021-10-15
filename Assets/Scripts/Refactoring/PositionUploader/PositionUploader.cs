using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

public class PositionUploader : MonoBehaviour
{
    public GameObject cellPreFab, dropdownPrefab, contentPanel, paramsPanel;
    private List<int> currentClickList;
    private PositionUploadTable uploadedTable;
    private ViewPort viewPort;
    private bool continueColoring = false;

    private Toggle dateTimeToggle;

    // Start is called before the first frame update
    private void Start()
    {
        PositionData reader = GameObject.Find("PositionReader").GetComponent<PositionData>();

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

        GameObject instructionPanel = contentPanel.transform.parent.transform.parent.Find("InstructionPanel").gameObject;
        instructionPanel.transform.Find("Image").transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "Please select the upper left corner of your data, excluding headers and ID columns.";
        instructionPanel.GetComponent<FadeCanvasGroup>().Fade(4f);
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
                GameObject instructionPanel = contentPanel.transform.parent.transform.parent.Find("InstructionPanel").gameObject;

                instructionPanel.transform.Find("Image").transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "Format Exception: At least one entry in your selection could not be converted into a decimal number" +
                    ", are you sure you've removed all headers and columns in your selection?";
                instructionPanel.GetComponent<FadeCanvasGroup>().Fade(6f);
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
        Toggle dateTimeToggle = paramsPanel.transform.Find("ParametersFrame").transform.Find("DateTimeFrame").transform.Find("Toggle_1").GetComponent<Toggle>();

        if (!viewPort.ColumnReqsSatisfied())
        {
            // throw error
        }
        else if (dateTimeToggle.isOn)
        {
            if (PositionUploadTable.startCutoff == null || PositionUploadTable.endCutoff == null)
            {
                // throw error
            }
            else if (DateTime.Compare(PositionUploadTable.startCutoff, PositionUploadTable.endCutoff) > 0)
            {
                // throw error
            }
            else
            {
                PositionUploadTable.applyDateFilter = true;
            }
        }
        else // if, the date filter can be in this next block as it already is, regardless of the content of that block
        {
            PositionUploadTable.applyDateFilter = false;
        }

        // GIS coords form a box
        // viewport Column formatting checks as optional --> proceed and delete format-erroneous rows, or cancel.


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
        else if (uploadedTable.nullCount > 0 && activeToggle.name == "Toggle_2" && string.IsNullOrEmpty(activeToggle.transform.Find("Input").GetComponent<TMP_InputField>().text))
        {
            // Ensure a replacement value has been entered
            instructionPanel.transform.Find("Image").transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text = "You've selected 'replacement' null handling but specified no value. Please specify a null/NaN replacment value";
            instructionPanel.GetComponent<FadeCanvasGroup>().Fade(6f);
        }
        else
        {   
            // Successful checks
            // Modify the PositionData table based on selection & nulls
            PositionData positionData = GameObject.Find("PositionReader").GetComponent<PositionData>();

            // if (activeToggle.name == "Toggle_1")
            // {
            //     uploadedTable.SetTable(currentClickList, 1);
            // }
            // else if (activeToggle.name == "Toggle_2")
            // {
            //     float replacementVal = float.Parse(activeToggle.transform.Find("Input").GetComponent<TMP_InputField>().text);
            //     uploadedTable.SetTable(currentClickList, 2, replacementVal);
            // }
            // else
            // {
            //     uploadedTable.SetTable(currentClickList);
            // }

            positionData.stringTable = uploadedTable.uploadTable;
            // positionData.waterLevel = float.Parse(paramsPanel.transform.Find("ParametersFrame").transform.Find("WaterLevelFrame").transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text);
            positionData.positionsUploaded = true;
            SceneManager.LoadScene("StartMenu");
        }
    }
}

