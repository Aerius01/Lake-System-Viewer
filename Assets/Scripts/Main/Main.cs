using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using UnityEngine.UI;
using System.Threading.Tasks;


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

    private bool called = false;

    private void Awake()
    {       
        Dictionary<string, TextAsset> textAssetDict = new Dictionary<string, TextAsset> {
            {"meshData", meshDataCSV},
            {"positionData", positionDataCSV},
            {"fishData", fishDataCSV},
            {"thermoclineData", thermoclineDataCSV},
            {"weatherData", weatherDataCSV},
            {"ysiData", ysiDataCSV}
        };

        processor = new DataProcessor(textAssetDict, NDVI);
        processor.ReadData();
    }

    private void Start()
    {
        StartCoroutine(SetupWorld());
    }

    private void FixedUpdate()
    {
        fishManager.UpdateFish();
        sunController.AdjustSunPosition();
        moonController.AdjustMoonPosition();
        ThermoclineDOMain.instance.UpdateBars();
        WindWeatherMain.instance.UpdateWindWeather();

        // if (!called)
        // {
        //     try
        //     {
        //         GetData();
        //         called = true;
        //     }
        //     catch { throw; }
        // }
        
    }

    private IEnumerator SetupWorld()
    {
        // fishManager.SetUpFish();
        // fishDictAssembled?.Invoke();
        // meshManager.SetUpMesh();
        fishManager = new FishManager(managerObject);
        fishList.PopulateList();

        yield return new WaitForSeconds(0.1f);
        TimeManager.instance.PlayButton();
        // Debug.Log(DatabaseConnection.connected);
        // dbCon.DoIt();
    }

    // private async void ConnectToDB() { await DatabaseConnection.ConnectAsync(); }
    
    private void GetData()
    {
        FishPacket packet = DatabaseConnection.GetMetaData(2054);

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