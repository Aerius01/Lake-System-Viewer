using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Linq;
using UnityEngine.SceneManagement;

public class MeshUploader : MonoBehaviour
{
    public GameObject cellPreFab, contentPanel, paramsPanel;
    private MeshUploadTable uploadedTable;
    private ViewPort viewPort;
    private bool continueColoring = false;

    // Start is called before the first frame update
    private void Start()
    {
        MeshData.instance.meshUploaded = MeshData.instance.backButton = false;
        
        uploadedTable = new MeshUploadTable(MeshData.instance.stringTable.Copy(), paramsPanel);
        viewPort = new ViewPort(uploadedTable, contentPanel);
        uploadedTable.viewPort = viewPort;

        FillViewPort();
    }

    private void FillViewPort()
    {
        // Get the uploaded file as a DataTable
        viewPort.SetGridParams();
        List<GameObject> listOfObjects = new List<GameObject>();

        for (int i = 0; i < viewPort.Rows; i++)
        {
            for (int j = 0; j < viewPort.Columns; j++)
            {
                GameObject go = (Instantiate (cellPreFab) as GameObject);
                go.transform.SetParent(contentPanel.transform);
                go.transform.Find("Text").GetComponent<TextMeshProUGUI>().text = string.Format("{0}", uploadedTable.uploadTable.Rows[i].ItemArray[j]);

                listOfObjects.Add(go);
            }
        }

        viewPort.listOfObjects = listOfObjects;

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

            ToggleNullFrame();

            if (uploadedTable.resetComplete)
            {
                // Update only if we've reset the params successfully
                paramsPanel.transform.Find("StatisticsFrame").transform.Find("TextContainer").transform.
                    Find("Text (TMP)").GetComponent<TextMeshProUGUI>().text =
                    string.Format("Max Depth: {0: 0.00}\nMin Depth: {1: 0.00}\n# Columns: {2}\n# Rows: {3}\n# Null/NaNs: {4}",
                    uploadedTable.maxDepth, uploadedTable.minDepth, uploadedTable.adjustedColumnCount, uploadedTable.adjustedRowCount, uploadedTable.nullCount);

                uploadedTable.containerCG.alpha = 1f;
                uploadedTable.loadingIcon.SetActive(false);
            }
            else if (uploadedTable.stopThread)
            {
                // if we've commanded the thread to stop, reset the UI
                uploadedTable.containerCG.alpha = 1f;
                uploadedTable.loadingIcon.SetActive(false);
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
    }

    private void ToggleNullFrame()
    {
        CanvasGroup nullcontent = paramsPanel.transform.Find("NullFrame").transform.Find("ToggleGroup").gameObject.GetComponent<CanvasGroup>();
        TextMeshProUGUI textContent = paramsPanel.transform.Find("NullFrame").transform.Find("WarningText").gameObject.GetComponent<TextMeshProUGUI>();
        if (uploadedTable.nullCount > 0)
        {
            nullcontent.alpha = 1;
            nullcontent.interactable = true;
            
            textContent.text = "!! Null or NaN values detected";
            textContent.color = new Color(255f, 0f, 0f, 1f);
        }
        else
        {
            nullcontent.alpha = 0;
            nullcontent.interactable = false;

            textContent.text = "No Null or NaN values detected";
            textContent.color = new Color(0f, 255f, 0f, 1f);
        }
    }

    public void BackButton()
    {
        MeshData mesh = GameObject.Find("MeshReader").GetComponent<MeshData>();
        mesh.backButton = true;

        SceneManager.LoadScene("StartMenu");
    }

    public void ExitChecks()
    {
        GameObject instructionPanel = contentPanel.transform.parent.transform.parent.Find("InstructionPanel").gameObject;
        GameObject activeToggle = paramsPanel.transform.Find("NullFrame").transform.Find("ToggleGroup").GetComponent<ToggleGroup>().ActiveToggles().FirstOrDefault().gameObject;
                            
        if (!viewPort.currentClickList.Any())
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
            // Modify the MeshData table based on selection & nulls
            MeshData mesh = GameObject.Find("MeshReader").GetComponent<MeshData>();

            if (activeToggle.name == "Toggle_1")
            {
                uploadedTable.toggleID = 1;
                uploadedTable.SetTable(viewPort.currentClickList);
            }
            else if (activeToggle.name == "Toggle_2")
            {
                uploadedTable.toggleID = 2;
                uploadedTable.replacementVal = float.Parse(activeToggle.transform.Find("Input").GetComponent<TMP_InputField>().text);
                uploadedTable.SetTable(viewPort.currentClickList);
            }
            else
            {
                uploadedTable.SetTable(viewPort.currentClickList);
            }

            mesh.stringTable = uploadedTable.uploadTable;
            mesh.waterLevel = float.Parse(paramsPanel.transform.Find("ParametersFrame").transform.Find("WaterLevelFrame").transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text);
            mesh.rowCount = mesh.stringTable.Rows.Count;
            mesh.columnCount = mesh.stringTable.Columns.Count;
            mesh.maxDepth = uploadedTable.maxDepth;
            mesh.minDepth = uploadedTable.minDepth;
            mesh.meshUploaded = true;
            SceneManager.LoadScene("StartMenu");
        }
    }
}
