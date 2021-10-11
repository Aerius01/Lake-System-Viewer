using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class UploadUtils : MonoBehaviour
{
    public GameObject uploadMenu;
    private GameObject readers, meshReader, positionReader, metaReader;
    private MeshData meshData;
    private PositionData positionData;

    private bool meshExists = false, positionExists = false;

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
            meshData = meshReader.GetComponent<MeshData>();
            meshExists = true;
        }

        if (readers.transform.Find("PositionReader") != null)
        {
            positionReader = readers.transform.Find("PositionReader").gameObject;
            positionData = positionReader.GetComponent<PositionData>();
            positionExists = true;
        }
    }

    private void Update()
    {
        if (meshExists)
        {
            if (meshData.meshUploaded)
            {
                Button uploadButton = uploadMenu.transform.Find("Panel_1").transform.Find("UploadButton").GetComponent<Button>();

                ColorBlock cb = uploadButton.colors;
                cb.normalColor = new Color(0, 1, 0, 1);
                uploadButton.colors = cb;

                meshData.meshUploaded = false;
            }
            else if (meshData.backButton)
            {
                Button uploadButton = uploadMenu.transform.Find("Panel_1").transform.Find("UploadButton").GetComponent<Button>();

                ColorBlock cb = uploadButton.colors;
                cb.normalColor = new Color(1, 0, 0, 1);
                uploadButton.colors = cb;

                meshData.backButton = false;
            }
        }

        if (positionExists)
        {
            if (positionData.positionsUploaded)
            {
                Button uploadButton = uploadMenu.transform.Find("Panel_2").transform.Find("UploadButton").GetComponent<Button>();

                ColorBlock cb = uploadButton.colors;
                cb.normalColor = new Color(0, 1, 0, 1);
                uploadButton.colors = cb;

                positionData.positionsUploaded = false;
            }
            else if (positionData.backButton)
            {
                Button uploadButton = uploadMenu.transform.Find("Panel_2").transform.Find("UploadButton").GetComponent<Button>();

                ColorBlock cb = uploadButton.colors;
                cb.normalColor = new Color(1, 0, 0, 1);
                uploadButton.colors = cb;

                positionData.backButton = false;
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
        
        meshData = meshReader.AddComponent<MeshData>() as MeshData;
        meshData.scriptObject = this.gameObject;
    }

    public void PositionsUploadButton()
    {
        if (positionReader != null)
        {
            Destroy(positionReader);
        }

        positionReader = new GameObject();

        positionReader.transform.SetParent(readers.transform);
        positionReader.name = "PositionReader";
        
        positionData = positionReader.AddComponent<PositionData>() as PositionData;
        positionData.scriptObject = this.gameObject;
    }
}
