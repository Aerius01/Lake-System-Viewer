using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NavigationScript : MonoBehaviour
{
    public GameObject confirmQuitObject;

    // Start is called before the first frame update
    public void GoToMesh()
    {
        SceneManager.LoadScene("MeshUploader");
    }

    public void GoToPositionDataUploader()
    {
        SceneManager.LoadScene("PositionDataUploader");
    }

    public void GoToMetadataUploader()
    {
        SceneManager.LoadScene("MetadataUploader");
    }

    public void QuitApplication()
    {
        Application.Quit();
    }

    public void ToggleConfirmQuit()
    {
        if (confirmQuitObject.activeSelf == false)
        {
            confirmQuitObject.SetActive(true);
        }
        else
        {
            confirmQuitObject.SetActive(false);
        }
    }

    // TODO: animation to fade the background
}
