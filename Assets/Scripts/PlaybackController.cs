using UnityEngine;
using System;
using TMPro;
using System.Globalization;
using UnityEngine.UI;

public class PlaybackController : MonoBehaviour
{
    private string timeDisplayText;
    private Slider timeControlSlider;
    private Single totalTicks;
    private bool sliderSelected;
    public static bool sliderHasChanged = false;

    private void Awake()
    {
        timeDisplayText = GameObject.Find("TimeDisplayText").GetComponent<TextMeshProUGUI>().text;
        timeControlSlider = GameObject.Find("TimeControlSlider").GetComponent<Slider>();
    }

    void Start()
    {
        timeDisplayText = PositionData.instance.earliestDate.ToString("G", CultureInfo.CreateSpecificCulture("de-DE"));

        sliderSelected = false;
        totalTicks = (PositionData.instance.latestDate - PositionData.instance.earliestDate).Ticks;
    }

    void FixedUpdate()
    {
        if (!TimeManager.instance.paused)
        {
            // Update time display
            timeDisplayText = TimeManager.instance.currentTime.ToString("G", CultureInfo.CreateSpecificCulture("de-DE"));

            // Adjust the slider value automatically if not touching it
            if (!sliderSelected)
            {
                timeControlSlider.normalizedValue = Convert.ToSingle((double)(TimeManager.instance.currentTime - PositionData.instance.earliestDate).Ticks / (double)totalTicks);
            }
        }
    }

    // Slider controls
    public void SliderChanged()
    {
        if (sliderSelected)
        {
            sliderHasChanged = true;
            TimeManager.instance.AddTicksToTime((long)(timeControlSlider.normalizedValue * totalTicks));
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
