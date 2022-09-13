public class UserSettings
{
    public static bool showFishTags
    {
        get {return _showFishTags;}
        set {_showFishTags = value; FishManager.ActivateAllTags(value);}
    }
    public static bool showFishDepthLines
    {
        get {return _showFishDepthLines;}
        set 
        {
            _showFishDepthLines = value;
            FishManager.ActivateAllDepths(value);
            if (UserSettings.showFishDepthLines && UserSettings.showThermocline) { if (!UserSettings.showThermoBobs) UserSettings.showThermoBobs = true; }
            else { if (UserSettings.showThermoBobs) UserSettings.showThermoBobs = false; }
        }
    }
    public static bool showFishTrails
    {
        get {return _showFishTrails;}
        set {_showFishTrails = value; FishManager.ActivateAllTrails(value);}
    }
    public static bool showThermocline 
    {
        get {return _showThermocline;} 
        set 
        {
            _showThermocline = value; 
            ThermoclineDOMain.instance.ToggleThermocline();
            if (UserSettings.showFishDepthLines && UserSettings.showThermocline) { if (!UserSettings.showThermoBobs) UserSettings.showThermoBobs = true; }
            else { if (UserSettings.showThermoBobs) UserSettings.showThermoBobs = false; }
        }
    }
    public static bool showThermoBobs
    {
        get {return _showThermoBobs;}
        set {_showThermoBobs = value; FishManager.ActivateAllThermoBobs(value);}
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

    private static bool _showFishTags;
    private static bool _showFishDepthLines;
    private static bool _showFishTrails;
    private static bool _showThermocline;
    private static bool _showThermoBobs;
    private static bool _showWindWeather;
    private static bool _showSatelliteImage;

    public static float verticalScalingFactor = 3f;
    public static float fishScalingFactor = 1f;
    public static float speedUpCoefficient = 10f;
    public static float waterLevel = 0f;
    public static float cutoffDist = 0.1f;

}
