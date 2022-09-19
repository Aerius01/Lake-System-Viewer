using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;

public class FishManager 
{
    public static Dictionary<int, Fish> fishDict {get; private set;}
    public static bool vertScaleChange = false, fishScaleChange = false;

    // Data set extremes
    public static DateTime earliestOverallTime {get; private set;}
    public static DateTime latestOverallTime {get; private set;}
    public static int minLength {get; private set;}
    public static int maxLength {get; private set;}
    public static float minWeight {get; private set;}
    public static float maxWeight {get; private set;}
    public static List<string> listOfSexes {get; private set;}
    public static List<string> listOfCaptureTypes {get; private set;}


    public FishManager(GameObject managerObject)
    {
        fishDict = new Dictionary<int, Fish>();

        // Extreme value initializations
        earliestOverallTime = DateTime.MaxValue;
        latestOverallTime = DateTime.MinValue;
        minLength = int.MaxValue;
        maxLength = int.MinValue;
        minWeight = int.MaxValue;
        maxWeight = int.MinValue;
        listOfSexes = new List<string>();
        listOfCaptureTypes = new List<string>();

        double totalTime = 0;

        foreach (int key in DatabaseConnection.GetFishKeys()) // temporary to not overwhelm during dev
        {
            DateTime startTime = DateTime.Now;
            FishPacket packet = DatabaseConnection.GetFishMetadata(key);
            if (packet != null)
            {
                Fish newFish = managerObject.AddComponent<Fish>() as Fish;
                newFish.CreateFish(packet, managerObject);
                fishDict.Add(key, newFish);

                // Extreme value assessments
                if (DateTime.Compare(newFish.earliestTime, FishManager.earliestOverallTime) < 0) FishManager.earliestOverallTime = newFish.earliestTime;
                if (DateTime.Compare(newFish.latestTime, FishManager.latestOverallTime) > 0) FishManager.latestOverallTime = newFish.latestTime;
                minLength = newFish.length == null ? minLength : (int)newFish.length < minLength ? (int)newFish.length : minLength;
                maxLength = newFish.length == null ? maxLength : (int)newFish.length > maxLength ? (int)newFish.length : maxLength;
                minWeight = newFish.weight == null ? minWeight : (int)newFish.weight < minWeight ? (int)newFish.weight : minWeight;
                maxWeight = newFish.weight == null ? maxWeight : (int)newFish.weight > maxWeight ? (int)newFish.weight : maxWeight;
                if (!listOfSexes.Any(s => s.Contains(string.IsNullOrEmpty(newFish.male.ToString()) ? "Undefined" : newFish.male.ToString()))) { listOfSexes.Add(string.IsNullOrEmpty(newFish.male.ToString()) ? "Undefined" : newFish.male.ToString()); }
                if (!listOfCaptureTypes.Any(s => s.Contains(string.IsNullOrEmpty(newFish.captureType.ToString()) ? "Undefined" : newFish.captureType.ToString()))) { listOfCaptureTypes.Add(string.IsNullOrEmpty(newFish.captureType.ToString()) ? "Undefined" : newFish.captureType.ToString()); }
            }
            totalTime += (DateTime.Now - startTime).TotalSeconds;
        }

        Debug.Log(string.Format("fishDict assembly: {0}", totalTime));
        TimeManager.instance.SetBoundingDates(FishManager.earliestOverallTime, FishManager.latestOverallTime);
    }

    public static void ActivateAllTags(bool activationStatus) { foreach (Fish fish in fishDict.Values) { fish.ActivateTag(activationStatus); } }
    public static void ActivateAllDepths(bool activationStatus) { foreach (Fish fish in fishDict.Values) { fish.ActivateDepthLine(activationStatus); } }
    public static void ActivateAllTrails(bool activationStatus) { foreach (Fish fish in fishDict.Values) { fish.ActivateTrail(activationStatus); } }
    public static void ActivateAllThermoBobs(bool activationStatus) { foreach (Fish fish in fishDict.Values) { fish.ActivateThermoBob(activationStatus); } }
    public static void ResetAllFishColor() { foreach (Fish fish in fishDict.Values) { fish.ResetFishColor(); } }
    public static void SetAllFishColor(string color) { foreach (Fish fish in fishDict.Values) { fish.SetFishColor(color); } }

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
}