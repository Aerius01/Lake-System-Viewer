using System.Collections.Generic;
using UnityEngine;
using System.Collections;

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

        processor = new DataProcessor(textAssetDict);
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
    }

    private IEnumerator SetupWorld()
    {
        fishManager.SetUpFish();
        meshManager.SetUpMesh();
        fishList.PopulateList();
        yield return new WaitForSeconds(0.1f);
        TimeManager.instance.PlayButton();
    }
}