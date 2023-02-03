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


    private PolygonPacket currentPacket;
    private DateTime earliestTimestamp;
    public float[,] intensityMap { get; private set; }

    private bool updating = false, performSyncUpdate = false;
    public Task<bool> initialized { get; private set; }

    private static readonly object locker = new object();
    public static readonly object mapLocker = new object();

    // Properties
    private bool timeBounded
    {
        get
        {
            if (this.currentPacket == null) return false; // no times to be bounded by
            else if (DateTime.Compare(TimeManager.instance.currentTime, Convert.ToDateTime(this.currentPacket.timestamp)) < 0) return false; // the current time is earlier than the packet's timestamp
            else if (this.currentPacket.nextTimestamp == null) return true; // we are in bounds (passed prev condition), but there is no future packet coming
            else
            {
                if (DateTime.Compare(TimeManager.instance.currentTime, Convert.ToDateTime(this.currentPacket.timestamp)) > 0
                && DateTime.Compare(TimeManager.instance.currentTime, Convert.ToDateTime(this.currentPacket.nextTimestamp)) < 0)
                return true; // traditional bounds (middle condition)
                else return false;
            }
        }
    }

    private bool beforeFirstTS
    {
        get
        {
            if (DateTime.Compare(TimeManager.instance.currentTime, this.earliestTimestamp) < 0) return true;
            else return false;
        }
    }
    
    // Singleton variables
    private static MacromapManager _instance;
    [HideInInspector]
    public static MacromapManager instance {get { return _instance; } set {_instance = value; }}

    private void Awake()
    {
        // Inits
        this.intensityMap = null;
        this.currentPacket = null;

        // Singleton
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }

        // No dependence on Unity functions --> can run async
        this.initialized = Task.Run(() => this.AwakeAsync());
    }

    private async Task<bool> AwakeAsync()
    {
        try
        {
            this.earliestTimestamp = DatabaseConnection.EarliestDate("macromap_polygons_local");
            if (this.earliestTimestamp == DateTime.MaxValue) throw new Exception();

            await this.UpdateMaps();
            return true;
        }
        catch (Exception) { return false; }
    }

    private async Task<bool> FetchNewBounds()
    {
        // if an exception is thrown or no data is returned (null), the method returns false
        try 
        { 
            this.currentPacket = await DatabaseConnection.GetMacromapPolygons();
            if (this.currentPacket == null) throw new Exception();
            else return true;
        }
        catch (Exception) { return false; }
    }

    public async Task UpdateMaps()
    {
        // Handle asynchronous updates
        // We don't want to senselessly overload the system with queries that return nothing
        if (!this.beforeFirstTS && !this.updating && !this.performSyncUpdate)
        {
            // Secure the multi-threading
            lock(MacromapManager.locker) this.updating = true;

            if (!this.timeBounded) 
            { 
                if (await this.FetchNewBounds()) 
                {
                    lock(MacromapManager.mapLocker)
                    {    
                        this.intensityMap = new float[LocalMeshData.resolution, LocalMeshData.resolution];
                        for (int y = 0; y < LocalMeshData.resolution; y++)
                        {
                            for (int x = 0; x < LocalMeshData.resolution; x++)
                            {
                                this.intensityMap[y, x] = 0f;
                                foreach (MacromapPolygon polygon in this.currentPacket.polygons)
                                {
                                    // Apply average intensity
                                    if (polygon.PointInPolygon(new Vector2(x, y)))
                                    {
                                        this.intensityMap[y, x] = (polygon.lowerCoverage + polygon.upperCoverage) / 2f / 100f;
                                        break;
                                    }
                                }
                            }
                        }
                    }

                    lock(MacromapManager.locker) { this.performSyncUpdate = true; } 
                }
                else { this.intensityMap = null; }
            }
            lock(MacromapManager.locker) this.updating = false;
        }
    }

    private async void Update()
    {
        if (await this.initialized)
        {
            // Constantly checked whether to enable or disable
            // These Unity operations are separated from the manual UpdateMaps() call because they need to be conducted on the main thread
            if (!this.beforeFirstTS)
            {
                if (this.toggle.interactable == false && !this.updating && this.intensityMap != null)
                { this.EnableMaps(); }
            }
            else if (this.toggle.interactable == true) { this.DisableMaps(); }

            // Decide whether or not to show the buffering icon
            if (this.updating && !this.bufferIcon.activeSelf) this.bufferIcon.SetActive(true);
            else if (!this.updating && this.bufferIcon.activeSelf) this.bufferIcon.SetActive(false);

            // Unity is demanding this be executed from the main thread, hence the workaround
            lock (MacromapManager.locker)
            {
                if (this.performSyncUpdate)
                {
                    this.performSyncUpdate = false;
                    GrassSpawner.instance.SpawnGrass();
                }
            }
        }
    }

    public void EnableMaps(bool status=true)
    {
        if (status) this.toggle.interactable = true; 
        else
        {
            this.toggle.isOn = false;
            UserSettings.macrophyteMaps = false;
            this.toggle.interactable = false; 
        }
    }

    public void DisableMaps() { this.EnableMaps(false); }
}
