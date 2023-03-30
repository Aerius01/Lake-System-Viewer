using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;

public delegate void MacroHeightChange();

public class HeightManager : MonoBehaviour
{
    private DateTime earliestTimestamp;
    public MacroHeightPacket currentPacket { get; private set; }
    private static readonly object locker = new object();
    private static readonly object spawnLock = new object();

    public Task<bool> initialized { get; private set; }
    private bool updating = false, performSyncUpdate = false;

    [SerializeField] private Toggle toggle;
    [SerializeField] private GameObject bufferIcon;

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
    private static HeightManager _instance;
    [HideInInspector]
    public static HeightManager instance {get { return _instance; } set {_instance = value; }}


    private void Awake()
    {
        this.initialized = null;
        this.currentPacket = null;

        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }
    }

    public void WakeUp() { this.initialized = Task.Run(() => this.AwakeAsync()); }

    public void Clear()
    { 
        this.initialized = Task.Run(() => false);
        this.currentPacket = null;
        this.bufferIcon.SetActive(false);
        if (GrassSpawner.instance != null) GrassSpawner.instance.Clear();
        lock(HeightManager.locker) this.performSyncUpdate = false;

        this.gameObject.SetActive(false); 
    }

    private async Task<bool> AwakeAsync()
    {
        try
        {
            this.earliestTimestamp = DatabaseConnection.EarliestDate("macromap_heights_local");
            if (this.earliestTimestamp == DateTime.MaxValue) throw new Exception();

            await this.UpdateHeights();
            return true;
        }
        catch (Exception) { return false; }
    }

    private async Task<bool> FetchNewBounds()
    {
        // if an exception is thrown or no data is returned (null), the method returns false
        try 
        { 
            this.currentPacket = await DatabaseConnection.GetMacromapHeights();
            if (this.currentPacket == null) throw new Exception();
            else return true;
        }
        catch (Exception) { return false; }
    }

    public async Task UpdateHeights()
    {
        // The grass spawner needs the intensity map. Without it, the entire exercise is moot.
        if (!this.beforeFirstTS && !this.updating && !this.performSyncUpdate)
        {
            // Check whether we need to requery
            if (!this.timeBounded) 
            {
                lock(HeightManager.locker) this.updating = true;
                if (await this.FetchNewBounds()) lock(HeightManager.locker) this.performSyncUpdate = true; 
                lock(HeightManager.locker) this.updating = false;
            }
        }

        if (this.beforeFirstTS) this.currentPacket = null;
    }

    private async void Update()
    {
        if (this.initialized.IsCompleted)
        {
            if (await this.initialized)
            {
                // Constantly check whether to enable or disable interactability
                // The macroheights toggle should only be enabled if MacromapManager is not updating, hence the lock
                if (!this.beforeFirstTS) { lock(MacromapManager.mapLocker) { if (!this.toggle.interactable && !this.updating && MacromapManager.instance.intensityMap != null && this.currentPacket != null) { this.EnableMaps(); } } }
                else if (this.toggle.interactable == true) { this.DisableMaps(); }

                // Decide whether or not to show the buffering icon
                if (this.updating && !this.bufferIcon.activeSelf) this.bufferIcon.SetActive(true);
                else if (!this.updating && this.bufferIcon.activeSelf) this.bufferIcon.SetActive(false);

                // Unity is demanding this be executed from the main thread, hence the workaround
                lock(HeightManager.locker)
                {
                    if (this.performSyncUpdate)
                    {
                        this.performSyncUpdate = false;
                        if (GrassSpawner.instance != null) GrassSpawner.instance.SpawnGrass();
                    }
                }
            }
        }
    }

    // Set the interactability of the toggle
    public void EnableMaps(bool status=true)
    {
        if (status) this.toggle.interactable = true; 
        else
        {
            this.toggle.isOn = false;
            UserSettings.macrophyteHeights = false;
            this.toggle.interactable = false; 
        }
    }

    public void DisableMaps() { this.EnableMaps(false); }

}