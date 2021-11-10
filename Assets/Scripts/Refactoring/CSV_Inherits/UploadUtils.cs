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

        if (MeshData.instance != null)
        {
            meshReader = MeshData.instance.gameObject;
            meshData = MeshData.instance;
            meshExists = true;
        }

        if (PositionData.instance != null)
        {
            positionReader = PositionData.instance.gameObject;
            positionData = PositionData.instance;
            positionExists = true;
        }
    }

    private void Start()
    {
        // if an existing reader comes back successfully, change button color
        if (meshExists)
        {
            if (meshData.meshUploaded)
            {
                Button uploadButton = uploadMenu.transform.Find("Panel_1").transform.Find("UploadButton").GetComponent<Button>();

                ColorBlock cb = uploadButton.colors;
                cb.normalColor = new Color(0, 1, 0, 1);
                uploadButton.colors = cb;
            }
            else if (meshData.backButton)
            {
                Button uploadButton = uploadMenu.transform.Find("Panel_1").transform.Find("UploadButton").GetComponent<Button>();

                ColorBlock cb = uploadButton.colors;
                cb.normalColor = new Color(1, 0, 0, 1);
                uploadButton.colors = cb;
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
            }
            else if (positionData.backButton)
            {
                Button uploadButton = uploadMenu.transform.Find("Panel_2").transform.Find("UploadButton").GetComponent<Button>();

                ColorBlock cb = uploadButton.colors;
                cb.normalColor = new Color(1, 0, 0, 1);
                uploadButton.colors = cb;
            }
        }
    }

    private void Update()
    {
        if (meshExists)
        {
            if (meshData.failedUpload)
            {
                Button uploadButton = uploadMenu.transform.Find("Panel_1").transform.Find("UploadButton").GetComponent<Button>();

                ColorBlock cb = uploadButton.colors;
                cb.normalColor = new Color(1, 0, 0, 1);
                uploadButton.colors = cb;
            }
        }

        if (positionExists)
        {
            if (positionData.failedUpload)
            {
                Button uploadButton = uploadMenu.transform.Find("Panel_2").transform.Find("UploadButton").GetComponent<Button>();

                ColorBlock cb = uploadButton.colors;
                cb.normalColor = new Color(1, 0, 0, 1);
                uploadButton.colors = cb;
            }
        }
    }

    public void MeshUploadButton()
    {
        if (meshReader != null)
        {
            MeshData.instance = null;
            Destroy(meshReader);
        }

        meshReader = new GameObject();

        meshReader.transform.SetParent(readers.transform);
        meshReader.name = "MeshReader";
        
        meshData = meshReader.AddComponent<MeshData>() as MeshData;
        meshData.scriptObject = this.gameObject;
        meshExists = true;
    }

    public void PositionsUploadButton()
    {
        if (positionReader != null)
        {
            PositionData.instance = null;
            Destroy(positionReader);
        }

        positionReader = new GameObject();

        positionReader.transform.SetParent(readers.transform);
        positionReader.name = "PositionReader";
        
        positionData = positionReader.AddComponent<PositionData>() as PositionData;
        positionData.scriptObject = this.gameObject;
        positionExists = true;
    }
}
