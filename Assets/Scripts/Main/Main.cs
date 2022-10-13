using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Threading.Tasks;
using System;


public delegate void FishDictAssembled();

public class Main : MonoBehaviour
{
    [SerializeField] private FishManager fishManager;
    [SerializeField] private TerrainManager terrainManager;
    [SerializeField] private MeshManager meshManager;
    [SerializeField] private SunController sunController;
    [SerializeField] private MoonController moonController;
    [SerializeField] private FishList fishList;
    [SerializeField] private TextAsset meshDataCSV;
    [SerializeField] private GameObject managerObject;
    private bool finishedStartup = false;

    public static event FishDictAssembled fishDictAssembled;

    private async void Start()
    {
        List<Task> taskList = new List<Task>();

        Task<bool> meshSetUp = meshManager.SetUpMesh();
        fishManager = new FishManager(managerObject);

        if (await meshSetUp)
        {
            // Cannot parallelize due to Unity operations
            ThermoclineDOMain.instance.StartThermo();
        }
        else
        {
            // error handling, mesh map fail
        }

        if (await FishManager.initialization)
        {
            fishDictAssembled?.Invoke();
            TimeManager.instance.PlayButton();

            finishedStartup = true;
        }
    }

    private void FixedUpdate()
    {
        if (finishedStartup)
        {
            fishManager.UpdateFish();
            sunController.AdjustSunPosition();
            moonController.AdjustMoonPosition();
            ThermoclineDOMain.instance.UpdateThermoclineDOMain();
            WindWeatherMain.instance.UpdateWindWeather(); 
        }
    }
}