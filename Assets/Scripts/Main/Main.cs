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
    private bool finishedStartup = false;

    public static event FishDictAssembled fishDictAssembled;

    public async Task<bool> Initialize(LoaderBar loadingBar)
    {
        // Have everything instanced, so that we can trash/reset anything by simply deleting the instance
        // Decide where the async nature should come in, in case some instances need to instantiate on the main thread

        // activate the respective gameobjects so that their initializations run synchronously, but are activated 
        this.gameCanvas.SetActive(true);
        this.gameObject.SetActive(true);

        this.meshObject.gameObject.SetActive(true);
        this.fishManager = new FishManager(fishManagerObject);

        // LoadingScreen.Text("Waiting on heightmap data...");
        // loadingBar.SetText("Processing heightmap");

        if (await MeshManager.initialization) MeshManager.instance.SetUpMeshSync();
        else throw new Exception(); 

        // LoadingScreen.Text("Waiting on fish repository...");
        // loadingBar.SetText("Setting up fish repository");
        if (await FishManager.initialization)
        {
            this.thermoObject.gameObject.SetActive(true);






            MacromapManager.InitializeMacrophyteMaps();
            HeightManager.InitializeMacrophyteHeights();
            WindWeatherMain.instance.StartWindWeather();

            fishDictAssembled?.Invoke();
            TimeManager.instance.PlayButton();

            finishedStartup = true;
            // LoadingScreen.Deactivate();
        }

        return finishedStartup;
    }

    public void ClearAll()
    {
        // clear all Start() initializations
        // Time/playback fully resets with a re-run of main.cs
        // restart
    }

    private async void FixedUpdate()
    {
        if (finishedStartup)
        {
            fishManager.UpdateFish();
            sunController.AdjustSunPosition();
            moonController.AdjustMoonPosition();
            if (this.thermoObject.initialized) await Task.Run(() => this.thermoObject.UpdateThermoclineDOMain());
            WindWeatherMain.instance.UpdateWindWeather(); 
            await Task.Run(() => MacromapManager.UpdateMaps());
            await Task.Run(() => HeightManager.instance.ManualUpdate());
        }
    }
}