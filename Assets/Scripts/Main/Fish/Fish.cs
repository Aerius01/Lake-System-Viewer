using UnityEngine;
using System;
using System.Data;

public class Fish
{   
    // World position data
    public Vector3 startPos, endPos;
    public Quaternion startOrient, endOrient;
    public int? lastRung;

    // The actual object in-game
    public GameObject fishObject {get; private set;} = null;
    public bool fishShouldExist
    {
        get
        {
            if (DateTime.Compare(TimeManager.instance.currentTime, this.earliestTime) < 0 
                || DateTime.Compare(TimeManager.instance.currentTime, this.latestTime) > 0)
                return true;
            else return false;
        }
    }

    // Full point lists
    public DataPointClass[] dataPoints {get; private set;}
    public DateTime[] timeVector {get; private set;}
    public int totalReadings {get; private set;}

    // Utility class representing depth lines, tags, etc
    public FishUtils utils {get; private set;}

    // Fish specific data
    public int id {get; private set;}
    public int? length {get; private set;}
    public int? speciesCode {get; private set;}
    public int? weight {get; private set;}
    public string speciesName{ get; private set;}
    public bool? male {get; private set;}
    public DateTime earliestTime {get; private set;}
    public DateTime latestTime {get; private set;}
   
    public Fish (int id)
    {
        this.id = id;

        // Position information
        this.dataPoints = LocalPositionData.positionDict[id];
        this.totalReadings = dataPoints.Length;
        this.earliestTime = dataPoints[0].obsTime;
        this.latestTime = dataPoints[totalReadings - 1].obsTime;
        this.startPos = new Vector3(
            dataPoints[0].x, 
            dataPoints[0].z * UserSettings.verticalScalingFactor, 
            dataPoints[0].y
        );
        this.startOrient = Quaternion.Euler(0f, 0f, 0f);

        this.timeVector = new DateTime[dataPoints.Length];
        for (int i = 0; i < dataPoints.Length; i++)
        {
            timeVector[i] = dataPoints[i].obsTime;
        }

        // Fish information
        DataRow fishInfo = LocalFishData.fishDict[id];

        try {this.length = int.Parse(fishInfo["tl"].ToString());}
        catch (FormatException) {this.length = null;}

        try {this.weight = int.Parse(fishInfo["weight"].ToString());}
        catch (FormatException) {this.weight = null;}

        try {this.speciesCode = int.Parse(fishInfo["speciesCode"].ToString());}
        catch (FormatException) {this.speciesCode = null;}

        if (fishInfo["male"].ToString() == "1") {this.male = true;}
        else if (fishInfo["male"].ToString() == "0") {this.male = false;}
        else {this.male = null;}

        this.speciesName = fishInfo["speciesName"].ToString();
    }

    public void SetFishUtils(FishUtils utils)
    {
        this.utils = utils;
    }

    public void SetFishGameObject(GameObject obj)
    {
        this.fishObject = obj;
    }

    public void Deactivate()
    {
        this.fishObject.SetActive(false);
    }

    public void Activate()
    {
        this.fishObject.SetActive(true);
    }

    public bool FishIsActive()
    {
        return this.fishObject.activeSelf;
    }
}