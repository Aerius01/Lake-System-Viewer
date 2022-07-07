using UnityEngine;
using System;
using System.Data;

public class Fish
{   
    // World position data
    public Vector3 startPos, endPos;
    public Quaternion startOrient, endOrient;
    public int? lastRung;
    private int cutoffCounter = 0;
    private bool smallDist = false;
    private float extremeFishAngle = 15f;

    // The actual object in-game
    public GameObject fishObject {get; private set;} = null;
    public bool fishShouldExist
    {
        get
        {
            if ((DateTime.Compare(TimeManager.instance.currentTime, this.earliestTime) < 0 
                || DateTime.Compare(TimeManager.instance.currentTime, this.latestTime) > 0) || spawnOverride)
                return false;
            else return true;
        }
    }
    public bool spawnOverride = false;
    
    public bool canvasActive {get { return utils.canvas.activeSelf; }} 
    public bool depthLineActive {get { return utils.depthLine.activeSelf; }} 
    public bool trailActive {get { return utils.trail.activeSelf; }} 
    public bool thermoIndActive {get { return utils.thermoInd.activeSelf; }}
    public float maxExtent { get { return utils.MaxExtent(); } }
    public Vector3 extents { get { return utils.Extents(); } }

    // Full point lists
    public DataPointClass[] dataPoints {get; private set;}
    public DateTime[] timeVector {get; private set;}
    public int totalReadings {get; private set;}

    // Utility class representing depth lines, tags, etc
    private FishUtils utils;
    private FishHighlighter highlighter;

    // Fish specific data
    public int id {get; private set;}
    public string captureType {get; private set;}
    public int? length {get; private set;}
    public int? speciesCode {get; private set;}
    public int? weight {get; private set;}
    public string speciesName{ get; private set;}
    public bool? male {get; private set;}
    public DateTime earliestTime {get; private set;}
    public DateTime latestTime {get; private set;}
    private string canvasTextPart1, canvasTextPart2;

    public (bool?, string, int?, int?, float, bool) stats
    {
        get
        {
            return (this.male, this.speciesName, this.weight, this.length, this.fishObject.transform.position.y / UserSettings.verticalScalingFactor, this.fishShouldExist);
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

        this.canvasTextPart1 = string.Format("Fish ID: {0}\nDepth: ", this.id);
        this.canvasTextPart2 = string.Format("\nSpecies: {0}\nSex: {1}", this.speciesName, this.male == null ? " ? " : this.male == true ? "M" : "F");
    }

    public void SetFishUtils(FishUtils utils) { this.utils = utils; }

    public void SetFishHighlighter(FishHighlighter highlighter) { this.highlighter = highlighter; }

    public void SetFishGameObject(GameObject obj) { this.fishObject = obj; }

    public void Deactivate() { this.fishObject.SetActive(false); }

    public void Activate() { this.fishObject.SetActive(true); }

    public bool FishIsActive() { return this.fishObject.activeSelf; }

    public void ActivateUtil(string util, bool activationStatus) { this.utils.ActivateUtil(util, activationStatus); }

    public void ResetFishColor() { this.highlighter.ResetColor(); }

    public void SetFishColor(Color color) { this.highlighter.SetColor(color); }

    public void LookAtFish() { Camera.main.transform.LookAt(this.fishObject.transform); }

    public void UpdateFishScale(float newVal)
    {
        Vector3 currentScale = this.fishObject.transform.localScale;
        currentScale.x = currentScale.x * newVal / UserSettings.fishScalingFactor;
        currentScale.y = currentScale.y * newVal / UserSettings.fishScalingFactor;
        currentScale.z = currentScale.z * newVal / UserSettings.fishScalingFactor;

        this.fishObject.transform.localScale = currentScale;
    }

    public void UpdateCanvasText()
    {
        string fullText =
            this.canvasTextPart1 +
            string.Format("{0:##0.00}", this.fishObject.transform.position.y / UserSettings.verticalScalingFactor) +
            this.canvasTextPart2;

        this.utils.newCanvasText = fullText;
    }

    public void UpdateFishPosition(bool timeJump, bool scaleChange)
    {
        int currentIndex = this.lastRung == null ? 1 : (int)this.lastRung;
        if (timeJump || this.lastRung == null)
        {
            // Search with no prior information
            currentIndex = Array.BinarySearch(this.timeVector, TimeManager.instance.currentTime);
            cutoffCounter = 0;
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
            if (currentIndex < 0) { currentIndex = Mathf.Abs(currentIndex) - 1; }

            // find new bounding values if we've entered a new timestep range or vertical scale has changed
            if (currentIndex != this.lastRung || scaleChange)
            {   
                // scale changes break the update without adjustment since index is not incremented, but cutoff is
                if (scaleChange && smallDist) cutoffCounter -= 1;         

                this.startPos = new Vector3(this.dataPoints[currentIndex - 1 - cutoffCounter].x + LocalMeshData.cutoffs["minWidth"], 
                    this.dataPoints[currentIndex - 1 - cutoffCounter].z * UserSettings.verticalScalingFactor, 
                    LocalMeshData.cutoffs["maxHeight"] - this.dataPoints[currentIndex - 1 - cutoffCounter].y)
                ;
                
                this.endPos = new Vector3(this.dataPoints[currentIndex].x + LocalMeshData.cutoffs["minWidth"],
                    this.dataPoints[currentIndex].z * UserSettings.verticalScalingFactor,
                    LocalMeshData.cutoffs["maxHeight"] - this.dataPoints[currentIndex].y)
                ;

                this.startOrient = this.fishObject.transform.rotation;

                if (this.endPos - this.startPos == new Vector3(0f, 0f, 0f)) this.endOrient = this.fishObject.transform.rotation;
                else this.endOrient = Quaternion.LookRotation(this.endPos - this.startPos, Vector3.up);

                // Determine whether or not to ignore the position reading
                if (Vector3.Magnitude(this.endPos - this.startPos) > UserSettings.cutoffDist)
                {
                    smallDist = false;
                    cutoffCounter = 0;
                }
                else
                {
                    smallDist = true;
                    cutoffCounter += 1;
                }

                this.lastRung = currentIndex;
            }

            if (!smallDist)
            {
                float ratio = Convert.ToSingle((double)(TimeManager.instance.currentTime - this.dataPoints[currentIndex - 1].obsTime).Ticks 
                / (double)(this.dataPoints[currentIndex].obsTime - this.dataPoints[currentIndex - 1].obsTime).Ticks);
            
                bool levelFish = false;

                // Section to enhance fish rotation believability
                if (Vector3.Magnitude(this.endPos - this.startPos) > 5f)
                {
                    // longer distance, keep extreme angle but level off in final 20% of movement
                    if ((this.endOrient.eulerAngles.x > extremeFishAngle || this.endOrient.eulerAngles.x < -extremeFishAngle) && ratio >= 0.8)
                    {
                        levelFish = true;

                        Vector3 currentAngles = this.endOrient.eulerAngles;
                        this.startOrient = this.fishObject.transform.rotation;

                        if (this.endOrient.eulerAngles.x > extremeFishAngle) { currentAngles.x = extremeFishAngle; }
                        else { currentAngles.x = -extremeFishAngle; }

                        this.endOrient = Quaternion.Euler(currentAngles);
                    }
                }
                else
                {
                    // shorter distance, remove extreme angles
                    if (this.endOrient.eulerAngles.x > extremeFishAngle || this.endOrient.eulerAngles.x < -extremeFishAngle)
                    {
                        Vector3 currentAngles = this.endOrient.eulerAngles;
                        this.startOrient = this.fishObject.transform.rotation;

                        if (this.endOrient.eulerAngles.x > extremeFishAngle) { currentAngles.x = extremeFishAngle; }
                        else { currentAngles.x = -extremeFishAngle; }

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

                // Update both position (LERP) and depth indicator line
                Vector3 LinePoint = this.fishObject.transform.position = Vector3.Lerp(this.startPos, this.endPos, ratio);
                this.utils.UpdateDepthIndicatorLine(LinePoint);

                // Update info text
                this.UpdateCanvasText();
            }
        }
    }
}