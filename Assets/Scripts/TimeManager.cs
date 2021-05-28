using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public static DateTime dateTimer {get; private set;}

    public static float speedUpCoefficient = 10f;

    // Start is called before the first frame update
    public static void StartTime(DateTime startTime)
    {
        dateTimer = startTime;
    }

    // Update is called once per frame
    public static void UpdateTime()
    {
        dateTimer = dateTimer.AddSeconds(Time.deltaTime * speedUpCoefficient);
        // Debug.Log(String.Format("{0} {1:00}:{2:00}:{3:00}", dateTimer.Date, dateTimer.Hour, dateTimer.Minute, dateTimer.Second));
    }
}
