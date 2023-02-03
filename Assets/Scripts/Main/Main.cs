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
    private bool finishedStartup = false;

    public async Task<bool> Initialize(LoaderBar loadingBar)
    {
        this.gameObject.SetActive(true);

        try
        {
            // Independent inits
            this.meshObject.gameObject.SetActive(true);
            this.fishManager = new FishManager(fishManagerObject);

            this.weatherObject.gameObject.SetActive(true); // TODO: if init failed, alert user
            this.macromapManager.gameObject.SetActive(true); // TODO: if init failed, alert user
            this.heightManager.gameObject.SetActive(true); // TODO: if init failed, alert user

            this.sunController.gameObject.SetActive(true);
            this.sunController.SetLatLong(53f, 13.58f);

            // Meshmap dependents
            if (await this.meshObject.initialized)
            {
                this.meshObject.SetUpMeshSync();
                this.thermoObject.gameObject.SetActive(true); // TODO: if init failed, alert user // Depends on meshmap resolution
            }
            else throw new Exception(); 

            // FishManager dependents
            if (await FishManager.initialized)
            {
                this.gameCanvas.SetActive(true); // activates FishList and the Categorical/ContinuousFilterHandler Awake() methods, which are dependents on the FishManager
                TimeManager.instance.PlayButton();
            }
            else throw new Exception();

            this.finishedStartup = true;
        }
        catch (Exception) { return false; }
        
        // need to return info if only partial success (some init fails)
        // wait here for all inits?
        return this.finishedStartup;
    }

    public void ClearAll()
    {
        // clear all Start() initializations
        // Time/playback fully resets with a re-run of main.cs
        // restart
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