using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public class MacromapManager: MonoBehaviour
{
    [SerializeField] private Toggle toggle;

    private static PolygonPacket currentPacket;
    private static DateTime earliestTimestamp;
    public static float[,] intensityMap;
    public static bool alreadyUpdating = false;
    private static readonly object locker = new object();

    // Properties
    private static bool timeBounded
    {
        get
        {
            if (MacromapManager.currentPacket == null) return false; // no times to be bounded by
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

    // Called in Start() of Main.cs; will run before local Update() --> FALSE, what the hell, Unity?
    public static void InitializeMacrophyteMaps()
    {
        MacromapManager.earliestTimestamp = DatabaseConnection.MacromapPolygonsEarliestDate();
        // MacromapManager.currentPacket = DatabaseConnection.GetMacromapPolygons();
        // foreach (MacromapPolygon polygon in MacromapManager.currentPacket.polygons)
        // {
        //     float xcoord = 0f;
        //     float ycoord = 0f;
        //     foreach (Vector2 coord in polygon.coordinates)
        //     { 
        //         xcoord += coord.x;
        //         ycoord += coord.y;
        //         Debug.Log(string.Format("{0}: ({1}, {2})", polygon.polygonID, coord.y, coord.x));
        //     }

        //     Debug.Log(string.Format("{0}: ({1}, {2})", polygon.polygonID, ycoord / polygon.vertexCount, xcoord / polygon.vertexCount)); 
        //     Debug.Log(string.Format("{0}: {1}", polygon.polygonID, polygon.PointInPolygon(new Vector2(xcoord / polygon.vertexCount, ycoord / polygon.vertexCount)))); 
        // }
        MacromapManager.intensityMap = null;
    }

    public static async Task UpdateMaps()
    {
        // Updating regardless as to the state of UserSettings.macrophyteMaps
        if (!MacromapManager.beforeFirstTS)
        {
            if ((!MacromapManager.timeBounded || MacromapManager.intensityMap == null) && !MacromapManager.alreadyUpdating)
            {
                lock(MacromapManager.locker) MacromapManager.alreadyUpdating = true;
                MacromapManager.currentPacket = await DatabaseConnection.GetMacromapPolygons();
                if (MacromapManager.instance.toggle.interactable == false) {MacromapManager.EnableMaps(); } 

                // Create a [0, 1] valued array of color intensities to be applied to mesh
                MacromapManager.intensityMap = new float[LocalMeshData.resolution, LocalMeshData.resolution];
                for (int y = 0; y < LocalMeshData.resolution; y++)
                {
                    for (int x = 0; x < LocalMeshData.resolution; x++)
                    {
                        MacromapManager.intensityMap[y, x] = 0f;
                        foreach (MacromapPolygon polygon in MacromapManager.currentPacket.polygons)
                        {
                            // Apply average intensity
                            if (polygon.PointInPolygon(new Vector2(x, y)))
                            {
                                MacromapManager.intensityMap[y, x] = (polygon.lowerCoverage + polygon.upperCoverage) / 2f / 100f;
                                break;
                            }
                        }
                    }
                }
                lock(MacromapManager.locker) MacromapManager.alreadyUpdating = false;
            }
            else if (MacromapManager.instance.toggle.interactable == false) {MacromapManager.EnableMaps(); }
        }
        else if (MacromapManager.instance.toggle.interactable == true) {MacromapManager.DisableMaps(); }
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
