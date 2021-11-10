using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Threading;
using System.Data;

public class NewCSVReader : MonoBehaviour
{
    public DataTable stringTable;
    public GameObject scriptObject;
    public int rowCount, columnCount;
    public bool failedUpload;

    public void Start()
    {
        failedUpload = false;
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

        if (fileDialog.success)
        {
            CanvasGroup loadingIcon = GameObject.Find("Pulsing 5").GetComponent<CanvasGroup>() as CanvasGroup;
            loadingIcon.alpha = 1f;

            // Put the data read operation into its own thread
            StringTable reader = new StringTable();
            Thread readingThread = new Thread(() => stringTable = reader.parseTable(fileDialog.csvFile));
            readingThread.Start();

            // Disable all buttons while loading data
            Button[] buttons = GameObject.FindObjectsOfType<Button>();
            foreach (Button button in buttons)
            {
                button.interactable = false;
            }

            // Wait for data load to be complete
            while (!reader.parsingComplete)
            {
                yield return new WaitForSeconds(0.1f);
            }
            
            // Join the thread if it hasn't automatically joined
            if (readingThread.IsAlive)
            {
                readingThread.Join();
            }
        }
        else
        {
            failedUpload = true;
        }

        Destroy(this.gameObject.GetComponent<NewLocalFileBrowser>());
	}
}
