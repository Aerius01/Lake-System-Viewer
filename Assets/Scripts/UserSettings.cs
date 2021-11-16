using UnityEngine;
using System;
using TMPro;
using System.Globalization;
using UnityEngine.UI;

public class UserSettings : MonoBehaviour
{
    public GameObject settingsMenu;
    public static bool showFishTags, showFishDepthLines, showFishTrails;
    public static float verticalScalingFactor;

    private void Awake()
    {
        // find gameobjects & set values
    }

    void Update()
    {
        // register onchange of those objects
    }

}
