using UnityEngine;
public class UserSettings
{
    public static bool showFishTags
    {
        get {return _showFishTags;}
        set {_showFishTags = value; FishManager.ActivateAllTags(value, TimeManager.instance.currentTime);}
    }
    public static bool showFishDepthLines
    {
        get {return _showFishDepthLines;}
        set 
        {
            _showFishDepthLines = value;
            FishManager.ActivateAllDepths(value, TimeManager.instance.currentTime);
        }
    }
    public static bool showFishTrails
    {
        get {return _showFishTrails;}
        set {_showFishTrails = value; FishManager.ActivateAllTrails(value, TimeManager.instance.currentTime);}
    }
    public static bool showThermocline 
    {
        get {return _showThermocline;} 
        set 
        {
            _showThermocline = value; 
            ThermoclineDOMain.instance.ToggleThermocline();
        }
    }
    public static bool showWindWeather 
    {
        get {return _showWindWeather;} 
        set {_showWindWeather = value; WindWeatherMain.instance.ToggleWind();}
    }
    public static bool showSatelliteImage 
    {
        get {return _showSatelliteImage;} 
        set {_showSatelliteImage = value; EnvironmentManager.ToggleSatelliteImage(value);}
    }
    public static bool showContours 
    {
        get { return _showContours; } 
        set { _showContours = value; MeshManager.instance.EvaluateContours(graded:gradedContours); }
    }
    public static bool showGradient 
    {
        get { return _showGradient; } 
        set { _showGradient = value; MeshManager.instance.EvaluateGradient(); }
    }
    public static bool gradedContours 
    {
        get { return _gradedContours; } 
        set { _gradedContours = value; MeshManager.instance.EvaluateContours(graded:gradedContours); }
    }

    private static bool _showFishTags;
    private static bool _showFishDepthLines;
    private static bool _showFishTrails;
    private static bool _showThermocline;
    private static bool _showWindWeather;
    private static bool _showSatelliteImage;
    private static bool _showContours;
    private static bool _showGradient;
    private static bool _gradedContours;

    public static float verticalScalingFactor = 3f;
    public static float fishScalingFactor = 1f;
    public static float speedUpCoefficient = 10f;
    public static float waterLevel = 0f;
    public static float cutoffDist = 0.1f;

}
