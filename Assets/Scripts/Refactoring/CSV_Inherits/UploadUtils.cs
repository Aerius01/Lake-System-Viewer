using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UploadUtils : MonoBehaviour
{
    public GameObject uploadMenu;

    private GameObject readers, meshReader, positionReader, metaReader;

    private MeshClass meshClass;

    private bool meshExists = false;

    private void Awake()
    {
        if (GameObject.Find("Readers") == null)
        {
            readers = new GameObject();
            readers.name = "Readers";
            DontDestroyOnLoad(readers);
        }
        else
        {
            readers = GameObject.Find("Readers").gameObject;
        }

        if (readers.transform.Find("MeshReader") != null)
        {
            meshReader = readers.transform.Find("MeshReader").gameObject;
            meshClass = meshReader.GetComponent<MeshClass>();
            meshExists = true;
        }
    }

    private void Update()
    {
        if (meshExists)
        {
            if (meshClass.meshUploaded)
            {
                Button uploadButton = uploadMenu.transform.Find("Panel_1").transform.Find("UploadButton").GetComponent<Button>();

                ColorBlock cb = uploadButton.colors;
                cb.normalColor = new Color(0, 1, 0, 1);
                uploadButton.colors = cb;

                meshClass.meshUploaded = false;
            }
            else if (meshClass.backButton)
            {
                Button uploadButton = uploadMenu.transform.Find("Panel_1").transform.Find("UploadButton").GetComponent<Button>();

                ColorBlock cb = uploadButton.colors;
                cb.normalColor = new Color(1, 0, 0, 1);
                uploadButton.colors = cb;

                meshClass.backButton = false;
            }
        }

        // if an existing reader comes back successfully, change button color
    }

    public void MeshUploadButton()
    {
        if (meshReader != null)
        {
            Destroy(meshReader);
        }

        meshReader = new GameObject();

        meshReader.transform.SetParent(readers.transform);
        meshReader.name = "MeshReader";
        
        meshClass = meshReader.AddComponent<MeshClass>() as MeshClass;
        meshClass.scriptObject = this.gameObject;
    }
}
