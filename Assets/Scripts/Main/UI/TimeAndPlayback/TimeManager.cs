using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public DateTime currentTime {get; private set;}
    public bool paused {get; private set;}
    
    private double percentJump;

    private static TimeManager _instance;
    [HideInInspector]
    public static TimeManager instance {get { return _instance; } set {_instance = value; }}

    private void Awake()
    {
        // Destroy duplicates instances
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }

        paused = true;
    }

    public void SetBoundingDates(DateTime earliestTime, DateTime latestTime)
    {
        currentTime = earliestTime;
        percentJump = latestTime.Subtract(earliestTime).TotalHours * 0.005;
        PlaybackController.SetTickerDisplay();
    }

    // Update is called once per frame
    private void FixedUpdate()  { if (!paused) { currentTime = currentTime.AddSeconds(Time.deltaTime * UserSettings.speedUpCoefficient); }}

    // Playback controls
    public void PlayButton() { paused = false; }
    public void PauseButton() { paused = true; }
    public void SkipAhead()
    { currentTime = currentTime.AddHours(percentJump) < FishManager.latestOverallTime ? currentTime.AddHours(percentJump) : FishManager.latestOverallTime; }

    public void SkipBack()
    { currentTime = currentTime.AddHours(-percentJump) > FishManager.earliestOverallTime ? currentTime.AddHours(-percentJump) : FishManager.earliestOverallTime; }

    // All changes to time are done centrally through the time manager
    public void AddTicksToTime(long ticksDifferential) { currentTime = currentTime.AddTicks(ticksDifferential); }
}