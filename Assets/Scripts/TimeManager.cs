using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using TMPro;
using System.Globalization;
using UnityEngine.UI;

public class TimeManager : MonoBehaviour
{
    public static DateTime dateTimer {get; private set;}

    public float speedUpCoefficient = 10f;

    private float speedUpCoefficientHolder;

    public GameObject fishManager, sunManager, timeDisplayObject, timeSliderObject;

    [HideInInspector]
    public Slider slider;

    private Single totalTicks;
    private bool sliderSelect;

    // Start is called before the first frame update
    void Start()
    {
        dateTimer = FishDataReader.earliestTimeStamp;
        timeDisplayObject.GetComponent<TextMeshProUGUI>().text = dateTimer.ToString("G", CultureInfo.CreateSpecificCulture("de-DE"));

        slider = timeSliderObject.GetComponent<Slider>();
        sliderSelect = false;
        totalTicks = (FishDataReader.latestTimeStamp - FishDataReader.earliestTimeStamp).Ticks;

        fishManager.GetComponent<FishGenerator>().SetUpFish();
    }

    // Update is called once per frame
    void Update()
    {
        dateTimer = dateTimer.AddSeconds(Time.deltaTime * speedUpCoefficient);
        timeDisplayObject.GetComponent<TextMeshProUGUI>().text = dateTimer.ToString("G", CultureInfo.CreateSpecificCulture("de-DE"));

        if (!sliderSelect)
        {
           // dynamically adjust the slider value
           slider.normalizedValue = Convert.ToSingle((double)(dateTimer - FishDataReader.earliestTimeStamp).Ticks / (double)totalTicks);
        }
        
        fishManager.GetComponent<FishGenerator>().UpdateFish();
        sunManager.GetComponent<SunController>().AdjustSunPosition();
    }

    // Slider controls
    public void ChangeSlider()
    {
        dateTimer = FishDataReader.earliestTimeStamp.AddTicks((long)(slider.normalizedValue * totalTicks));
    }

    public void SliderSelected()
    {
        sliderSelect = true;
    }

    public void SliderDeselect()
    {
        sliderSelect = false;
    }

    // Playback controls
    public void PlayButton()
    {
        speedUpCoefficient = speedUpCoefficientHolder;
    }

    public void PauseButton()
    {
        if (speedUpCoefficient != 0f)
        {
            speedUpCoefficientHolder = speedUpCoefficient;
        }
        speedUpCoefficient = 0f;
    }
}
