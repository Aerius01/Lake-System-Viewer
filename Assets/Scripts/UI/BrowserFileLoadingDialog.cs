using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections;
using UnityEngine.Networking;
using System.Runtime.InteropServices;
public class BrowserFileLoadingDialog : MonoBehaviour
{
    // THIS IS FOR THE WEBGL BUILD
    public Text UrlTextField;

    [DllImport("__Internal")] private static extern void FileUploaderInit();

    void Start()
    {
        FileUploaderInit();
    }

    public void FileDialogResult(string fileUrl)
    {
        StartCoroutine(LoadBlob(fileUrl));
    }

    IEnumerator LoadBlob(string url)
    {
        Debug.Log(url);
        UnityWebRequest webRequest = UnityWebRequest.Get(url);
        yield return webRequest.SendWebRequest();
        Debug.Log("Done");

        if (webRequest.result != UnityWebRequest.Result.ConnectionError && webRequest.result != UnityWebRequest.Result.ProtocolError)
        {
        // Get text content like this:
        // Debug.Log(webRequest.downloadHandler.text);

        Debug.Log(webRequest.downloadHandler.text.GetType());
        Debug.Log(webRequest.downloadHandler.text.Length);

        }
    }
}