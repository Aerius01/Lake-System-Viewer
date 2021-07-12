using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class EventSystemManager : MonoBehaviour
{
    public GameObject settingsMenu, heightMapObject, fishManagerObject;

    // Start is called before the first frame update
    void Start()
    {
        settingsMenu.transform.Find("Inputs").transform.Find("ScalingFactor").transform.Find("ScalingFactorInput").GetComponent<TMP_InputField>().text = "1";
        settingsMenu.transform.Find("Inputs").transform.Find("SpeedUpCoeff").transform.Find("SpeedUpInput").GetComponent<TMP_InputField>().text = "10";
        
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
             
            if (Physics.Raycast(ray, out hit))
            {
                GameObject fishCanvas = FishGenerator.fishDict[int.Parse(hit.collider.gameObject.name)].canvasObject;
                fishCanvas.SetActive(!fishCanvas.activeSelf);
            }
        }
    }

    public void TagToggle()
    {
        if (settingsMenu.transform.Find("Toggles").transform.Find("TagToggle").GetComponent<Toggle>().isOn)
        {
            foreach (var key in FishGenerator.fishDict.Keys)
            {
                FishGenerator.fishDict[key].canvasObject.SetActive(true);
            }
        }
        else
        {
            foreach (var key in FishGenerator.fishDict.Keys)
            {
                FishGenerator.fishDict[key].canvasObject.SetActive(false);
            }
        }
    }
    public void DepthLineToggle()
    {
        if (settingsMenu.transform.Find("Toggles").transform.Find("DepthLineToggle").GetComponent<Toggle>().isOn)
        {
            foreach (var key in FishGenerator.fishDict.Keys)
            {
                FishGenerator.fishDict[key].depthLineObject.SetActive(true);
            }
        }
        else
        {
            foreach (var key in FishGenerator.fishDict.Keys)
            {
                FishGenerator.fishDict[key].depthLineObject.SetActive(false);
            }
        }
    }

    public void TrailToggle()
    {
        if (settingsMenu.transform.Find("Toggles").transform.Find("TrailToggle").GetComponent<Toggle>().isOn)
        {
            foreach (var key in FishGenerator.fishDict.Keys)
            {
                FishGenerator.fishDict[key].trailObject.SetActive(true);
            }
        }
        else
        {
            foreach (var key in FishGenerator.fishDict.Keys)
            {
                FishGenerator.fishDict[key].trailObject.SetActive(false);
            }
        }
    }

    public void AdjustWaterHeight()
    {
        if (!string.IsNullOrEmpty(settingsMenu.transform.Find("Inputs").transform.Find("WaterLevel").transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text) 
            || !string.IsNullOrWhiteSpace(settingsMenu.transform.Find("Inputs").transform.Find("WaterLevel").transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text))
        {
            GameObject waterObject = heightMapObject.transform.Find("WaterBlock").gameObject;

            waterObject.transform.position = new Vector3(
                waterObject.transform.position.x, 
                float.Parse(settingsMenu.transform.Find("Inputs").transform.Find("WaterLevel").transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text), 
                waterObject.transform.position.z);
        }
    }

    public void AdjustScalingFactor()
    {
        if (!string.IsNullOrEmpty(settingsMenu.transform.Find("Inputs").transform.Find("ScalingFactor").transform.Find("ScalingFactorInput").GetComponent<TMP_InputField>().text) 
            || !string.IsNullOrWhiteSpace(settingsMenu.transform.Find("Inputs").transform.Find("ScalingFactor").transform.Find("ScalingFactorInput").GetComponent<TMP_InputField>().text))
        {
            GameObject meshObject = heightMapObject.transform.Find("MeshGenerator").gameObject;
            float scaleValue = float.Parse(settingsMenu.transform.Find("Inputs").transform.Find("ScalingFactor").transform.Find("ScalingFactorInput").GetComponent<TMP_InputField>().text);

            if (scaleValue <= 0)
            {
                // we only want positive scaling factors
                scaleValue = 1f;
                settingsMenu.transform.Find("Inputs").transform.Find("ScalingFactor").transform.Find("ScalingFactorInput").
                    GetComponent<TMP_InputField>().text = string.Format("{0}", scaleValue);
            }
            
            Vector3 scaler = meshObject.transform.localScale;
            scaler.y = scaleValue;
            meshObject.transform.localScale = scaler;

            fishManagerObject.GetComponent<FishGenerator>().scalingFactor = scaleValue;
        }
    }

    public void AdjustTimeSpeed()
    {
        if (!string.IsNullOrEmpty(settingsMenu.transform.Find("Inputs").transform.Find("SpeedUpCoeff").transform.Find("SpeedUpInput").GetComponent<TMP_InputField>().text) 
            || !string.IsNullOrWhiteSpace(settingsMenu.transform.Find("Inputs").transform.Find("SpeedUpCoeff").transform.Find("SpeedUpInput").GetComponent<TMP_InputField>().text))
        {
            TimeManager timeManager = GameObject.Find("TimeManager").GetComponent<TimeManager>();

            float enteredValue = float.Parse(settingsMenu.transform.Find("Inputs").transform.Find("SpeedUpCoeff").transform.Find("SpeedUpInput").GetComponent<TMP_InputField>().text);

            if (enteredValue <= 0)
            {
                // we only want positive scaling factors
                enteredValue = 10f;
                settingsMenu.transform.Find("Inputs").transform.Find("ScalingFactor").transform.Find("ScalingFactorInput").
                    GetComponent<TMP_InputField>().text = string.Format("{0}", enteredValue);
            }

            timeManager.speedUpCoefficient = enteredValue;
        }
    }
}
