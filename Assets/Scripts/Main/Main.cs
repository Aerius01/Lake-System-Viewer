using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;


public delegate void FishDictAssembled();

public class Main : MonoBehaviour
{
    [SerializeField] private FishManager fishManager;
    [SerializeField] private SunController sunController;
    [SerializeField] private GameObject fishManagerObject, gameCanvas;
    [SerializeField] private MeshManager meshObject;
    [SerializeField] private ThermoclineDOMain thermoObject;
    [SerializeField] private WindWeatherMain weatherObject;
    [SerializeField] private MacromapManager macromapManager;
    [SerializeField] private HeightManager heightManager;
    [SerializeField] private FishList fishList;
    [SerializeField] private Species species;
    private bool finishedStartup = false;

    private float latitude = 53f, longitude = 13.58f;

    public async Task<bool> Initialize(LoaderBar loadingBar)
    {
        this.gameObject.SetActive(true);

        try
        {
            // Independent inits
            this.meshObject.gameObject.SetActive(true);
            this.meshObject.WakeUp();
            this.fishManager = new FishManager(fishManagerObject);

            // positions
            TableImports.tables[TableImports.checkTables[2]].Imported(TableImports.tables[TableImports.checkTables[2]].status);

            // Polygons (macromapManager)
            if (TableImports.tables[TableImports.checkTables[3]].status)
            {
                this.macromapManager.gameObject.SetActive(true); // TODO: if init failed, alert user
                this.macromapManager.WakeUp();
            }
            
            // Height Manager (macrophyte "grass" spawner)
            if (TableImports.tables[TableImports.checkTables[4]].status)
            {
                this.heightManager.gameObject.SetActive(true); // TODO: if init failed, alert user
                this.heightManager.WakeUp();
            }

            // Wind & weather
            if (TableImports.tables[TableImports.checkTables[6]].status)
            {
                this.weatherObject.gameObject.SetActive(true); // TODO: if init failed, alert user
                await this.weatherObject.WakeUp(); // run synchronously
                TableImports.tables[TableImports.checkTables[6]].Imported(this.weatherObject.initialized);
                if (!this.weatherObject.initialized) this.weatherObject.Clear();
            }

            this.sunController.WakeUp(this.latitude, this.longitude);

            // Meshmap dependents
            if (await this.meshObject.initialized)
            {
                if (!this.meshObject.SetUpMeshSync()) throw new Exception();
                else TableImports.tables[TableImports.checkTables[1]].Imported(true);

                if (TableImports.tables[TableImports.checkTables[7]].status)
                {
                    this.thermoObject.gameObject.SetActive(true); // TODO: if init failed, alert user // Depends on meshmap resolution
                    await this.thermoObject.WakeUp(); // run synchronously
                    TableImports.tables[TableImports.checkTables[7]].Imported(this.thermoObject.initialized);
                    if (!this.thermoObject.initialized) this.thermoObject.Clear();
                }
            }
            else throw new Exception(); 

            // Wait for the inits to finish and then attribute import statuses
            if (TableImports.tables[TableImports.checkTables[3]].status)
            {
                if (await this.macromapManager.initialized) TableImports.tables[TableImports.checkTables[3]].Imported(true); 
                else this.macromapManager.Clear();
            }

            if (TableImports.tables[TableImports.checkTables[4]].status)
            {
                if (await this.heightManager.initialized) TableImports.tables[TableImports.checkTables[4]].Imported(true);
                else this.heightManager.Clear();
            }

            // FishManager dependents
            if (await FishManager.initialized)
            {
                TableImports.tables[TableImports.checkTables[0]].Imported(await FishManager.initialized);

                this.gameCanvas.SetActive(true); // activates FishList and the Categorical/ContinuousFilterHandler Awake() methods, which are dependents on the FishManager
                this.fishList.WakeUp();
            }
            else throw new Exception();

            TimeManager.instance.PlayButton();
            this.finishedStartup = true;
        }
        catch (Exception) { return false; }
        
        return this.finishedStartup;
    }

    public void ClearAll()
    {
        this.finishedStartup = false;

        TimeManager.instance.PauseButton();
        this.gameCanvas.SetActive(false); 
        this.fishList.Clear();
        // filters too ? 

        this.sunController.Clear();
        if (TableImports.tables[TableImports.checkTables[7]].status) this.thermoObject.Clear();
        if (TableImports.tables[TableImports.checkTables[6]].status) this.weatherObject.Clear();
        if (TableImports.tables[TableImports.checkTables[3]].status) this.macromapManager.Clear(); 
        if (TableImports.tables[TableImports.checkTables[4]].status) this.heightManager.Clear();

        this.meshObject.Clear();
        this.fishManager.Clear();
        this.fishManager = null;

        this.gameObject.SetActive(false);
    }

    private async void FixedUpdate()
    {
        // TODO: error handling for interrupted DB connection

        if (this.finishedStartup)
        {
            fishManager.UpdateFish();
            
            // Parallelized updates
            List<Task> updateTasks = new List<Task>();

            if (this.thermoObject.initialized) updateTasks.Add(Task.Run(() => this.thermoObject.UpdateThermoclineDOMain()));
            if (this.weatherObject.initialized) updateTasks.Add(Task.Run(() => this.weatherObject.UpdateWindWeather()));
            if (TableImports.tables[TableImports.checkTables[3]].imported) { if (await this.macromapManager.initialized) updateTasks.Add(Task.Run(() => this.macromapManager.UpdateMaps())); }
            if (TableImports.tables[TableImports.checkTables[4]].imported) { if (await this.heightManager.initialized) updateTasks.Add(Task.Run(() => this.heightManager.UpdateHeights())); }

            Task completionTask = Task.WhenAll(updateTasks.ToArray());
            await completionTask;
        }
    }
}