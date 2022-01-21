using UnityEngine;
using System;

public class ThermoclineDOMain : MonoBehaviour
{
    public ColorBar TempCB, DOCB;
    private int lastIndex;

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

    public void UpdateBars()
    {
        bool jumpingInTime = false;
        if (PlaybackController.sliderHasChanged)
        {
            jumpingInTime = true;
        }

        // Find most recent timestamp for which there is data
        int currentIndex = Array.BinarySearch(LocalThermoclineData.uniqueTimeStamps, TimeManager.instance.currentTime);
        if (currentIndex < 0)
        {
            currentIndex = Mathf.Abs(currentIndex) - 1;
        }

        // Only update the bars if something is different
        if (jumpingInTime || currentIndex != lastIndex)
        {
            TempCB.UpdateCells(currentIndex, "temp");
            DOCB.UpdateCells(currentIndex, "oxygen");
        }

        if (jumpingInTime)
        {
            PlaybackController.sliderHasChanged = false;
        }

        lastIndex = currentIndex;
    }
}
