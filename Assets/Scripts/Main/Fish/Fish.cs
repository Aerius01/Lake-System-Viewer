using UnityEngine;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Threading;

public class Fish : MonoBehaviour
{   
    // World position data
    public Vector3 startPos, endPos;
    private readonly object locker = new object();
    public Quaternion startOrient, endOrient;
    private float extremeFishAngle = 15f;

    // The actual object in-game
    public GameObject fishObject {get; private set;} = null;

    public bool fishCurrentlyExists { get { return this.fishObject.activeSelf; } }
    public bool spawnOverride = false;
    
    public bool canvasActive { get { return utils.canvas.activeSelf; } } 
    public bool depthLineActive { get { return utils.depthLine.activeSelf; } } 
    public bool trailActive { get { return utils.trail.activeSelf; } } 
    public bool thermoIndActive { get { return utils.thermoInd.activeSelf; } }
    public float baseExtent { get { return utils.baseExtent; } }


    // Utility class representing depth lines, tags, colors, etc
    private FishUtils utils;
    public Color color { get { return this.utils.fishColor; } }

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
    private PositionCache positionCache;

    // Changing information
    public Vector3 currentPosition {get {return this.fishObject.transform.position;} }
    public float currentDepth { get { return this.currentPosition.y / UserSettings.verticalScalingFactor - UserSettings.waterLevel; } }
    private DataPacket[] currentPacket = null;
    // private Thread fetchingThread;
   
    public void CreateFish(FishPacket packet, GameObject manager)
    {
        // Get all fish metadata
        this.id = packet.fishID;
        this.earliestTime = packet.earliestTime;
        this.latestTime = packet.latestTime;
        this.length = packet.length;
        this.weight = packet.weight;
        this.speciesCode = packet.speciesCode;
        this.speciesName = packet.speciesName;
        this.male = packet.male;
        this.captureType = packet.captureType;

        // Create object in-game
        GameObject prefab = Species.prefabDict["Roach"];
        if (this.speciesName != null) { if (Species.prefabDict.ContainsKey(this.speciesName)) prefab = Species.prefabDict[this.speciesName]; }
        else this.speciesName = "Unspecified";

        this.fishObject = (Instantiate (prefab, this.startPos, this.startOrient) as GameObject);
        this.fishObject.transform.parent = manager.transform;
        this.fishObject.name = string.Format("{0}", this.id);

        // Set layer, including that of all children, for renderer customizations (handling occlusion)
        // https://www.youtube.com/watch?v=szsWx9IQVDI
        string layerName = (this.speciesName == "Mirror carp" || this.speciesName == "Scaled carp" || this.speciesName == "Catfish") ? (this.speciesName == "Catfish" ? "Catfish" : "Carp") : "Fish";
        int fishLayer = LayerMask.NameToLayer(layerName);

        this.utils = this.fishObject.GetComponent<FishUtils>();
        this.utils.Setup(fishLayer, this);
        this.UpdateFishScale();
        this.positionCache = new PositionCache(this.id);

        this.fishObject.SetActive(false);
    }

    public void Clear()
    {
        Destroy(this.fishObject);
        Destroy(this);
    }

    public bool FishShouldExist(DateTime timestamp)
    {
        if ((DateTime.Compare(timestamp, this.earliestTime) < 0 
            || DateTime.Compare(timestamp, this.latestTime) > 0) || this.spawnOverride || !FilterManager.PassesAllFilters(this))
            return false;
        else return true;
    }

    public bool Timebounded(DateTime timestamp)
    {
        return this.currentPacket == null ? false :
            (
                this.currentPacket[1] == null ? false :
                (DateTime.Compare(timestamp, this.currentPacket[0].timestamp) > 0 && DateTime.Compare(timestamp, this.currentPacket[1].timestamp) < 0)
            );
    }

    public void Deactivate() { this.fishObject.SetActive(false); }
    public void Activate() { this.fishObject.SetActive(true); }

    public void ActivateTag(bool activationStatus, DateTime timestamp) { if (this.FishShouldExist(timestamp)) this.utils.ActivateTag(activationStatus); else this.utils.ActivateTag(false); }
    public void ActivateDepthLine(bool activationStatus, DateTime timestamp) { if (this.FishShouldExist(timestamp)) this.utils.ActivateDepthLine(activationStatus); else this.utils.ActivateDepthLine(false); }
    public void ActivateTrail(bool activationStatus, DateTime timestamp) { if (this.FishShouldExist(timestamp)) this.utils.ActivateTrail(activationStatus); else this.utils.ActivateTrail(false); }
    public void ActivateThermoBob(bool activationStatus, DateTime timestamp) { if (this.FishShouldExist(timestamp)) this.utils.ActivateThermoBob(activationStatus); else this.utils.ActivateThermoBob(false); }
    public void UpdateFishScale() { this.utils.UpdateFishScale(UserSettings.fishScalingFactor, (float)(this.length == null ? 500 : this.length)); }

    public void LookAtFish() { Camera.main.transform.LookAt(this.fishObject.transform); }

    public void UpdateCanvasText()
    {
        string fullText = string.Format("ID: {0}\n{1}{2}\nDepth: {3:##0.00}", this.id, this.speciesName, this.male == null ? "" : this.male == true ? " (Male)" : " (Female)", this.currentDepth);
        this.utils.setNewText(fullText);
    }

    public Task UpdatePositionCache(List<DataPacket> newPackets, bool forwardOnly, CancellationToken token)
    {
        this.positionCache.AllocateNewPackets(newPackets, forwardOnly, token);
        // Debug.Log(string.Format("Allocated packets for ID: {0}", this.id));
        return Task.CompletedTask; 
    }

    public void RequeryCache(DateTime updateTime) { this.positionCache.FullRequery(updateTime); }

    public void UpdateFishPosition(bool scaleChange, DateTime updateTime)
    {
        // Update normally if we're bounded (simple LERP)
        // Timebounded() handles the currentPacket == null case
        if (this.Timebounded(updateTime)) this.CalculatePositions(updateTime);

        // Fetch a new set of packets and wait for update to circle back
        else lock(this.locker) this.currentPacket = this.positionCache.GetCachedBounds(updateTime);
    }

    private void CalculatePositions(DateTime updateTime)
    {
        bool levelFish = false;
        this.startOrient = this.fishObject.transform.rotation;
        float ratio = Convert.ToSingle((double)(updateTime - this.currentPacket[0].timestamp).Ticks 
            / (double)(this.currentPacket[1].timestamp - this.currentPacket[0].timestamp).Ticks);

        Vector3 workingStartVector = new Vector3();
        Vector3 workingEndVector = new Vector3();

        lock(this.locker)
        {
            workingStartVector = this.currentPacket[0].pos;
            workingEndVector = this.currentPacket[1].pos;
        }
        
        this.startPos = new Vector3(workingStartVector.x + LocalMeshData.cutoffs["minWidth"], 
                (workingStartVector.z + UserSettings.waterLevel) * UserSettings.verticalScalingFactor, 
                LocalMeshData.cutoffs["maxHeight"] - workingStartVector.y)
        ;

        this.endPos = new Vector3(workingEndVector.x + LocalMeshData.cutoffs["minWidth"], 
                (workingEndVector.z + UserSettings.waterLevel) * UserSettings.verticalScalingFactor, 
                LocalMeshData.cutoffs["maxHeight"] - workingEndVector.y)
        ;

        if (this.endPos - this.startPos == new Vector3(0f, 0f, 0f)) this.endOrient = this.fishObject.transform.rotation;
        else this.endOrient = Quaternion.LookRotation(this.endPos - this.startPos, Vector3.up);

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

        // Update both position (LERP) and depth indicator line
        Vector3 LinePoint = this.fishObject.transform.position = Vector3.Lerp(this.startPos, this.endPos, ratio);
        this.utils.UpdateDepthIndicatorLine(LinePoint);

        // SLERP according to required method. Map the final range [0.8, 1] to the range [0, 1]
        if (levelFish) { this.fishObject.transform.rotation = Quaternion.Slerp(this.startOrient, this.endOrient, (float)(5 * (ratio - 0.8))); }
        // use an exponentiated interpolator for rotation (1 - e^-10x), and linear for position. SLERP AND LERP!!
        else { this.fishObject.transform.rotation = Quaternion.Slerp(this.startOrient, this.endOrient, (float)(1 - Math.Pow(Math.E,(-10*ratio)))); }

        // Update info text
        this.UpdateCanvasText();
    }

    public void ResetFishColor() { this.utils.ResetColor(); }
    public void SetFishColor(string color) { this.utils.SetFishColor(color); }
}