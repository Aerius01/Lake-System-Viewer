using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;

public delegate void MacroHeightChange();

public class HeightManager : MonoBehaviour
{
    private static DateTime earliestTimestamp;
    public static MacroHeightPacket currentPacket { get; private set; }
    public static bool alreadyUpdating = false;
    private static readonly object locker = new object();
    private static readonly object spawnLock = new object();

    public static event MacroHeightChange macroHeightChange;
    private static bool triggerSpawn = false;

    [SerializeField] private Toggle toggle;

    // Properties
    private static bool timeBounded
    {
        get
        {
            if (HeightManager.currentPacket == null) return false; // no times to be bounded by
            else if (DateTime.Compare(TimeManager.instance.currentTime, Convert.ToDateTime(HeightManager.currentPacket.timestamp)) < 0) return false; // the current time is earlier than the packet's timestamp
            else if (HeightManager.currentPacket.nextTimestamp == null) return true; // we are in bounds (passed prev condition), but there is no future packet coming
            else
            {
                if (DateTime.Compare(TimeManager.instance.currentTime, Convert.ToDateTime(HeightManager.currentPacket.timestamp)) > 0
                && DateTime.Compare(TimeManager.instance.currentTime, Convert.ToDateTime(HeightManager.currentPacket.nextTimestamp)) < 0)
                return true; // traditional bounds (middle condition)
                else return false;
            }
        }
    }

    private static bool beforeFirstTS
    {
        get
        {
            if (DateTime.Compare(TimeManager.instance.currentTime, HeightManager.earliestTimestamp) < 0) return true;
            else return false;
        }
    }

    // Singleton variables
    private static HeightManager _instance;
    [HideInInspector]
    public static HeightManager instance {get { return _instance; } set {_instance = value; }}




    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }
    }

    // Called in Start() of Main.cs
    public static void InitializeMacrophyteHeights()
    {
        HeightManager.earliestTimestamp = DatabaseConnection.EarliestDate("macromap_heights_local");
        macroHeightChange += GrassSpawner.instance.SpawnGrass;
    }

    private void Update()
    {
        // Constantly check whether to enable or disable interactability
        if (!HeightManager.beforeFirstTS)
        {
            if (HeightManager.instance.toggle.interactable == false && !HeightManager.alreadyUpdating && !MacromapManager.alreadyUpdating && MacromapManager.intensityMap != null)
            { HeightManager.EnableMaps(); }
        }
        else if (HeightManager.instance.toggle.interactable == true) { HeightManager.DisableMaps(); }

        // Unity is demanding this be executed from the main thread, hence the workaround
        lock(HeightManager.spawnLock)
        {
            if (HeightManager.triggerSpawn)
            {
                HeightManager.triggerSpawn = false;
                macroHeightChange?.Invoke();
            }
        }
    }

    public async void ManualUpdate()
    {
        // The grass spawner needs the intensity map. Without it, the entire exercise is moot.
        if (!HeightManager.beforeFirstTS && !HeightManager.alreadyUpdating)
        {
            // Check whether we need to requery
            if (!HeightManager.timeBounded)
            {
                lock(HeightManager.locker) alreadyUpdating = true;
                await HeightManager.Requery();
            }
        }
    }

    private static async Task Requery()
    {
        HeightManager.currentPacket = await DatabaseConnection.GetMacromapHeights();
        Debug.Log("pulled");

        // Alert the grass spawner that it should respawn the prefabs
        lock(HeightManager.spawnLock) { HeightManager.triggerSpawn = true; }

        lock(HeightManager.locker) alreadyUpdating = false;
    }

    // Set the interactability of the toggle
    public static void EnableMaps(bool status=true)
    {
        if (status) HeightManager.instance.toggle.interactable = true; 
        else
        {
            HeightManager.instance.toggle.isOn = false;
            UserSettings.macrophyteHeights = false;
            HeightManager.instance.toggle.interactable = false; 
        }
    }

    public static void DisableMaps() { HeightManager.EnableMaps(false); }

}