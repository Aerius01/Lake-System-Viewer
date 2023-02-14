using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public DateTime currentTime {get; private set;}
    public bool paused {get; private set;}
    
    private double percentJump;
    private PlaybackController playbackController;

    private static TimeManager _instance;
    [HideInInspector]
    public static TimeManager instance {get { return _instance; } set {_instance = value; }}

    private void Awake()
    {
        // Destroy duplicates instances
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }

        paused = true;
        this.playbackController = this.gameObject.GetComponent<PlaybackController>();
    }

    public void SetBoundingDates(DateTime earliestTime, DateTime latestTime)
    {
        currentTime = earliestTime;
        percentJump = latestTime.Subtract(earliestTime).TotalHours * 0.005;
        this.playbackController.SetTickerDisplay();
    }

    // Update is called once per frame
    private void FixedUpdate()
    { 
        if (!paused) 
        { 
            // If adding an increment of time would push the timer over the date limits of the fish data, then freeze time.
            if (DateTime.Compare(currentTime.AddSeconds(Time.deltaTime * UserSettings.speedUpCoefficient), FishManager.latestOverallTime) > 0) currentTime = FishManager.latestOverallTime;
            if (DateTime.Compare(currentTime.AddSeconds(Time.deltaTime * UserSettings.speedUpCoefficient), FishManager.earliestOverallTime) < 0) currentTime = FishManager.earliestOverallTime;
            else currentTime = currentTime.AddSeconds(Time.deltaTime * UserSettings.speedUpCoefficient); 
        }
    }

    // Playback controls
    public void PlayButton() { paused = false; }
    public void PauseButton() { paused = true; }
    public void SkipAhead() { currentTime = DateTime.Compare(currentTime.AddHours(percentJump), FishManager.latestOverallTime) > 0 ? FishManager.latestOverallTime : currentTime.AddHours(percentJump); }
    public void SkipBack() { currentTime = DateTime.Compare(currentTime.AddHours(-percentJump), FishManager.earliestOverallTime) < 0 ? FishManager.earliestOverallTime : currentTime.AddHours(-percentJump); }

    // All changes to time are done centrally through the time manager
    public void AddTicksToTime(long ticksDifferential) { currentTime = currentTime.AddTicks(ticksDifferential); }
}