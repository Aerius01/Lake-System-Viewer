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

    public static event FishDictAssembled fishDictAssembled;

    private void Awake()
    {       
        Dictionary<string, TextAsset> textAssetDict = new Dictionary<string, TextAsset> {
            {"meshData", meshDataCSV},
            {"positionData", positionDataCSV},
            {"thermoclineData", thermoclineDataCSV},
        };

        processor = new DataProcessor(textAssetDict, NDVI);
        processor.ReadData();
    }

    private void Start()
    {
        meshManager.SetUpMesh();
        fishManager = new FishManager(managerObject);
        fishDictAssembled?.Invoke();
        fishList.PopulateList();
        TimeManager.instance.PlayButton();
    }

    private void FixedUpdate()
    {
        fishManager.UpdateFish();
        sunController.AdjustSunPosition();
        moonController.AdjustMoonPosition();
        // ThermoclineDOMain.instance.UpdateThermoclineDOMain();
        WindWeatherMain.instance.UpdateWindWeather();        
    }

    private void GetData()
    {
        FishPacket packet = DatabaseConnection.GetFishMetadata(2054);

        Debug.Log(packet.fishID);
        Debug.Log(packet.earliestTime);
        Debug.Log(packet.latestTime);
        Debug.Log(packet.length);
        Debug.Log(packet.weight);
        Debug.Log(packet.speciesCode);
        Debug.Log(packet.speciesName);
        Debug.Log(packet.male);
        Debug.Log(packet.captureType);
    }
}