using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class Fish
{   
    // Utility class representing each fish
    public Vector3 startPos, endPos;
    public Quaternion startOrient, endOrient;
    public DateTime earliestTime, latestTime;
    public GameObject fishObject = null;
    public DataPointClass[] dataPoints;
    public int totalReadings, id;
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
    }

    public void SetUpFish()
    {
        // This is separated from Start() because the parsing requires the limits of the heightmap
        //  // when converting the GIS to local coordinates
        fishReader.parseFishData();
        fishDict = new Dictionary<int, Fish>();

        foreach (var key in fishReader.parsedData.Keys)
        {
            Fish fish = new Fish();
            fish.id = key;
            fish.dataPoints = fishReader.parsedData[key];
            fish.totalReadings = fish.dataPoints.Length;
            fish.startPos = new Vector3(fishReader.parsedData[key][0].x, fishReader.parsedData[key][0].z, fishReader.parsedData[key][0].y);
            fish.startOrient = Quaternion.Euler(0f, 0f, 0f);
            fish.earliestTime = fishReader.parsedData[key][0].obsTime;
            fish.latestTime = fishReader.parsedData[key][fish.totalReadings - 1].obsTime;

            fishDict.Add(key, fish);
        }
    }

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
                if (fishDict[key].fishObject == null)
                {
                    GameObject obj = (Instantiate (fishPrefab, fishDict[key].startPos, fishDict[key].startOrient) as GameObject);

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
        int currentRung = 1;

        while (currentRung < fish.totalReadings)
        {
            if (fish.dataPoints[currentRung].obsTime >= TimeManager.dateTimer)
            {
                // Set the new positions if we're in a different rung
                if (fish.endPos != new Vector3(fish.dataPoints[currentRung].x, fish.dataPoints[currentRung].z, fish.dataPoints[currentRung].y))
                {
                    fish.startPos = new Vector3(fish.dataPoints[currentRung - 1].x, fish.dataPoints[currentRung - 1].z, fish.dataPoints[currentRung - 1].y);
                    fish.endPos = new Vector3(fish.dataPoints[currentRung].x, fish.dataPoints[currentRung].z, fish.dataPoints[currentRung].y);

                    // Use the terminal rotation from the previous rung if possible, otherwise the current rotation
                    if (currentRung >= 2)
                    {
                        fish.startOrient = Quaternion.LookRotation(fish.startPos - (new Vector3(fish.dataPoints[currentRung - 2].x, fish.dataPoints[currentRung - 2].z, fish.dataPoints[currentRung - 2].y)), Vector3.up);
                    }
                    else
                    {
                        fish.startOrient = fish.fishObject.transform.rotation;
                    }

                    fish.endOrient = Quaternion.LookRotation(fish.endPos - fish.startPos, Vector3.up);
                }

                float ratio = Convert.ToSingle((double)(TimeManager.dateTimer - fish.dataPoints[currentRung - 1].obsTime).Ticks 
                    / (double)(fish.dataPoints[currentRung].obsTime - fish.dataPoints[currentRung - 1].obsTime).Ticks);

                // TODO: make rotations more realistic by leveling the fish off at the end of the motion (prevent a fish from looking straight up)
                // use an exponentiated inerpolator for rotation (1 - e^-10x), and linear for position. SLERP AND LERP!!
                fish.fishObject.transform.rotation = Quaternion.Slerp(fish.startOrient, fish.endOrient, (float)(1 - Math.Pow(Math.E,(-10*ratio))));
                fish.fishObject.transform.position = Vector3.Lerp(fish.startPos, fish.endPos, ratio);

                break;
            }

            currentRung++;
        }
    }
}
