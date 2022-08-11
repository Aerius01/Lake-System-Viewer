using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Data;

public class FishNew : MonoBehaviour
{   
    // World position data
    public Vector3 startPos, endPos;
    public Quaternion startOrient, endOrient;
    private float extremeFishAngle = 15f;

    // The actual object in-game
    public GameObject fishObject {get; private set;} = null;
    public bool fishShouldExist
    {
        get
        {
            if ((DateTime.Compare(TimeManager.instance.currentTime, this.earliestTime) < 0 
                || DateTime.Compare(TimeManager.instance.currentTime, this.latestTime) > 0) || this.spawnOverride || !FilterManager.PassesAllFilters(this))
                return false;
            else return true;
        }
    }
    public bool fishCurrentlyExists { get { return this.fishObject.activeSelf; } }
    public bool spawnOverride = false;
    
    public bool canvasActive {get { return utils.canvas.activeSelf; }} 
    public bool depthLineActive {get { return utils.depthLine.activeSelf; }} 
    public bool trailActive {get { return utils.trail.activeSelf; }} 
    public bool thermoIndActive {get { return utils.thermoInd.activeSelf; }}
    public float maxExtent { get { return utils.maxExtent; } }
    public Vector3 extents { get { return utils.extents; } }

    // Full point lists
    public DataPointClass[] dataPoints {get; private set;}
    public DateTime[] timeVector {get; private set;}
    public int totalReadings {get; private set;}

    // Utility class representing depth lines, tags, etc
    private FishUtilsNew utils;
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

    public Vector3 currentPosition {get {return this.fishObject.transform.position;} }
    public float currentDepth { get { return this.currentPosition.y / UserSettings.verticalScalingFactor; } }
    private DataPacket[] currentPacket;
   
    public void CreateFish(int id, GameObject manager)
    {
        this.id = id;


        // NEED TO REFORMAT THIS TOO
        
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

        // Create object in-game
        bool speciesAccountedFor = Species.prefabDict.ContainsKey(this.speciesName);
        GameObject prefab = speciesAccountedFor ? Species.prefabDict[this.speciesName] : Species.prefabDict["Roach"];
        this.fishObject = (Instantiate (prefab, this.startPos, this.startOrient) as GameObject);
        this.fishObject.transform.parent = manager.transform;
        this.fishObject.name = string.Format("{0}", this.id);

        this.utils = this.fishObject.GetComponent<FishUtilsNew>();
        this.highlighter = this.fishObject.GetComponent<FishHighlighter>();

        if (this.length != null)
        {
            GameObject scaleDummy = this.fishObject.transform.Find("ScaleDummy").gameObject;
            BoxCollider collider = scaleDummy.GetComponent<BoxCollider>();

            string name = speciesAccountedFor ? this.speciesName.ToLower() : "roach";

            float requiredScale = (float)this.length / 1000f / this.maxExtent * UserSettings.fishScalingFactor;
            scaleDummy.transform.localScale = new Vector3(requiredScale, requiredScale, requiredScale);

            // Adjust collider size
            collider.size = this.extents * 1.2f;
        }

        this.fishObject.SetActive(false);
    }


    public void Deactivate() { this.fishObject.SetActive(false); }
    public void Activate() { this.fishObject.SetActive(true); }

    public void ActivateTag(bool activationStatus) { if (this.fishShouldExist) this.utils.ActivateTag(activationStatus); else this.utils.ActivateTag(false); }
    public void ActivateDepthLine(bool activationStatus) { if (this.fishShouldExist) this.utils.ActivateDepthLine(activationStatus); else this.utils.ActivateDepthLine(false); }
    public void ActivateTrail(bool activationStatus) { if (this.fishShouldExist) this.utils.ActivateTrail(activationStatus); else this.utils.ActivateTrail(false); }
    public void ActivateThermoBob(bool activationStatus) { if (this.fishShouldExist) this.utils.ActivateThermoBob(activationStatus); else this.utils.ActivateThermoBob(false); }

    public void UpdateFishScale(float newVal)
    {
        Vector3 currentScale = this.fishObject.transform.localScale;
        currentScale.x = currentScale.x * newVal / UserSettings.fishScalingFactor;
        currentScale.y = currentScale.y * newVal / UserSettings.fishScalingFactor;
        currentScale.z = currentScale.z * newVal / UserSettings.fishScalingFactor;

        this.fishObject.transform.localScale = currentScale;
    }

    public void LookAtFish() { Camera.main.transform.LookAt(this.fishObject.transform); }

    public void UpdateCanvasText()
    {
        string fullText =
            this.canvasTextPart1 +
            string.Format("{0:##0.00}", this.currentDepth) +
            this.canvasTextPart2;

        this.utils.setNewText(fullText);
    }

    // need to modify SQL for min dist btw points --> handle cut-offs passively
    public void UpdateFishPosition(bool timeJump, bool scaleChange)
    {
        // First runthrough condition. Query result can never be null due to FishManager gatekeeping with this.fishShouldExist
        if (this.currentPacket == null) { this.EvaluateNewBounds(); }

        // Check if we've changed boundary conditions
        else if (DateTime.Compare(this.currentPacket[1].timestamp, TimeManager.instance.currentTime) > 0 || timeJump || scaleChange) { this.EvaluateNewBounds(); } 

        // Update normally
        else { this.CalculatePositions(true); }
    }

    private async void EvaluateNewBounds() { this.CalculatePositions(await this.UpdateVectors()); }

    private async Task<bool> UpdateVectors()
    {
        try
        {
            this.currentPacket = await DatabaseConnection.GetFishData(this);

            Vector3 workingStartVector = this.currentPacket[0].pos;
            this.startPos = new Vector3(workingStartVector.x + LocalMeshData.cutoffs["minWidth"], 
                    workingStartVector.z * UserSettings.verticalScalingFactor, 
                    LocalMeshData.cutoffs["maxHeight"] - workingStartVector.y)
            ;

            Vector3 workingEndVector = this.currentPacket[1].pos;
            this.startPos = new Vector3(workingEndVector.x + LocalMeshData.cutoffs["minWidth"], 
                    workingEndVector.z * UserSettings.verticalScalingFactor, 
                    LocalMeshData.cutoffs["maxHeight"] - workingEndVector.y)
            ;

            return true;
        }
        catch { return false; }
    }

    private void CalculatePositions(bool dataBoundsExist)
    {
        if (dataBoundsExist)
        {    
            this.startOrient = this.fishObject.transform.rotation;
            if (this.endPos - this.startPos == new Vector3(0f, 0f, 0f)) this.endOrient = this.fishObject.transform.rotation;
            else this.endOrient = Quaternion.LookRotation(this.endPos - this.startPos, Vector3.up);

            float ratio = Convert.ToSingle((double)(TimeManager.instance.currentTime - this.currentPacket[0].timestamp).Ticks 
            / (double)(this.currentPacket[1].timestamp - this.currentPacket[0].timestamp).Ticks);
        
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

            // SLERP according to required method. Map the final range [0.8, 1] to the range [0, 1]
            if (levelFish) { this.fishObject.transform.rotation = Quaternion.Slerp(this.startOrient, this.endOrient, (float)(5 * (ratio - 0.8))); }
            // use an exponentiated interpolator for rotation (1 - e^-10x), and linear for position. SLERP AND LERP!!
            else { this.fishObject.transform.rotation = Quaternion.Slerp(this.startOrient, this.endOrient, (float)(1 - Math.Pow(Math.E,(-10*ratio)))); }

            // Update both position (LERP) and depth indicator line
            Vector3 LinePoint = this.fishObject.transform.position = Vector3.Lerp(this.startPos, this.endPos, ratio);
            this.utils.UpdateDepthIndicatorLine(LinePoint);

            // Update info text
            this.UpdateCanvasText();
        }
    }










    public void ResetFishColor() { this.highlighter.ResetColor(); }

    public void SetFishColor(Color color) { this.highlighter.SetColor(color); }

}