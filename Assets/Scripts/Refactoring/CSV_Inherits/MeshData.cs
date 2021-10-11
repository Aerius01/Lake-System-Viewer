using System.Collections;
using UnityEngine;

public class MeshData : NewCSVReader
{
    [HideInInspector]
    public bool meshUploaded = false, backButton = false;
    public float waterLevel;

    protected override IEnumerator CallDialog()
	{	
        yield return StartCoroutine(base.CallDialog());
        scriptObject.GetComponent<NavigationScript>().GoToMesh();
	}
}
