using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;
using System.Globalization;
using UnityEngine.UI;

public class UserSettings
{
    public static bool showFishTags {get {return _showFishTags;} set {_showFishTags = value; FishGeneratorNew.ActivateAll(0, value);}}
    public static bool showFishDepthLines {get {return _showFishDepthLines;} set {_showFishDepthLines = value; FishGeneratorNew.ActivateAll(1, value);}}
    public static bool showFishTrails {get {return _showFishTrails;} set {_showFishTrails = value; FishGeneratorNew.ActivateAll(2, value);}}

    private static bool _showFishTags;
    private static bool _showFishDepthLines;
    private static bool _showFishTrails;

    public static float verticalScalingFactor;
}
