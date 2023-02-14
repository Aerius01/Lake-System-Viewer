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
            if (ThermoclineDOMain.instance != null) { ThermoclineDOMain.instance.ToggleThermocline(); }
        }
    }
    public static bool showWindWeather 
    {
        get {return _showWindWeather;} 
        set {_showWindWeather = value; if (WindWeatherMain.instance != null) { WindWeatherMain.instance.ToggleWind(); }}
    }
    // public static bool showSatelliteImage 
    // {
    //     get {return _showSatelliteImage;} 
    //     set {_showSatelliteImage = value; EnvironmentManager.ToggleSatelliteImage(value);}
    // }
    public static bool showContours 
    {
        get { return _showContours; } 
        set { _showContours = value; if (MeshManager.instance != null) { MeshManager.instance.EvaluateContours(graded:gradedContours); } }
    }
    public static bool showGradient 
    {
        get { return _showGradient; } 
        set { _showGradient = value; if (MeshManager.instance != null) { MeshManager.instance.EvaluateGradient(); } }
    }
    public static bool gradedContours 
    {
        get { return _gradedContours; } 
        set { _gradedContours = value; if (MeshManager.instance != null) { MeshManager.instance.EvaluateContours(graded:gradedContours); } }
    }
    public static bool macrophyteMaps 
    {
        get { return _macrophyteMaps; } 
        set { _macrophyteMaps = value; if (MeshManager.instance != null) { if (UserSettings.showGradient) MeshManager.instance.EvaluateGradient(); else MeshManager.instance.EvaluateContours(graded:gradedContours); } }
    }
    public static bool macrophyteHeights 
    {
        get { return _macrophyteHeights; } 
        set { _macrophyteHeights = value; if (GrassSpawner.instance != null) { GrassSpawner.instance.SpawnGrass(); } }
    }


    private static bool _showFishTags;
    private static bool _showFishDepthLines;
    private static bool _showFishTrails;
    private static bool _showThermocline;
    private static bool _showWindWeather;
    // private static bool _showSatelliteImage;
    private static bool _showContours;
    private static bool _showGradient;
    private static bool _gradedContours;
    private static bool _macrophyteMaps;
    private static bool _macrophyteHeights;
    

    public static float verticalScalingFactor = 3f;
    public static float fishScalingFactor = 1f;
    public static float speedUpCoefficient = 10f;
    public static float waterLevel = 0f;
    public static float cutoffDist = 0.1f;
}
