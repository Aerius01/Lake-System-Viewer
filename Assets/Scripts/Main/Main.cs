using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;


public delegate void FishDictAssembled();

public class Main : MonoBehaviour
{
    [SerializeField] private FishManager fishManager;
    [SerializeField] private SunController sunController;
    [SerializeField] private MoonController moonController;
    [SerializeField] private GameObject fishManagerObject, mainCanvas;
    private bool finishedStartup = false;

    public static event FishDictAssembled fishDictAssembled;

    private async void Start()
    {
        mainCanvas.SetActive(true);
        LoadingScreen.Activate();
        List<Task> taskList = new List<Task>();


        // Have everything instanced, so that we can trash/reset anything by simply deleting the instance
        Task<bool> meshSetUp = MeshManager.instance.SetUpMesh();
        fishManager = new FishManager(fishManagerObject);

        LoadingScreen.Text("Waiting on heightmap data...");
        if (await meshSetUp) ThermoclineDOMain.instance.StartThermo(); // Cannot parallelize due to Unity operations 
        else
        {
            // error handling, mesh map fail
        }

        LoadingScreen.Text("Waiting on fish repository...");
        if (await FishManager.initialization)
        {
            MacromapManager.InitializeMacrophyteMaps();
            HeightManager.InitializeMacrophyteHeights();

            fishDictAssembled?.Invoke();
            TimeManager.instance.PlayButton();

            finishedStartup = true;
            LoadingScreen.Deactivate();
        }
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
            ThermoclineDOMain.instance.UpdateThermoclineDOMain();
            WindWeatherMain.instance.UpdateWindWeather(); 
            await Task.Run(() => MacromapManager.UpdateMaps());
            await Task.Run(() => HeightManager.instance.ManualUpdate());
        }
    }
}