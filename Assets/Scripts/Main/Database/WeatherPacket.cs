using UnityEngine;
using System;

// Support class
public class WeatherPacket
{
    public DateTime timestamp {get; private set;}
    public float? windspeed {get; private set;}
    public float? winddirection {get; private set;}
    public float? temperature {get; private set;}
    public float? humidity {get; private set;}
    public float? airpressure {get; private set;}
    public float? precipitation {get; private set;}

    public WeatherPacket(DateTime timestamp, float? windspeed, float? winddirection, float? temperature, float? humidity, float? airPressure, float? precipitation)
    {
        this.timestamp = timestamp;
        this.windspeed = windspeed;
        this.winddirection = winddirection;
        this.temperature = temperature;
        this.humidity = humidity;
        this.airpressure = airpressure;
        this.precipitation = precipitation;
    }
}