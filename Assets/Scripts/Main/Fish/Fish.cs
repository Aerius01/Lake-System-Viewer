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
    private FishUtils utils;
    private FishHighlighter highlighter;

    // Fish specific data
    public int id {get; private set;}
    public int? length {get; private set;}
    public int? speciesCode {get; private set;}
    public int? weight {get; private set;}
    public string speciesName{ get; private set;}
    public bool? male {get; private set;}
    public DateTime earliestTime {get; private set;}
    public DateTime latestTime {get; private set;}

    public (bool?, string, int?, int?, float) stats
    {
        get
        {
            return (this.male, this.speciesName, this.weight, this.length, this.fishObject.transform.position.y);
        }
    }

    public Vector3 currentPosition {get {return this.fishObject.transform.position;} }
   
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

        try {this.length = (int)float.Parse(fishInfo["tl"].ToString());}
        catch (FormatException) {this.length = null;}

        try {this.weight = (int)float.Parse(fishInfo["weight"].ToString());}
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

    public void SetFishHighlighter(FishHighlighter highlighter)
    {
        this.highlighter = highlighter;
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

    public void ActivateUtil(string util, bool activationStatus)
    {
        this.utils.ActivateUtil(util, activationStatus);
    }

    public void ResetFishColor()
    {
        this.highlighter.ResetColor();
    }

    public void SetFishColor(Color color)
    {
        this.highlighter.SetColor(color);

    }

    public void LookAtFish()
    {
        Camera.main.transform.LookAt(this.fishObject.transform);
    }

    public void UpdateCanvasText()
    {
        this.utils.newCanvasText = string.Format(
            "Fish ID: {0}\nDepth: {1:##0.00}", 
            this.id,
            this.fishObject.transform.position.y / UserSettings.verticalScalingFactor
        );
    }

    public void UpdateDepthIndicatorLine(float ratio)
    {
        Vector3 LinePoint = this.fishObject.transform.position = Vector3.Lerp(this.startPos, this.endPos, ratio);
        this.utils.UpdateDepthIndicatorLine(LinePoint);
    }

    public void UpdateFishPosition(bool timeJump)
    {
        int currentIndex = this.lastRung == null ? 1 : (int)this.lastRung;
        if (timeJump || this.lastRung == null)
        {
            // Search with no prior information
            currentIndex = Array.BinarySearch(this.timeVector, TimeManager.instance.currentTime);
        }
        else if (this.lastRung != null)
        {
            if (DateTime.Compare(TimeManager.instance.currentTime, this.timeVector[(int)this.lastRung]) > 0)
            {
                // We've moved forward a time step, but have prior information
                int remainingVectorLength = this.timeVector.Length - ((int)this.lastRung - 1);
                currentIndex = Array.BinarySearch(this.timeVector, (int)this.lastRung - 1, remainingVectorLength, TimeManager.instance.currentTime);
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
            if (currentIndex != this.lastRung)
            {
                this.startPos = new Vector3(this.dataPoints[currentIndex - 1].y, 
                    this.dataPoints[currentIndex - 1].z * UserSettings.verticalScalingFactor, 
                    this.dataPoints[currentIndex - 1].x);

                this.endPos = new Vector3(this.dataPoints[currentIndex].y,
                    this.dataPoints[currentIndex].z * UserSettings.verticalScalingFactor,
                    this.dataPoints[currentIndex].x);

                this.startOrient = this.fishObject.transform.rotation;

                if (this.endPos - this.startPos == new Vector3(0f, 0f, 0f))
                {
                    this.endOrient = Quaternion.Euler(20f, 0f, 0f);
                }
                else
                {
                    this.endOrient = Quaternion.LookRotation(this.endPos - this.startPos, Vector3.up);
                }

                this.lastRung = currentIndex;
            }

            float ratio = Convert.ToSingle((double)(TimeManager.instance.currentTime - this.dataPoints[currentIndex - 1].obsTime).Ticks 
                / (double)(this.dataPoints[currentIndex].obsTime - this.dataPoints[currentIndex - 1].obsTime).Ticks);
            
            bool levelFish = false;

            // Section to enhance fish rotation believability
            if (Vector3.Magnitude(this.endPos - this.startPos) > 5f)
            {
                // longer distance, keep extreme angle but level off in final 20% of movement
                if ((this.endOrient.eulerAngles.x > 25f || this.endOrient.eulerAngles.x < -25f) && ratio >= 0.8)
                {
                    levelFish = true;

                    Vector3 currentAngles = this.endOrient.eulerAngles;
                    this.startOrient = this.fishObject.transform.rotation;

                    if (this.endOrient.eulerAngles.x > 25f)
                    {
                        currentAngles.x = 25f;
                    }
                    else
                    {
                        currentAngles.x = -25f;
                    }

                    this.endOrient = Quaternion.Euler(currentAngles);
                }
            }
            else
            {
                // shorter distance, remove extreme angles
                if (this.endOrient.eulerAngles.x > 25f || this.endOrient.eulerAngles.x < -25f)
                {
                    Vector3 currentAngles = this.endOrient.eulerAngles;
                    this.startOrient = this.fishObject.transform.rotation;

                    if (this.endOrient.eulerAngles.x > 25f)
                    {
                        currentAngles.x = 25f;
                    }
                    else
                    {
                        currentAngles.x = -25f;
                    }

                    this.endOrient = Quaternion.Euler(currentAngles);
                }
            }

            // SLERP according to required method
            if (levelFish)
            {
                // map the final range [0.8, 1] to the range [0, 1]
                this.fishObject.transform.rotation = Quaternion.Slerp(this.startOrient, this.endOrient, (float)(5 * (ratio - 0.8)));
            }
            else
            {
                // use an exponentiated interpolator for rotation (1 - e^-10x), and linear for position. SLERP AND LERP!!
                this.fishObject.transform.rotation = Quaternion.Slerp(this.startOrient, this.endOrient, (float)(1 - Math.Pow(Math.E,(-10*ratio))));
            }

            // Update info text
            this.UpdateCanvasText();

            // Update depth indicator line
            this.UpdateDepthIndicatorLine(ratio);
        }
    }
}