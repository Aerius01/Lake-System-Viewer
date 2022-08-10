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

        if (!called)
        {
            try
            {
                GetData();
                called = true;
            }
            catch { throw; }
        }
        
    }

    private IEnumerator SetupWorld()
    {
        fishManager.SetUpFish();
        fishDictAssembled?.Invoke();
        // meshManager.SetUpMesh();
        fishList.PopulateList();
        yield return new WaitForSeconds(0.1f);
        TimeManager.instance.PlayButton();
        // Debug.Log(DatabaseConnection.connected);
        // dbCon.DoIt();
    }

    // private async void ConnectToDB() { await DatabaseConnection.ConnectAsync(); }
    
    private async void GetData()
    {
        DataPacket[] packet = await DatabaseConnection.GetFishData(FishManager.fishDict[2033]);

        foreach (DataPacket pack in packet)
        {
            Debug.Log(pack);
            Debug.Log(pack.timestamp);
            Debug.Log(pack.id);
            Debug.Log(pack.pos);
        }
        
    }
}