using System.Collections;
using UnityEngine;
using System.Data;

public class NewCSVReader : MonoBehaviour
{
    public DataTable stringTable;
    public GameObject scriptObject;

    public void Start()
    {
        StartCoroutine(CallDialog());
    }

    protected virtual IEnumerator CallDialog()
	{	
        // TODO: when file is open, the reading fails
        NewLocalFileBrowser fileDialog = this.gameObject.AddComponent<NewLocalFileBrowser>() as NewLocalFileBrowser;
        fileDialog.ReadData();

        while (!fileDialog.operationComplete)
        {
            yield return new WaitForSeconds(0.1f);
        }

        StringTable reader = new StringTable();
        stringTable = reader.parseTable(fileDialog.csvFile);

        Destroy(this.gameObject.GetComponent<NewLocalFileBrowser>());
	}
}
