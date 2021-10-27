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


    protected override IEnumerator CallDialog()
	{	
        yield return StartCoroutine(base.CallDialog());
        scriptObject.GetComponent<NavigationScript>().GoToPositionDataUploader();
	}
}
