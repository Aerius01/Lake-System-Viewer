using UnityEngine;
using System;

// Support class
public class WeatherPacket
{
    public DateTime timestamp {get; private set;}
    public DateTime? nextTimestamp {get; private set;}
    public float? windspeed {get; private set;}
    public float? windDirection {get; private set;}
    public float? temperature {get; private set;}
    public float? humidity {get; private set;}
    public float? airPressure {get; private set;}
    public float? precipitation {get; private set;}

    public WeatherPacket(DateTime timestamp, DateTime? nextTimestamp, float? windspeed, float? windDirection, float? temperature, float? humidity, float? airPressure, float? precipitation)
    {
        this.timestamp = timestamp;
        this.nextTimestamp = nextTimestamp;
        this.windspeed = windspeed;
        this.windDirection = windDirection;
        this.temperature = temperature;
        this.humidity = humidity;
        this.airPressure = airPressure;
        this.precipitation = precipitation;
    }
}