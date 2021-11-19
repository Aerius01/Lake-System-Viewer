using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Threading.Tasks;
using System.Linq;
using TMPro;

public class Main : MonoBehaviour
{
    FishGeneratorNew fishGeneratorNew;
    MeshGeneratorNew meshGeneratorNew;
    SunController sunController;

    private void Awake()
    {
        fishGeneratorNew = GameObject.Find("FishManager").GetComponent<FishGeneratorNew>();
        meshGeneratorNew = GameObject.Find("HeightMap").transform.Find("MeshGenerator").GetComponent<MeshGeneratorNew>();
        sunController = GameObject.Find("SkyBox").transform.Find("Sun").GetComponent<SunController>();
    }

    private void Start()
    {
        StartCoroutine(SetupWorld());
    }

    private void FixedUpdate()
    {
        fishGeneratorNew.UpdateFish();
        sunController.AdjustSunPosition();
    }

    private IEnumerator SetupWorld()
    {
        fishGeneratorNew.SetUpFish();
        meshGeneratorNew.SetUpMesh();
        yield return new WaitForSeconds(0.1f);
        TimeManager.instance.PlayButton();
    }

}