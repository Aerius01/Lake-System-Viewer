using System.Collections;
using UnityEngine;
using System.Data;

public class NewCSVReader : MonoBehaviour
{
    public DataTable stringTable;

    [HideInInspector]
    public int nullCounter;

    public GameObject navigationObject;

    void Awake()
    {
        DontDestroyOnLoad(this.transform.parent.gameObject);
    }

    public void Upload()
    {
        StartCoroutine(CallDialog());
    }

    IEnumerator CallDialog()
	{	
        // TODO: when file is open, the reading fails
        GameObject go = new GameObject();
        NewLocalFileBrowser fileDialog = go.AddComponent<NewLocalFileBrowser>() as NewLocalFileBrowser;
        fileDialog.ReadData();

        while (!fileDialog.operationComplete)
        {
            yield return new WaitForSeconds(0.1f);
        }

        StringTable reader = new StringTable();
        stringTable = reader.parseTable(fileDialog.csvFile);
        nullCounter = reader.nullCounter;

        Destroy(go);

        navigationObject.GetComponent<NavigationScript>().GoToMesh();
	}
}
