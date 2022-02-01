public class UserSettings
{
    public static bool showFishTags
    {
        get {return _showFishTags;}
        set {_showFishTags = value; FishGeneratorNew.ActivateAll(0, value);}
    }
    public static bool showFishDepthLines
    {
        get {return _showFishDepthLines;}
        set {_showFishDepthLines = value; FishGeneratorNew.ActivateAll(1, value);}
    }
    public static bool showFishTrails
    {
        get {return _showFishTrails;}
        set {_showFishTrails = value; FishGeneratorNew.ActivateAll(2, value);}
    }
    public static bool showThermocline 
    {
        get {return _showThermocline;} 
        set {_showThermocline = value; ThermoclineDOMain.instance.ToggleThermocline();}
    }

    private static bool _showFishTags;
    private static bool _showFishDepthLines;
    private static bool _showFishTrails;
    private static bool _showThermocline;

    public static float verticalScalingFactor;
}
