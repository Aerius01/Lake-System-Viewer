using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;
using System;


public delegate void FishDictAssembled();

public class Main : MonoBehaviour
{
    [SerializeField] private FishManager fishManager;
    [SerializeField] private SunController sunController;
    [SerializeField] private MoonController moonController;
    [SerializeField] private GameObject fishManagerObject, gameCanvas;
    [SerializeField] private MeshManager meshObject;
    [SerializeField] private ThermoclineDOMain thermoObject;
    [SerializeField] private WindWeatherMain weatherObject;
    [SerializeField] private MacromapManager macromapManager;
    [SerializeField] private HeightManager heightManager;
    [SerializeField] private FishList fishList;
    [SerializeField] private Species species;
    private bool finishedStartup = false;
    private int counter = 0;

    public async Task<bool> Initialize(LoaderBar loadingBar)
    {
        this.gameObject.SetActive(true);

        // try
        // {
            // Independent inits
            this.meshObject.gameObject.SetActive(true);
            this.meshObject.WakeUp();

            this.fishManager = new FishManager(fishManagerObject);

            // positions
            TableImports.tables[TableImports.checkTables[2]].Imported(TableImports.tables[TableImports.checkTables[2]].status);

            if (TableImports.tables[TableImports.checkTables[3]].status)
            {
                this.macromapManager.gameObject.SetActive(true); // TODO: if init failed, alert user
                this.macromapManager.WakeUp();
            }

            if (TableImports.tables[TableImports.checkTables[4]].status)
            {
                this.heightManager.gameObject.SetActive(true); // TODO: if init failed, alert user
                this.heightManager.WakeUp();
            }

            if (TableImports.tables[TableImports.checkTables[5]].status)
            {
                this.species.gameObject.SetActive(true); 
                this.species.WakeUp(); // TODO: if init failed, alert user
                TableImports.tables[TableImports.checkTables[5]].Imported(this.species.initialized);
            }
            
            if (TableImports.tables[TableImports.checkTables[6]].status)
            {
                this.weatherObject.gameObject.SetActive(true); // TODO: if init failed, alert user
                await this.weatherObject.WakeUp(); // run synchronously
                TableImports.tables[TableImports.checkTables[6]].Imported(this.weatherObject.initialized);
            }

            this.sunController.gameObject.SetActive(true);
            this.sunController.SetLatLong(53f, 13.58f);

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
                }
            }
            else throw new Exception(); 

            // FishManager dependents
            if (await FishManager.initialized)
            {
                TableImports.tables[TableImports.checkTables[0]].Imported(await FishManager.initialized);

                this.gameCanvas.SetActive(true); // activates FishList and the Categorical/ContinuousFilterHandler Awake() methods, which are dependents on the FishManager
                this.fishList.WakeUp();
            }
            else throw new Exception();

            // Wait for the inits to finish and then attribute import statuses
            TableImports.tables[TableImports.checkTables[3]].Imported(await this.macromapManager.initialized);
            TableImports.tables[TableImports.checkTables[4]].Imported(await this.heightManager.initialized);

            TimeManager.instance.PlayButton();
            this.finishedStartup = true;
        // }
        // catch (Exception) { return false; }
        
        this.counter++;
        if (this.counter == 2) return true;
        else return false;
        // return this.finishedStartup;
    }

    public void ClearAll()
    {
        this.finishedStartup = false;

        TimeManager.instance.PauseButton();
        this.gameCanvas.SetActive(false); 
        this.fishList.Clear();
        // filters too ? 

        this.sunController.gameObject.SetActive(false);
        this.thermoObject.Clear();
        this.weatherObject.Clear();
        this.macromapManager.Clear();
        this.heightManager.Clear();
        this.species.Clear();
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
            moonController.AdjustMoonPosition();
            
            // Parallelized updates
            List<Task> updateTasks = new List<Task>();

            if (this.thermoObject.initialized) updateTasks.Add(Task.Run(() => this.thermoObject.UpdateThermoclineDOMain()));
            if (this.weatherObject.initialized) updateTasks.Add(Task.Run(() => this.weatherObject.UpdateWindWeather()));
            if (await this.macromapManager.initialized) updateTasks.Add(Task.Run(() => this.macromapManager.UpdateMaps()));
            if (await this.heightManager.initialized) updateTasks.Add(Task.Run(() => this.heightManager.UpdateHeights()));

            Task completionTask = Task.WhenAll(updateTasks.ToArray());
            await completionTask;
        }
    }
}