using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using SimpleFileBrowser;
using TMPro;

public class LocalFileBrowser : MonoBehaviour
{
	// Warning: paths returned by FileBrowser dialogs do not contain a trailing '\' character
	// Warning: FileBrowser can only show 1 dialog at a time

    // https://github.com/yasirkula/UnitySimpleFileBrowser

    [HideInInspector]
    public string csvFile;
	public GameObject objectRenderer, canvasObject, timeManagerObject, companionObject, paramsObject;

	[HideInInspector]
	public bool headers = true, colIDs = true;
	
	[HideInInspector]
	public float waterLevel;

	
	public void ReadStuff()
	{
        // Coroutine example
		StartCoroutine( ShowLoadDialogCoroutine() );
	}

	IEnumerator ShowLoadDialogCoroutine()
	{
		// Set the parameter information
		if (paramsObject.transform.Find("HeaderToggle").GetComponent<Toggle>().isOn)
		{
			headers = true;
		}
		else
		{
			headers = false;
		}

		if (paramsObject.transform.Find("ColumnIDToggle").GetComponent<Toggle>().isOn)
		{
			colIDs = true;
		}
		else
		{
			colIDs = false;
		}

		if (paramsObject.transform.Find("WaterLevelInput") != null)
		{
			if (!string.IsNullOrEmpty(paramsObject.transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text) ||
				!string.IsNullOrWhiteSpace(paramsObject.transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text))
			{
				waterLevel = float.Parse(paramsObject.transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text);
			}
			else
			{
				waterLevel = 0f;
			}
		}
		
		FileBrowser.SetFilters(true, new FileBrowser.Filter( "CSV File", ".csv" ));
		FileBrowser.SetDefaultFilter( ".csv" );

		// Add a new quick link to the browser (optional) (returns true if quick link is added successfully)
		// It is sufficient to add a quick link just once
		// Name: Users
		// Path: C:\Users
		// Icon: default (folder icon)
		FileBrowser.AddQuickLink( "Users", "C:\\Users", null );

		// Show a load file dialog and wait for a response from user
		// Load file/folder: Files, Allow multiple selection: false
		// Initial path: default (Documents), Initial filename: empty
		// Title: "Select Height Map", Submit button text: "Select"
		yield return FileBrowser.WaitForLoadDialog( FileBrowser.PickMode.Files, false, null, null, "Select CSV File", "Select" );

		if( FileBrowser.Success )
		{
			csvFile = FileBrowserHelpers.ReadTextFromFile( FileBrowser.Result[0] );

            ColorBlock cb = this.gameObject.GetComponent<Button>().colors;
            cb.normalColor = new Color(0, 1, 0, 1);
            this.gameObject.GetComponent<Button>().colors = cb;

			objectRenderer.SetActive(true);
			
			if (companionObject.GetComponent<LocalFileBrowser>().objectRenderer.activeSelf == true)
			{
				canvasObject.SetActive(false);
				timeManagerObject.SetActive(true);
			}
		}
        else
        {
            ColorBlock cb = this.gameObject.GetComponent<Button>().colors;
            cb.normalColor = new Color(1, 0, 0, 1);
            this.gameObject.GetComponent<Button>().colors = cb;
        }
	}
}