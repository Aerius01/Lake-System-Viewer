using System.Collections.Generic;
using UnityEngine;
using System.Collections;

public class Main : MonoBehaviour
{
    [SerializeField]
    private FishGeneratorNew fishGeneratorNew;
    [SerializeField]
    private MeshGeneratorNew meshGeneratorNew;
    [SerializeField]
    private SunController sunController;
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
        fishGeneratorNew.UpdateFish();
        sunController.AdjustSunPosition();
        ThermoclineDOMain.instance.UpdateBars();
    }

    private IEnumerator SetupWorld()
    {
        fishGeneratorNew.SetUpFish();
        meshGeneratorNew.SetUpMesh();
        fishList.PopulateList();
        yield return new WaitForSeconds(0.1f);
        TimeManager.instance.PlayButton();
    }
}