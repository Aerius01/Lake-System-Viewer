using UnityEngine;
using System;

public class ThermoclineDOMain : MonoBehaviour
{
    public ColorBar TempCB, DOCB;

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
        // jumps in time not necessary as there is no interpolation over time (for now)

        // Find most recent timestamp for which there is data
        int currentIndex = Array.BinarySearch(LocalThermoclineData.uniqueTimeStamps, TimeManager.instance.currentTime);
        if (currentIndex < 0)
        {
            if (currentIndex == - LocalThermoclineData.uniqueTimeStamps.Length)
            {
                // Beyond the scope of our timestamps
            }
            else
            {
                currentIndex = Mathf.Abs(currentIndex) - 1;
            }
        }

        // TODO: if no change in TS, don't update, lot's of efficiency here
        TempCB.UpdateCells(currentIndex, "temp");
        // DOCB.UpdateCells(currentIndex, "oxygen");
    }
}
