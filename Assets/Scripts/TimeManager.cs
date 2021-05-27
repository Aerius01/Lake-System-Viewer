using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public static DateTime dateTimer {get; private set;}

    public float speedUpCoefficient = 10f;

    // Start is called before the first frame update
    void Start()
    {
        dateTimer = DateTime.Parse("2015-11-01 00:00:18");
    }

    // Update is called once per frame
    void Update()
    {
        dateTimer = dateTimer.AddSeconds(Time.deltaTime * speedUpCoefficient);
        // Debug.Log(String.Format("{0} {1:00}:{2:00}:{3:00}", dateTimer.Date, dateTimer.Hour, dateTimer.Minute, dateTimer.Second));
    }
}
