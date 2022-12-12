using System.Collections.Generic;
using UnityEngine;
using System.Threading.Tasks;


public delegate void FishDictAssembled();

public class Main : MonoBehaviour
{
    [SerializeField] private FishManager fishManager;
    [SerializeField] private SunController sunController;
    [SerializeField] private MoonController moonController;
    [SerializeField] private GameObject managerObject;
    private bool finishedStartup = false;

    public static event FishDictAssembled fishDictAssembled;

    private async void Start()
    {
        LoadingScreen.Activate();
        List<Task> taskList = new List<Task>();

        Task<bool> meshSetUp = MeshManager.instance.SetUpMesh();
        fishManager = new FishManager(managerObject);

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

            fishDictAssembled?.Invoke();
            TimeManager.instance.PlayButton();

            finishedStartup = true;
            LoadingScreen.Deactivate();
        }
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
        }
    }
}