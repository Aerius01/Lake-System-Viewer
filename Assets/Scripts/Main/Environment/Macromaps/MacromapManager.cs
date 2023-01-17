using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;
using System;
using System.Threading.Tasks;

public delegate void MacroPolyChange();

public class MacromapManager: MonoBehaviour
{
    [SerializeField] private Toggle toggle;
    [SerializeField] private GameObject bufferIcon;


    private static PolygonPacket currentPacket;
    private static DateTime earliestTimestamp;
    public static float[,] intensityMap;
    public static bool alreadyUpdating { get; private set; }
    private static readonly object locker = new object();
    public static readonly object mapLocker = new object();
    public static readonly object spawnLock = new object();

    public static event MacroPolyChange macroPolyChange;
    private static bool triggerSpawn = false;

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

        MacromapManager.alreadyUpdating = false;
    }

    // Called in Start() of Main.cs
    public static void InitializeMacrophyteMaps()
    {
        MacromapManager.earliestTimestamp = DatabaseConnection.EarliestDate("macromap_polygons_local");
        MacromapManager.intensityMap = null;
        macroPolyChange += GrassSpawner.instance.SpawnGrass;
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

                // Create a [0, 1] valued array of color intensities to be applied to mesh
                lock(MacromapManager.mapLocker)
                {    
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

                    // Alert the grass spawner that it should respawn the prefabs
                    lock (MacromapManager.spawnLock) { MacromapManager.triggerSpawn = true; }
                }
                lock(MacromapManager.locker) MacromapManager.alreadyUpdating = false;
            }
        }
        else MacromapManager.intensityMap = null;
    }

    private void Update()
    {
        // Constantly checked whether to enable or disable
        // These Unity operations are separated from the manual UpdateMaps() call because they need to be conducted on the main thread
        if (!MacromapManager.beforeFirstTS)
        {
            if (MacromapManager.instance.toggle.interactable == false && !MacromapManager.alreadyUpdating && MacromapManager.intensityMap != null)
            { MacromapManager.EnableMaps(); }
        }
        else if (MacromapManager.instance.toggle.interactable == true) { MacromapManager.DisableMaps(); }

        // Decide whether or not to show the buffering icon
        if (MacromapManager.alreadyUpdating && !bufferIcon.activeSelf) bufferIcon.SetActive(true);
        else if (!MacromapManager.alreadyUpdating && bufferIcon.activeSelf) bufferIcon.SetActive(false);

        // Unity is demanding this be executed from the main thread, hence the workaround
        lock (MacromapManager.spawnLock)
        {
            if (MacromapManager.triggerSpawn)
            {
                MacromapManager.triggerSpawn = false;
                macroPolyChange?.Invoke();
            }
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
