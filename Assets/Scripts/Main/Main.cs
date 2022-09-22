using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Threading.Tasks;
using System;


public delegate void FishDictAssembled();

public class Main : MonoBehaviour
{
    [SerializeField]
    private FishManager fishManager;
    [SerializeField]
    private MeshManager meshManager;
    [SerializeField]
    private SunController sunController;
    [SerializeField]
    private MoonController moonController;
    [SerializeField]
    private FishList fishList;
    private DataProcessor processor;
    [SerializeField]
    private TextAsset meshDataCSV, positionDataCSV, fishDataCSV, thermoclineDataCSV, weatherDataCSV, ysiDataCSV;

    [SerializeField]
    private Texture2D NDVI;

    [SerializeField]
    private GameObject managerObject;
    public static bool finishedStartup { get; private set; }

    public static event FishDictAssembled fishDictAssembled;

    private void Awake()
    {       
        finishedStartup = false;
        Dictionary<string, TextAsset> textAssetDict = new Dictionary<string, TextAsset> {
            {"meshData", meshDataCSV},
            {"positionData", positionDataCSV},
            {"thermoclineData", thermoclineDataCSV},
        };

        processor = new DataProcessor(textAssetDict, NDVI);
        processor.ReadData();
    }

    private async void Start()
    {
        meshManager.SetUpMesh();
        fishManager = new FishManager(managerObject);

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
            // ThermoclineDOMain.instance.UpdateThermoclineDOMain();
            WindWeatherMain.instance.UpdateWindWeather(); 
        }
    }
}