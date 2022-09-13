using System.Collections.Generic;
using UnityEngine;
using System;

public class FishManager 
{
    public static Dictionary<int, Fish> fishDict {get; private set;}
    public static bool vertScaleChange = false, fishScaleChange = false;
    public static DateTime earliestOverallTime {get; private set;}
    public static DateTime latestOverallTime {get; private set;}

    public FishManager(GameObject managerObject)
    {
        fishDict = new Dictionary<int, Fish>();
        earliestOverallTime = DateTime.MaxValue;
        latestOverallTime = DateTime.MinValue;

        foreach (int key in LocalPositionData.positionDict.Keys) // temporary to not overwhelm during dev
        {
            FishPacket packet = DatabaseConnection.GetMetaData(key);
            if (packet != null)
            {
                Fish newFish = managerObject.AddComponent<Fish>() as Fish;
                newFish.CreateFish(packet, managerObject);
                fishDict.Add(key, newFish);

                // Determine bounding DateTimes on dataset
                if (DateTime.Compare(newFish.earliestTime, FishManager.earliestOverallTime) < 0) FishManager.earliestOverallTime = newFish.earliestTime;
                if (DateTime.Compare(newFish.latestTime, FishManager.latestOverallTime) > 0) FishManager.latestOverallTime = newFish.latestTime;
            }
        }

        TimeManager.instance.SetBoundingDates(FishManager.earliestOverallTime, FishManager.latestOverallTime);
    }

    public static void ActivateAllTags(bool activationStatus) { foreach (Fish fish in fishDict.Values) { fish.ActivateTag(activationStatus); } }
    public static void ActivateAllDepths(bool activationStatus) { foreach (Fish fish in fishDict.Values) { fish.ActivateDepthLine(activationStatus); } }
    public static void ActivateAllTrails(bool activationStatus) { foreach (Fish fish in fishDict.Values) { fish.ActivateTrail(activationStatus); } }
    public static void ActivateAllThermoBobs(bool activationStatus) { foreach (Fish fish in fishDict.Values) { fish.ActivateThermoBob(activationStatus); } }

    public static void ChangeVerticalScale() { vertScaleChange = true; } // ascribed to the event handled by EventSystemManager.cs
    public static void ChangeFishScale(float newVal) // ascribed to the event handled by EventSystemManager.cs
    { foreach (Fish fish in fishDict.Values) { fish.UpdateFishScale(newVal); } }

    public static void LookAtFish(int fishID) { fishDict[fishID].LookAtFish(); }

    public void UpdateFish()
    {
        // localScaler prevents the scale change going into effect halfway through an update
        bool localScaler = vertScaleChange ? true : false;

        foreach (Fish currentFish in fishDict.Values)
        {
            // Check whether the fish should be currently spawned or not
            if (!currentFish.fishShouldExist) { if (currentFish.fishCurrentlyExists) currentFish.Deactivate(); }
            else
            {
                // spawn the fish if it isn't already
                if (!currentFish.fishCurrentlyExists) currentFish.Activate();

                // Update position if already spawned
                else currentFish.UpdateFishPosition(localScaler);
            }
        }

        if (vertScaleChange && localScaler) vertScaleChange = false;
    }




    public static void ResetFishColor(int id)
    {
        fishDict[id].ResetFishColor();
    }

    public static void SetFishColor(int id, Color color)
    {
        fishDict[id].SetFishColor(color);
    }

}