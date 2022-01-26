using UnityEngine;
using System;

public class ThermoclineDOMain : MonoBehaviour
{
    public ColorBar TempCB, DOCB;
    public Material planeMaterial;
    private int lastIndex, currentIndex;
    public ThermoclinePlane thermoclinePlane {get; private set;}
    private float? oldScalingFactor = null;

    private static ThermoclineDOMain _instance;
    [HideInInspector]
    public static ThermoclineDOMain instance {get { return _instance; } set {_instance = value; }}

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

    private void Start()
    {
        thermoclinePlane = new ThermoclinePlane();
        thermoclinePlane.CreatePlane(planeMaterial);
    }

    public void UpdateBars()
    {
        bool jumpingInTime = false;
        if (PlaybackController.sliderHasChanged)
        {
            jumpingInTime = true;
        }

        // Find most recent timestamp for which there is data
        currentIndex = Array.BinarySearch(LocalThermoclineData.uniqueTimeStamps, TimeManager.instance.currentTime);
        // currentIndex = Array.BinarySearch(LocalThermoclineData.uniqueTimeStamps, DateTime.Parse("2015-05-10 00:00:00"));
        if (currentIndex < 0)
        {
            currentIndex = Mathf.Abs(currentIndex) - 2;
        }

        // Only update the bars if something is different or the scaling factor has changed
        if (jumpingInTime || currentIndex != lastIndex)
        {
            TempCB.UpdateCells(currentIndex, "temp");
            DOCB.UpdateCells(currentIndex, "oxygen");

            thermoclinePlane.RecalculatePlane();
        }
        else if (oldScalingFactor != UserSettings.verticalScalingFactor)
        {
            thermoclinePlane.RecalculatePlane();
        }

        if (jumpingInTime)
        {
            PlaybackController.sliderHasChanged = false;
        }

        lastIndex = currentIndex;
        oldScalingFactor = UserSettings.verticalScalingFactor;
    }

    public int CurrentIndex()
    {
        return currentIndex;
    }
}


