using UnityEngine;
using System.Collections.Generic;
using System;
using TMPro;
using System.Globalization;
using UnityEngine.UI;

public class UserSettings
{
    public static bool showFishTags {get {return showFishTags;} set {showFishTags = value; FishGeneratorNew.ActivateAll(0, value);}}
    public static bool showFishDepthLines {get {return showFishDepthLines;} set {showFishDepthLines = value; FishGeneratorNew.ActivateAll(1, value);}}
    public static bool showFishTrails {get {return showFishTrails;} set {showFishTrails = value; FishGeneratorNew.ActivateAll(2, value);}}

    public static float verticalScalingFactor;
}
