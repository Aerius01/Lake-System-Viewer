using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public static DateTime dateTimer {get; private set;}

    public float speedUpCoefficient = 10f;

    public GameObject fishManager, sunManager;

    // Start is called before the first frame update
    void Start()
    {
        dateTimer = FishDataReader.earliestTimeStamp;
        fishManager.GetComponent<FishGenerator>().SetUpFish();
    }

    // Update is called once per frame
    void Update()
    {
        dateTimer = dateTimer.AddSeconds(Time.deltaTime * speedUpCoefficient);
        fishManager.GetComponent<FishGenerator>().UpdateFish();
        sunManager.GetComponent<SunController>().AdjustSunPosition();
    }
}
