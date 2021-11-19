using System.Collections;
using UnityEngine;

public class MeshData : NewCSVReader
{
    [HideInInspector]
    public bool meshUploaded = false, backButton = false;
    public float waterLevel, maxDepth, minDepth;

    private static MeshData _instance;
    [HideInInspector]
    public static MeshData instance {get { return _instance; } set {_instance = value;}}

    private void Awake()
    {
        // Destroy duplicates instances
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    protected override IEnumerator CallDialog()
	{	
        yield return StartCoroutine(base.CallDialog());
        
        if (!failedUpload)
        {
            scriptObject.GetComponent<NavigationScript>().GoToMesh();
        }
	}
}
