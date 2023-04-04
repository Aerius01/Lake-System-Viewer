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
    private bool finishedStartup = false;
    private float lowResTimer = 0f;

    private float latitude = 53f, longitude = 13.58f;

    public async Task<bool> Initialize()
    {
        this.gameObject.SetActive(true);

        try
        {
            // Independent inits
            this.meshObject.gameObject.SetActive(true);
            this.meshObject.WakeUp();
            this.fishManager = new FishManager(fishManagerObject);

            // positions
            TableProofings.tables[TableProofings.checkTables[2]].Imported(TableProofings.tables[TableProofings.checkTables[2]].status);

            // Polygons (macromapManager)
            if (TableProofings.tables[TableProofings.checkTables[3]].status)
            {
                this.macromapManager.gameObject.SetActive(true);
                this.macromapManager.WakeUp(); // a single async statement, immediately yields control back to this thread
            }
            
            // Height Manager (macrophyte "grass" spawner)
            if (TableProofings.tables[TableProofings.checkTables[4]].status)
            {
                this.heightManager.gameObject.SetActive(true);
                this.heightManager.WakeUp(); // a single async statement, immediately yields control back to this thread
            }

            // Wind & weather
            if (TableProofings.tables[TableProofings.checkTables[6]].status)
            {
                this.weatherObject.gameObject.SetActive(true);
                await this.weatherObject.WakeUp(); // run synchronously
                TableProofings.tables[TableProofings.checkTables[6]].Imported(this.weatherObject.initialized);
                if (!this.weatherObject.initialized) this.weatherObject.Clear();
            }

            this.sunController.WakeUp(this.latitude, this.longitude);

            // Meshmap dependents
            if (await this.meshObject.initialized)
            {
                if (!this.meshObject.SetUpMeshSync()) throw new Exception();
                else TableProofings.tables[TableProofings.checkTables[1]].Imported(true);

                if (TableProofings.tables[TableProofings.checkTables[7]].status)
                {
                    this.thermoObject.gameObject.SetActive(true); // Depends on meshmap resolution
                    await this.thermoObject.WakeUp(); // run synchronously
                    TableProofings.tables[TableProofings.checkTables[7]].Imported(this.thermoObject.initialized);
                    if (!this.thermoObject.initialized) this.thermoObject.Clear();
                }
            }
            else throw new Exception(); 

            // Wait for the inits to finish and then attribute import statuses
            if (TableProofings.tables[TableProofings.checkTables[3]].status)
            {
                if (await this.macromapManager.initialized) TableProofings.tables[TableProofings.checkTables[3]].Imported(true); 
                else this.macromapManager.Clear();
            }

            if (TableProofings.tables[TableProofings.checkTables[4]].status)
            {
                if (await this.heightManager.initialized) TableProofings.tables[TableProofings.checkTables[4]].Imported(true);
                else this.heightManager.Clear();
            }

            // FishManager dependents
            if (await FishManager.initialized)
            {
                TableProofings.tables[TableProofings.checkTables[0]].Imported(await FishManager.initialized);

                this.gameCanvas.SetActive(true); // activates FishList and the Categorical/ContinuousFilterHandler Awake() methods, which are dependents on the FishManager
                this.fishList.WakeUp();
            }
            else 
            { Debug.Log(FishManager.initialized); throw new Exception(); }
        }
        catch (Exception) { return false; }
        
        return true;
    }

    public void ClearAll()
    {
        this.finishedStartup = false;

        TimeManager.instance.PauseButton();
        this.gameCanvas.SetActive(false); 
        this.fishList.Clear();

        this.sunController.Clear();
        if (TableProofings.tables[TableProofings.checkTables[7]].status) this.thermoObject.Clear();
        if (TableProofings.tables[TableProofings.checkTables[6]].status) this.weatherObject.Clear();
        if (TableProofings.tables[TableProofings.checkTables[3]].status) this.macromapManager.Clear(); 
        if (TableProofings.tables[TableProofings.checkTables[4]].status) this.heightManager.Clear();

        this.meshObject.Clear();
        this.fishManager.Clear();
        this.fishManager = null;

        this.gameObject.SetActive(false);
    }

    public void GoodToGo() { this.finishedStartup = true; }

    private async void Update()
    {
        if (this.finishedStartup)
        {
            // Update the fish at high resolution
            fishManager.UpdateFish(); // ~50fps

            // Update the environment at a lower resolution
            if (this.lowResTimer >= 1f) // 1fps
            {
                this.lowResTimer = 0f;

                if (this.thermoObject.initialized) Task.Run(() => this.thermoObject.UpdateThermoclineDOMain());
                if (this.weatherObject.initialized) Task.Run(() => this.weatherObject.UpdateWindWeather());
                if (TableProofings.tables[TableProofings.checkTables[3]].imported) { if (this.macromapManager.initialized.IsCompleted) { if (await this.macromapManager.initialized) Task.Run(() => this.macromapManager.UpdateMaps()); } }
                if (TableProofings.tables[TableProofings.checkTables[4]].imported) { if (this.heightManager.initialized.IsCompleted) { if (await this.heightManager.initialized) Task.Run(() => this.heightManager.UpdateHeights()); } }
            }
            else this.lowResTimer += Time.deltaTime;
        }
    }
}