using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Fish
{   
    // Utility class for the fish
    public Vector3 startPos, startOrient, endPos, endOrient;
    public DateTime earliestTime, latestTime;
    public GameObject fishObject = null;
    public DataPointClass[] dataPoints;
    public int totalReadings;
}

public class FishGenerator : MonoBehaviour
{
    Dictionary<int, Fish> fishDict;
    FishDataReader fishReader;
    Fish fishClass;
    public GameObject fishDataUploadObject, fishPrefab;
    public bool hasHeaders, removeIdCol;

    // Start is called before the first frame update
    private void Start()
    {
        fishReader = new FishDataReader();
        fishReader.ReadData(fishDataUploadObject, hasHeaders, removeIdCol);

        fishDict = new Dictionary<int, Fish>();

        foreach (var key in fishReader.parsedData.Keys)
        {
            Fish fish = new Fish();
            fish.dataPoints = fishReader.parsedData[key];
            fish.totalReadings = fish.dataPoints.Length;
            fish.startPos = new Vector3(fishReader.parsedData[key][0].x, fishReader.parsedData[key][0].y, fishReader.parsedData[key][0].z);
            fish.startOrient = new Vector3(0f, 0f, 0f);
            fish.earliestTime = fishReader.parsedData[key][0].obsTime;
            fish.latestTime = fishReader.parsedData[key][fish.totalReadings - 1].obsTime;

            fishDict.Add(key, fish);
        }
    }

    // Update is called once per frame
    public void UpdateFish()
    {
        foreach (var key in fishDict.Keys)
        {
            // if before first time step, do nothing
            if (TimeManager.dateTimer < fishDict[key].earliestTime)
            {

            }
            // if after last time step, check if despawned
            else if (TimeManager.dateTimer > fishDict[key].latestTime)
            {
                if (fishDict[key].fishObject.activeSelf == true)
                {
                    fishDict[key].fishObject.SetActive(false);
                }
            }
            else
            {
                // spawn the fish if it isn't already
                // TODO: initial interpolation of position w/ timestamp
                if (fishDict[key].fishObject == null)
                {
                    GameObject obj = (Instantiate (fishPrefab, fishDict[key].startPos, Quaternion.Euler(fishDict[key].startOrient)) as GameObject);

                    obj.transform.parent = this.gameObject.transform;
                    fishDict[key].fishObject = obj;
                }
                // Update position if already spawned
                else
                {
                    UpdateFishPosition(fishDict[key]);
                }
            }
        }
    }

    void UpdateFishPosition(Fish fish)
    {
        // TODO: update fish orientations

        int currentRung = 1;

        while (currentRung < fish.totalReadings)
        {
            if (fish.dataPoints[currentRung].obsTime >= TimeManager.dateTimer)
            {
                fish.startPos = new Vector3(fish.dataPoints[currentRung - 1].x, fish.dataPoints[currentRung - 1].y, fish.dataPoints[currentRung - 1].z);
                fish.endPos = new Vector3(fish.dataPoints[currentRung].x, fish.dataPoints[currentRung].y, fish.dataPoints[currentRung].z);

                float ratio = Convert.ToSingle((double)(TimeManager.dateTimer - fish.dataPoints[currentRung - 1].obsTime).Ticks / (double)(fish.dataPoints[currentRung].obsTime - fish.dataPoints[currentRung - 1].obsTime).Ticks);
                
                fish.fishObject.transform.position = Vector3.Lerp(fish.startPos, fish.endPos, ratio);

                break;
            }

            currentRung++;
        }
    }
}
