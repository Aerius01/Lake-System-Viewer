using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;
using UnityEngine;
using TMPro;

public class EventSystemManager : MonoBehaviour
{
    public GameObject settingsMenu, waterObject;

    // Start is called before the first frame update
    void Start()
    {
        
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

    public void AdjustWaterHeight()
    {
        if (!string.IsNullOrEmpty(settingsMenu.transform.Find("Inputs").transform.Find("WaterLevel").transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text) 
            || !string.IsNullOrWhiteSpace(settingsMenu.transform.Find("Inputs").transform.Find("WaterLevel").transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text))
        {
            waterObject.transform.position = new Vector3(
                waterObject.transform.position.x, 
                float.Parse(settingsMenu.transform.Find("Inputs").transform.Find("WaterLevel").transform.Find("WaterLevelInput").GetComponent<TMP_InputField>().text), 
                waterObject.transform.position.z);
        }
    }
}
