using System.Collections;
using UnityEngine;

public class UploadUtils : MonoBehaviour
{
    private GameObject readers;

    private GameObject meshReader, positionReader, metaReader;

    private MeshClass meshClass;

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
        }
    }

    private void Update()
    {
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
