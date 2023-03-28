using UnityEngine;
using System;
using TMPro;
using System.Globalization;
using UnityEngine.UI;

public class PlaybackController : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI timeDisplayText;
    [SerializeField] private Slider timeControlSlider;
    public static double totalTicks { get; private set; }
    private bool sliderSelected;

    private void Start() { this.sliderSelected = false; }

    public void SetTickerDisplay()
    {
        this.timeDisplayText.text = TimeManager.instance.currentTime.ToString("G", CultureInfo.CreateSpecificCulture("de-DE"));
        PlaybackController.totalTicks = FishManager.latestOverallTime.Ticks - FishManager.earliestOverallTime.Ticks;
    }

    void Update()
    {
        if (!TimeManager.instance.paused)
        {
            // Update time display
            this.timeDisplayText.text = TimeManager.instance.currentTime.ToString("G", CultureInfo.CreateSpecificCulture("de-DE"));

            // Adjust the slider value automatically if not touching it
            if (!this.sliderSelected)
            {
                this.timeControlSlider.normalizedValue = Convert.ToSingle((double)(TimeManager.instance.currentTime.Ticks - FishManager.earliestOverallTime.Ticks) / (double)totalTicks);
            }
        }
    }

    // Slider controls
    public void SliderDeselect()
    {
        long differential = (long)(this.timeControlSlider.normalizedValue * totalTicks) - ((long)TimeManager.instance.currentTime.Ticks - FishManager.earliestOverallTime.Ticks);
        TimeManager.instance.AddTicksToTime(differential);
        this.sliderSelected = false;
    }

    public void ChangingValue()
    {
        long differential = (long)(this.timeControlSlider.normalizedValue * totalTicks) - ((long)TimeManager.instance.currentTime.Ticks - FishManager.earliestOverallTime.Ticks);
        TimeManager.instance.AddTicksToTime(differential);
    }

    public void SliderSelect() { this.sliderSelected = true; }
}
