using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PositionData : NewCSVReader
{
    [HideInInspector]
    public bool positionsUploaded = false, backButton = false;
    public bool usingFilterDates = false, usingGIS = false;
    public DateTime[] filterDates;
    public Dictionary<string, float> GISCoords;

    private static PositionData _instance;
    [HideInInspector]
    public static PositionData instance {get { return _instance; } set {_instance = value; }}

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
            scriptObject.GetComponent<NavigationScript>().GoToPositionDataUploader();
        }
	}
}
