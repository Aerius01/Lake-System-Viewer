using UnityEngine;
using System;
using TMPro;
using System.Globalization;
using UnityEngine.UI;

public class PlaybackController : MonoBehaviour
{
    private TextMeshProUGUI timeDisplayText;
    private Slider timeControlSlider;
    private Single totalTicks;
    private bool sliderSelected;

    public static bool sliderHasChanged {
        set {
            if (value)
            {
                ThermoclineDOMain.instance.jumpingInTime = true;
                // WindMain.instance.jumpingInTime = true;
                FishManager.jumpingInTime = true;
            }
        }
    
    }

    private void Awake()
    {
        timeDisplayText = GameObject.Find("TimeDisplayText").GetComponent<TextMeshProUGUI>();
        timeControlSlider = GameObject.Find("TimeControlSlider").GetComponent<Slider>();
    }

    void Start()
    {
        timeDisplayText.text = LocalPositionData.earliestDate.ToString("G", CultureInfo.CreateSpecificCulture("de-DE"));

        sliderSelected = false;
        totalTicks = LocalPositionData.latestDate.Ticks - LocalPositionData.earliestDate.Ticks;
    }

    void FixedUpdate()
    {
        if (!TimeManager.instance.paused)
        {
            // Update time display
            timeDisplayText.text = TimeManager.instance.currentTime.ToString("G", CultureInfo.CreateSpecificCulture("de-DE"));

            // Adjust the slider value automatically if not touching it
            if (!sliderSelected)
            {
                timeControlSlider.normalizedValue = Convert.ToSingle((double)(TimeManager.instance.currentTime.Ticks - LocalPositionData.earliestDate.Ticks) / (double)totalTicks);
            }
        }
    }

    // Slider controls
    public void SliderChanged()
    {
        sliderHasChanged = true;
        long differential = (long)(timeControlSlider.normalizedValue * totalTicks) - ((long)TimeManager.instance.currentTime.Ticks - LocalPositionData.earliestDate.Ticks);
        TimeManager.instance.AddTicksToTime(differential);
    }
}
