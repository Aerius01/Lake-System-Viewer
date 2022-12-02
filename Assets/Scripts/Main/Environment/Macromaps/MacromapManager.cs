using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;

public class MacromapManager: MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    private static PolygonPacket currentPacket;
    private static DateTime earliestTimestamp;

    // Properties
    private static bool timeBounded
    {
        get
        {
            if (MacromapManager.currentPacket.timestamp == null) return false; // no times to be bounded by
            else if (DateTime.Compare(TimeManager.instance.currentTime, Convert.ToDateTime(MacromapManager.currentPacket.timestamp)) < 0) return false; // the current time is earlier than the packet's timestamp
            else if (MacromapManager.currentPacket.nextTimestamp == null) return true; // we are in bounds (passed prev condition), but there is no future packet coming
            else
            {
                if (DateTime.Compare(TimeManager.instance.currentTime, Convert.ToDateTime(MacromapManager.currentPacket.timestamp)) > 0
                && DateTime.Compare(TimeManager.instance.currentTime, Convert.ToDateTime(MacromapManager.currentPacket.nextTimestamp)) < 0)
                return true; // traditional bounds (middle condition)
                else return false;
            }
        }
    }

    private static bool beforeFirstTS
    {
        get
        {
            if (DateTime.Compare(TimeManager.instance.currentTime, MacromapManager.earliestTimestamp) < 0) return true;
            else return false;
        }
    }
    
    // Singleton variables
    private static MacromapManager _instance;
    [HideInInspector]
    public static MacromapManager instance {get { return _instance; } set {_instance = value; }}

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }
    }

    // Called in Start() of Main.cs; will run before local Update()
    public static void InitializeMacrophyteMaps()
    {
        MacromapManager.earliestTimestamp = DatabaseConnection.MacromapPolygonsEarliestDate();
        MacromapManager.currentPacket = DatabaseConnection.GetMacromapPolygons();
    }

    public static void UpdateMaps()
    {
        if (!MacromapManager.beforeFirstTS)
        {
            if (!MacromapManager.timeBounded)
            {
                MacromapManager.currentPacket = DatabaseConnection.GetMacromapPolygons();
                if (currentPacket.timestamp == null) {MacromapManager.DisableMaps(); } // We're before the first timestamp
                else
                {
                    if (MacromapManager.instance.toggle.interactable == false) {MacromapManager.EnableMaps(); } 
                    // Update the maps

                }
            }
            else if (MacromapManager.instance.toggle.interactable == false) {MacromapManager.EnableMaps(); }
        }
        else if (MacromapManager.instance.toggle.interactable == true) {MacromapManager.DisableMaps(); }
    }

    public static void ToggleMaps()
    {
        if (UserSettings.macrophyteMaps)
        {
            // Activate them
            Debug.Log("activating");
        }
        else
        {
            // Deactivate them
            Debug.Log("deactivating");
        }
    }

    public static void EnableMaps(bool status=true)
    {
        if (status) MacromapManager.instance.toggle.interactable = true; 
        else
        {
            MacromapManager.instance.toggle.isOn = false;
            UserSettings.macrophyteMaps = false;
            MacromapManager.instance.toggle.interactable = false; 
        }
    }

    public static void DisableMaps() { MacromapManager.EnableMaps(false); }
}
