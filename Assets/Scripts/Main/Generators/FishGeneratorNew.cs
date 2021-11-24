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
    public GameObject fishObject = null;
    public DataPointClass[] dataPoints;
    public FishUtils utils;
    public int totalReadings, id;
    public int? lastRung;
    public DateTime[] timeVector;

    public Fisch (int id, DataPointClass[] dataPoints)
    {
        this.id = id;
        this.dataPoints = dataPoints;
        totalReadings = dataPoints.Length;
        startPos = new Vector3(dataPoints[0].x, dataPoints[0].z * UserSettings.verticalScalingFactor, dataPoints[0].y);
        startOrient = Quaternion.Euler(0f, 0f, 0f);
        earliestTime = dataPoints[0].obsTime;
        latestTime = dataPoints[totalReadings - 1].obsTime;

        timeVector = new DateTime[dataPoints.Length];
        for (int i = 0; i < dataPoints.Length; i++)
        {
            timeVector[i] = dataPoints[i].obsTime;
        }
    }
}

public class FishGeneratorNew : MonoBehaviour
{
    private static Dictionary<int, Fisch> fishDict;
    public GameObject fishPrefab;

    public static void ActivateAll(int handle, bool activationStatus)
    {
        // 0: Tag
        // 1: Depth line
        // 2: Trail
        foreach (var key in fishDict.Keys)
        {
            if (handle == 0)
            {
                fishDict[key].utils.ActivateTag(activationStatus);
            }
            else if (handle == 1)
            {
                fishDict[key].utils.ActivateDepthLine(activationStatus);
            }
            else if (handle == 2)
            {
                fishDict[key].utils.ActivateTrail(activationStatus);
            }
        }
    }

    public static Dictionary<int, MetaData> GetFishMetaData()
    {
        Dictionary<int, MetaData> metaDict = new Dictionary<int, MetaData>();
        foreach (var key in fishDict.Keys)
        {
            metaDict.Add(key, new MetaData(fishDict[key]));
        }

        return metaDict;
    }

    public static float CurrentFishDepth(int fishID)
    {
        return fishDict[fishID].fishObject.transform.position.y;   
    }

    public void SetUpFish()
    {
        // TODO: make sure data is sorted in parsed structure
        Dictionary<int, DataPointClass[]> parsedData = CreateDataStructure(PositionData.instance.stringTable);
        fishDict = new Dictionary<int, Fisch>();

        foreach (var key in parsedData.Keys)
        {
            Fisch fish = new Fisch(key, parsedData[key]);
            
            GameObject obj = (Instantiate (fishPrefab, fish.startPos, fish.startOrient) as GameObject);
            fish.utils = obj.GetComponent<FishUtils>();
            obj.transform.parent = this.gameObject.transform;
            obj.name = string.Format("{0}", fish.id);

            fish.fishObject = obj;
            obj.SetActive(false);

            fishDict.Add(key, fish);
        }
    }

    public void UpdateFish()
    {
        bool jumpingInTime = false;
        if (PlaybackController.sliderHasChanged)
        {
            jumpingInTime = true;
        }

        foreach (var key in fishDict.Keys)
        {
            // if before first time step or after last and fish exists, despawn it
            if (DateTime.Compare(TimeManager.instance.currentTime, fishDict[key].earliestTime) < 0 
                || DateTime.Compare(TimeManager.instance.currentTime, fishDict[key].latestTime) > 0)
            {
                if (fishDict[key].fishObject.activeSelf)
                {
                    fishDict[key].fishObject.SetActive(false);
                }       
            }
            else
            {
                // spawn the fish if it isn't already
                if (!fishDict[key].fishObject.activeSelf)
                {
                    fishDict[key].fishObject.SetActive(true);
                }
                // Update position if already spawned
                else
                {
                    UpdateFishPosition(fishDict[key], jumpingInTime);
                }
            }
        }

        if (jumpingInTime)
        {
            PlaybackController.sliderHasChanged = false;
        }
    }

    private void UpdateFishPosition(Fisch fish, bool timeJump)
    {
        int currentIndex = fish.lastRung == null ? 1 : (int)fish.lastRung;
        if (timeJump || fish.lastRung == null)
        {
            // Search with no prior information
            currentIndex = Array.BinarySearch(fish.timeVector, TimeManager.instance.currentTime);
        }
        else if (fish.lastRung != null)
        {
            if (DateTime.Compare(TimeManager.instance.currentTime, fish.timeVector[(int)fish.lastRung]) > 0)
            {
                // We've moved forward a time step, but have prior information
                int remainingVectorLength = fish.timeVector.Length - ((int)fish.lastRung - 1);
                currentIndex = Array.BinarySearch(fish.timeVector, (int)fish.lastRung - 1, remainingVectorLength, TimeManager.instance.currentTime);
            }
        }

        if (currentIndex != 0)
        {
            // Only operate if we have timestep bounds

            if (currentIndex < 0)
            {
                currentIndex = Mathf.Abs(currentIndex) - 1;
            }

            // find new bounding values if we've entered a new timestep range
            if (currentIndex != fish.lastRung)
            {
                fish.startPos = new Vector3(fish.dataPoints[currentIndex - 1].y, 
                    fish.dataPoints[currentIndex - 1].z * UserSettings.verticalScalingFactor, 
                    fish.dataPoints[currentIndex - 1].x);

                fish.endPos = new Vector3(fish.dataPoints[currentIndex].y,
                    fish.dataPoints[currentIndex].z * UserSettings.verticalScalingFactor,
                    fish.dataPoints[currentIndex].x);

                fish.startOrient = fish.fishObject.transform.rotation;

                if (fish.endPos - fish.startPos == new Vector3(0f, 0f, 0f))
                {
                    fish.endOrient = Quaternion.Euler(20f, 0f, 0f);
                }
                else
                {
                    fish.endOrient = Quaternion.LookRotation(fish.endPos - fish.startPos, Vector3.up);
                }

                fish.lastRung = currentIndex;
            }

            float ratio = Convert.ToSingle((double)(TimeManager.instance.currentTime - fish.dataPoints[currentIndex - 1].obsTime).Ticks 
                / (double)(fish.dataPoints[currentIndex].obsTime - fish.dataPoints[currentIndex - 1].obsTime).Ticks);
            
            bool levelFish = false;

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

            // Update info text
            fish.utils.UpdateCanvasText(string.Format("Fish ID: {0}\nDepth: {1:##0.00}", 
                fish.id, fish.fishObject.transform.position.y / UserSettings.verticalScalingFactor));

            Vector3 LinePoint = fish.fishObject.transform.position = Vector3.Lerp(fish.startPos, fish.endPos, ratio);

            // Update depth indicator line
            fish.utils.UpdateDepthIndicatorLine(LinePoint);
        }
    }



    // Utility functions for set-up
    Dictionary<int, DataPointClass[]> CreateDataStructure(DataTable stringGrid)
    {
        string[] stringKeys = SliceCol(stringGrid, "ID").Distinct().ToArray();
        Dictionary<int, DataPointClass[]> dataSet = new Dictionary<int, DataPointClass[]>();

        foreach (string key in stringKeys)
        {
            DataPointClass[] positions = SliceMultipleColsByKey(stringGrid, key);
            dataSet.Add(int.Parse(key.Trim()), positions);
        }
        
        return dataSet;
    }

    string[] SliceCol(DataTable array, string columnName)
    {
        string[] arraySlice = new string[array.Rows.Count];
        for (int y = 0; y < array.Rows.Count; y++)
        {
            arraySlice[y] = array.Rows[y][columnName].ToString().Trim();
        }

        return arraySlice;
    }

    DataPointClass[] SliceMultipleColsByKey(DataTable array, string key)
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
