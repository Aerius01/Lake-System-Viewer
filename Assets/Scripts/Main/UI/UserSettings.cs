public class UserSettings
{
    public static bool showFishTags
    {
        get {return _showFishTags;}
        set {_showFishTags = value; FishManager.ActivateAll("tag", value);}
    }
    public static bool showFishDepthLines
    {
        get {return _showFishDepthLines;}
        set {_showFishDepthLines = value; FishManager.ActivateAll("line", value);}
    }
    public static bool showFishTrails
    {
        get {return _showFishTrails;}
        set {_showFishTrails = value; FishManager.ActivateAll("trail", value);}
    }
    public static bool showThermocline 
    {
        get {return _showThermocline;} 
        set {_showThermocline = value; ThermoclineDOMain.instance.ToggleThermocline();}
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
    private static bool _showWindWeather;
    private static bool _showSatelliteImage;

    public static float verticalScalingFactor = 3f;
    public static float fishScalingFactor = 25f;
    public static float speedUpCoefficient = 10f;

}
