using UnityEngine;
using System;

public class TimeManager : MonoBehaviour
{
    public DateTime currentTime {get; private set;}
    public bool paused {get; private set;}
    public float speedUpCoefficient = 10f;
    
    private double percentJump;

    private static TimeManager _instance;
    [HideInInspector]
    public static TimeManager instance {get { return _instance; } set {_instance = value; }}

    private void Awake()
    {
        // Destroy duplicates instances
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    // Start is called before the first frame update
    private void Start()
    {
        currentTime = LocalPositionData.earliestDate;
        paused = true;
        percentJump = LocalPositionData.latestDate.Subtract(LocalPositionData.earliestDate).TotalHours * 0.005;
    }

    // Update is called once per frame
    private void FixedUpdate()
    {
        if (!paused)
        {
            currentTime = currentTime.AddSeconds(Time.deltaTime * speedUpCoefficient);
        }
    }

    // Playback controls
    public void PlayButton()
    {
        paused = false;
    }

    public void PauseButton()
    {
        paused = true;
    }

    public void SkipAhead()
    {
        currentTime = currentTime.AddHours(percentJump);
    }

    public void SkipBack()
    {
        currentTime = currentTime.AddHours(-percentJump);
    }

    // All changes to time are done centrally through the time manager
    public void AddTicksToTime(long ticksDifferential)
    {
        currentTime = currentTime.AddTicks(ticksDifferential);
    }
}