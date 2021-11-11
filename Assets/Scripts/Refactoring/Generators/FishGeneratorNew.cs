using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Data;
using TMPro;

public class Fisch
{   
    // Utility class representing each fish
    public Vector3 startPos, endPos;
    public Quaternion startOrient, endOrient;
    public DateTime earliestTime, latestTime;
    public GameObject fishObject = null, canvasObject = null, depthLineObject = null, trailObject = null;
    public DataPointClass[] dataPoints;
    public int totalReadings, id;
}

public class FishGeneratorNew : MonoBehaviour
{
    public static Dictionary<int, Fish> fishDict;
    public GameObject fishPrefab;
    [HideInInspector]
    public float scalingFactor = 1f;

    public void SetUpFish()
    {
        // This is separated from Start() because the parsing requires the limits of the heightmap
        //  // when converting the GIS to local coordinates

        Dictionary<int, DataPointClass[]> parsedData = createDataStructure(PositionData.instance.stringTable);
        fishDict = new Dictionary<int, Fish>();

        foreach (var key in parsedData.Keys)
        {
            Fish fish = new Fish();
            fish.id = key;
            fish.dataPoints = parsedData[key];
            fish.totalReadings = fish.dataPoints.Length;
            fish.startPos = new Vector3(parsedData[key][0].x, parsedData[key][0].z * scalingFactor, parsedData[key][0].y);
            fish.startOrient = Quaternion.Euler(0f, 0f, 0f);
            fish.earliestTime = parsedData[key][0].obsTime;
            fish.latestTime = parsedData[key][fish.totalReadings - 1].obsTime;

            GameObject obj = (Instantiate (fishPrefab, fish.startPos, fish.startOrient) as GameObject);
            obj.transform.parent = this.gameObject.transform;
            obj.name = string.Format("{0}", fish.id);
            fish.fishObject = obj;
            fish.canvasObject = obj.transform.Find("Canvas").gameObject;
            fish.depthLineObject = obj.transform.Find("DepthLine").gameObject;
            fish.trailObject = obj.transform.Find("Trail").gameObject;

            fish.canvasObject.SetActive(false);
            fish.depthLineObject.SetActive(false);
            fish.trailObject.SetActive(false);
            obj.SetActive(false);

            fishDict.Add(key, fish);
        }
    }

    public void UpdateFish()
    {
        foreach (var key in fishDict.Keys)
        {
            // if before first time step or after last, check if despawned
            if (TimeManager.dateTimer < fishDict[key].earliestTime || TimeManager.dateTimer > fishDict[key].latestTime)
            {
                if (fishDict[key].fishObject.activeSelf == true)
                {
                    fishDict[key].fishObject.SetActive(false);
                }
            }
            else
            {
                // spawn the fish if it isn't already
                if (fishDict[key].fishObject.activeSelf == false)
                {
                    fishDict[key].fishObject.SetActive(true);
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
        bool levelFish = false;

        while (currentRung < fish.totalReadings)
        {
            if (fish.dataPoints[currentRung].obsTime >= TimeManager.dateTimer)
            {
                // Set the new positions if we're in a different rung
                if (fish.endPos != new Vector3(fish.dataPoints[currentRung].x, fish.dataPoints[currentRung].z * scalingFactor, fish.dataPoints[currentRung].y))
                {
                    fish.startPos = new Vector3(fish.dataPoints[currentRung - 1].x, fish.dataPoints[currentRung - 1].z * scalingFactor, fish.dataPoints[currentRung - 1].y);
                    fish.endPos = new Vector3(fish.dataPoints[currentRung].x, fish.dataPoints[currentRung].z * scalingFactor, fish.dataPoints[currentRung].y);

                    fish.startOrient = fish.fishObject.transform.rotation;

                    if (fish.endPos - fish.startPos == new Vector3(0f, 0f, 0f))
                    {
                        fish.endOrient = Quaternion.Euler(20f, 0f, 0f);
                    }
                    else
                    {
                        fish.endOrient = Quaternion.LookRotation(fish.endPos - fish.startPos, Vector3.up);
                    }
                }

                float ratio = Convert.ToSingle((double)(TimeManager.dateTimer - fish.dataPoints[currentRung - 1].obsTime).Ticks 
                    / (double)(fish.dataPoints[currentRung].obsTime - fish.dataPoints[currentRung - 1].obsTime).Ticks);


                // Section to enhance fish rotation believability
                if (Vector3.Magnitude(fish.endPos - fish.startPos) > 5f)
                {
                    // longer distance, keep extreme angle but level off in final 20% of movement
                    if ((fish.endOrient.eulerAngles.x > 25f || fish.endOrient.eulerAngles.x < -25f) && ratio >= 0.8)
                    {
                        levelFish = true;

                        Vector3 currentAngles = fish.endOrient.eulerAngles;
                        fish.startOrient = fish.fishObject.transform.rotation;

                        if (fish.endOrient.eulerAngles.x > 25f)
                        {
                            currentAngles.x = 25f;
                        }
                        else
                        {
                            currentAngles.x = -25f;
                        }

                        fish.endOrient = Quaternion.Euler(currentAngles);
                    }
                }
                else
                {
                    // shorter distance, remove extreme angles
                    if (fish.endOrient.eulerAngles.x > 25f || fish.endOrient.eulerAngles.x < -25f)
                    {
                        Vector3 currentAngles = fish.endOrient.eulerAngles;
                        fish.startOrient = fish.fishObject.transform.rotation;

                        if (fish.endOrient.eulerAngles.x > 25f)
                        {
                            currentAngles.x = 25f;
                        }
                        else
                        {
                            currentAngles.x = -25f;
                        }

                        fish.endOrient = Quaternion.Euler(currentAngles);
                    }
                }

                // SLERP according to required method
                if (levelFish)
                {
                    // map the final range [0.8, 1] to the range [0, 1]
                    fish.fishObject.transform.rotation = Quaternion.Slerp(fish.startOrient, fish.endOrient, (float)(5 * (ratio - 0.8)));
                }
                else
                {
                    // use an exponentiated interpolator for rotation (1 - e^-10x), and linear for position. SLERP AND LERP!!
                    fish.fishObject.transform.rotation = Quaternion.Slerp(fish.startOrient, fish.endOrient, (float)(1 - Math.Pow(Math.E,(-10*ratio))));
                }

                Vector3 LinePoint = fish.fishObject.transform.position = Vector3.Lerp(fish.startPos, fish.endPos, ratio);

                // Update info text
                fish.canvasObject.transform.Find("Panel").transform.Find("Background").transform.Find("InfoText").
                    GetComponent<TextMeshProUGUI>().text = string.Format("Fish ID: {0}\nDepth: {1:##0.00}", 
                    fish.id, fish.fishObject.transform.position.y / scalingFactor);

                // Update depth indicator line
                LineRenderer line = fish.fishObject.transform.Find("DepthLine").GetComponent<LineRenderer>();
                GameObject waterblock = GameObject.Find("WaterBlock");
                
                // TODO: find the lake depth at the position of the fish
                LinePoint.y = - MeshDataReader.maxDepth * scalingFactor;
                line.SetPosition(0, LinePoint);

                LinePoint.y = waterblock.transform.position.y;
                line.SetPosition(1, LinePoint);

                break;
            }

            currentRung++;
        }
    }

    // Utility functions for set-up
    Dictionary<int, DataPointClass[]> createDataStructure(DataTable stringGrid)
    {
        string[] stringKeys = SliceCol(stringGrid, 0).Distinct().ToArray();
        Dictionary<int, DataPointClass[]> dataSet = new Dictionary<int, DataPointClass[]>();

        foreach (string key in stringKeys)
        {
            DataPointClass[] positions = SliceMultipleColsByKey(stringGrid, 1, 4, key);
            dataSet.Add(int.Parse(key.Trim()), positions);
        }
        
        return dataSet;
    }

    string[] SliceCol(DataTable array, int column)
    {
        string[] arraySlice = new string[array.Rows.Count];
        for (int y = 0; y < array.Rows.Count; y++)
        {
            arraySlice[y] = array.Rows[y][column].ToString().Trim();
        }

        return arraySlice;
    }

    DataPointClass[] SliceMultipleColsByKey(DataTable array, int from, int to, string key)
    {
        int firstInstance = 0;
        while (array.Rows[firstInstance]["ID"].ToString() != key){
            firstInstance++;
        }

        int lastInstance = firstInstance;
        while (array.Rows[lastInstance]["ID"].ToString() == key && lastInstance != array.Rows.Count - 1){
            lastInstance++;
        }

        if (lastInstance != array.Rows.Count - 1){
            lastInstance -= 1; 
        }
        
        int cutSize = lastInstance - firstInstance + 1;
        List<DataPointClass> slicedList = new List<DataPointClass>();

        for (int y = 0; y < cutSize; y++)
        {
            DataPointClass record = new DataPointClass();

            record.x = float.Parse(array.Rows[y + firstInstance]["x"].ToString());
            record.y = float.Parse(array.Rows[y + firstInstance]["y"].ToString());
            record.z = - float.Parse(array.Rows[y + firstInstance]["D"].ToString());

            DateTime parsedDate = DateTime.Parse(array.Rows[y + firstInstance]["Time"].ToString());
            record.obsTime = parsedDate;
            slicedList.Add(record);
        }

        DataPointClass[] sorted = slicedList.OrderBy(f => f.obsTime).ToArray();
        return sorted;
    }
}
