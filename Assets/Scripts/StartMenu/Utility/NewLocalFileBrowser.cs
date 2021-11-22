using System.Collections;
using SimpleFileBrowser;
using UnityEngine;

public class NewLocalFileBrowser : MonoBehaviour
{
    // https://github.com/yasirkula/UnitySimpleFileBrowser

    [HideInInspector]
    public string csvFile;
	public bool operationComplete = false, success = false;
	
	public void ReadData()
	{
        // Coroutine example
		StartCoroutine( ShowLoadDialogCoroutine() );
	}

	IEnumerator ShowLoadDialogCoroutine()
	{	
		FileBrowser.SetFilters(true, new FileBrowser.Filter( "CSV File", ".csv" ));
		FileBrowser.SetDefaultFilter( ".csv" );
		FileBrowser.AddQuickLink( "Users", "C:\\Users", null );
		yield return FileBrowser.WaitForLoadDialog( FileBrowser.PickMode.Files, false, null, null, "Select CSV File", "Select" );

		if( FileBrowser.Success )
		{
			csvFile = FileBrowserHelpers.ReadTextFromFile( FileBrowser.Result[0] );
			success = true;
		}
		else
		{
			success = false;
		}

		operationComplete = true;
	}
}