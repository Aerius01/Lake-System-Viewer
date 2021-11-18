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
    public static bool sliderHasChanged = false;

    private void Awake()
    {
        timeDisplayText = GameObject.Find("TimeDisplayText").GetComponent<TextMeshProUGUI>();
        timeControlSlider = GameObject.Find("TimeControlSlider").GetComponent<Slider>();
    }

    void Start()
    {
        timeDisplayText.text = PositionData.instance.earliestDate.ToString("G", CultureInfo.CreateSpecificCulture("de-DE"));

        sliderSelected = false;
        totalTicks = PositionData.instance.latestDate.Ticks - PositionData.instance.earliestDate.Ticks;
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
                timeControlSlider.normalizedValue = Convert.ToSingle((double)(TimeManager.instance.currentTime.Ticks - PositionData.instance.earliestDate.Ticks) / (double)totalTicks);
            }
        }
    }

    // Slider controls
    public void SliderChanged()
    {
        if (sliderSelected)
        {
            sliderHasChanged = true;
            long differential = (long)(timeControlSlider.normalizedValue * totalTicks) - ((long)TimeManager.instance.currentTime.Ticks - PositionData.instance.latestDate.Ticks);
            TimeManager.instance.AddTicksToTime(differential);
        }
    }

    public void SliderSelected()
    {
        sliderSelected = true;
    }

    public void SliderDeselected()
    {
        sliderSelected = false;
    }
}
