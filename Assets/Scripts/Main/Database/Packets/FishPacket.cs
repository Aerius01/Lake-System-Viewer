using System;

// Support class
public class FishPacket
{
    public int fishID {get; private set;}
    public string captureType {get; private set;}
    public int? length {get; private set;}
    public int? speciesCode {get; private set;}
    public int? weight {get; private set;}
    public string speciesName{ get; private set;}
    public bool? male {get; private set;}
    public DateTime earliestTime {get; private set;}
    public DateTime latestTime {get; private set;}

    public FishPacket(int fishID, string captureType, int? length, int? speciesCode, int? weight, string speciesName, bool? male, DateTime earliestTime, DateTime latestTime)
    {
        this.fishID = fishID;
        this.captureType = captureType;
        this.length = length;
        this.speciesCode = speciesCode;
        this.weight = weight;
        this.speciesName = speciesName;
        this.male = male;
        this.earliestTime = earliestTime;
        this.latestTime = latestTime;
    }
}