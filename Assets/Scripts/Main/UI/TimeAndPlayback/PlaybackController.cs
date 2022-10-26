using UnityEngine;
using System;
using TMPro;
using System.Globalization;
using UnityEngine.UI;

public class PlaybackController : MonoBehaviour
{
    private static TextMeshProUGUI timeDisplayText;
    private Slider timeControlSlider;
    public static double totalTicks { get; private set; }
    private bool sliderSelected;

    private void Awake()
    {
        timeDisplayText = GameObject.Find("TimeDisplayText").GetComponent<TextMeshProUGUI>();
        timeControlSlider = GameObject.Find("TimeControlSlider").GetComponent<Slider>();
    }

    private void Start() { sliderSelected = false; }

    public static void SetTickerDisplay()
    {
        timeDisplayText.text = TimeManager.instance.currentTime.ToString("G", CultureInfo.CreateSpecificCulture("de-DE"));
        totalTicks = FishManager.latestOverallTime.Ticks - FishManager.earliestOverallTime.Ticks;
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
                timeControlSlider.normalizedValue = Convert.ToSingle((double)(TimeManager.instance.currentTime.Ticks - FishManager.earliestOverallTime.Ticks) / (double)totalTicks);
            }
        }
    }

    // Slider controls
    public void SliderDeselect()
    {
        long differential = (long)(timeControlSlider.normalizedValue * totalTicks) - ((long)TimeManager.instance.currentTime.Ticks - FishManager.earliestOverallTime.Ticks);
        TimeManager.instance.AddTicksToTime(differential);
        sliderSelected = false;
    }

    public void ChangingValue()
    {
        long differential = (long)(timeControlSlider.normalizedValue * totalTicks) - ((long)TimeManager.instance.currentTime.Ticks - FishManager.earliestOverallTime.Ticks);
        TimeManager.instance.AddTicksToTime(differential);
    }

    public void SliderSelect() { sliderSelected = true; }
}
