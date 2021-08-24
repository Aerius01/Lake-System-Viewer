using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NavigationScript : MonoBehaviour
{
    public GameObject confirmQuitObject, fadingMenu;

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

    public void FadePanel()
    {
        if (fadingMenu != null)
        {
            Animator animator = fadingMenu.GetComponent<Animator>();
            if(animator != null)
            {
                bool triggerActive = animator.GetBool("Activated");
                animator.SetBool("Activated", !triggerActive);
            }
        }
    }
}
