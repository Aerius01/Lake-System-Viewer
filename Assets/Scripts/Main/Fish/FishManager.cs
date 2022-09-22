using System.Collections.Generic;
using UnityEngine;
using System;
using System.Linq;
using System.Threading.Tasks;

public class FishManager
{
    public static Dictionary<int, Fish> fishDict {get; private set;}
    public static bool vertScaleChange = false;

    // Data set extremes
    public static DateTime earliestOverallTime {get; private set;}
    public static DateTime latestOverallTime {get; private set;}
    public static int minLength {get; private set;}
    public static int maxLength {get; private set;}
    public static float minWeight {get; private set;}
    public static float maxWeight {get; private set;}
    public static List<string> listOfSexes {get; private set;}
    public static List<string> listOfCaptureTypes {get; private set;}

    public static Task<bool> initialization { get; private set; }

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

        // SyncConstructor(managerObject);
        initialization = AsyncInitialize(managerObject);
    }

    // private void SyncConstructor(GameObject managerObject)
    // {
    //     foreach (int key in DatabaseConnection.GetFishKeys())
    //     {
    //         // DateTime startTime = DateTime.Now;
    //         FishPacket packet = DatabaseConnection.GetFishMetadata(key);
    //         if (packet != null)
    //         {
    //             Fish newFish = managerObject.AddComponent<Fish>() as Fish;
    //             newFish.CreateFish(packet, managerObject);
    //             fishDict.Add(key, newFish);

    //             // Extreme value assessments
    //             if (DateTime.Compare(newFish.earliestTime, FishManager.earliestOverallTime) < 0) FishManager.earliestOverallTime = newFish.earliestTime;
    //             if (DateTime.Compare(newFish.latestTime, FishManager.latestOverallTime) > 0) FishManager.latestOverallTime = newFish.latestTime;
    //             minLength = newFish.length == null ? minLength : (int)newFish.length < minLength ? (int)newFish.length : minLength;
    //             maxLength = newFish.length == null ? maxLength : (int)newFish.length > maxLength ? (int)newFish.length : maxLength;
    //             minWeight = newFish.weight == null ? minWeight : (int)newFish.weight < minWeight ? (int)newFish.weight : minWeight;
    //             maxWeight = newFish.weight == null ? maxWeight : (int)newFish.weight > maxWeight ? (int)newFish.weight : maxWeight;
    //             if (!listOfSexes.Any(s => s.Contains(string.IsNullOrEmpty(newFish.male?.ToString() ?? "") ? "Undefined" : (bool)newFish.male ? "Male" : "Female"))) { listOfSexes.Add(string.IsNullOrEmpty(newFish.male?.ToString() ?? "") ? "Undefined" : (bool)newFish.male ? "Male" : "Female"); }
    //             if (!listOfCaptureTypes.Any(s => s.Contains(string.IsNullOrEmpty(newFish.captureType?.ToString() ?? "") ? "Undefined" : newFish.captureType.ToString()))) { listOfCaptureTypes.Add(string.IsNullOrEmpty(newFish.captureType?.ToString() ?? "") ? "Undefined" : newFish.captureType.ToString()); }
    //         }
    //     }

    //     TimeManager.instance.SetBoundingDates(FishManager.earliestOverallTime, FishManager.latestOverallTime);
    // }

    public static List<List<T>> ChunkList<T>(IEnumerable<T> data, int size)
    {
        // https://www.programing.io/c-chunk-list-or-enumerable-to-smaller-list-of-lists.html
        return data
        .Select((x, i) => new { Index = i, Value = x })
        .GroupBy(x => x.Index / size)
        .Select(x => x.Select(v => v.Value).ToList())
        .ToList();
    }

    private Task<bool> AsyncInitialize(GameObject managerObject)
    {
        // Framework dafuer:
        // https://stackoverflow.com/questions/17197699/awaiting-multiple-tasks-with-different-results

        // Local function to await task completion before calling the faux-construtor
        async Task<bool> AwaitFetching(Task<Dictionary<int, FishPacket>>[] tasks) { Debug.Log("Awaiting fish creation..."); return AsyncConstructor(await Task.WhenAll(tasks), managerObject); }

        List<Task<Dictionary<int, FishPacket>>> tasks = new List<Task<Dictionary<int, FishPacket>>>();
        List<int> listOfKeys = DatabaseConnection.GetFishKeys();
        listOfKeys.Sort();
        List<List<int>> partialKeyLists = ChunkList(listOfKeys, (int)Mathf.Ceil(listOfKeys.Count / 30));
        
        // Break the list into chunks for batch SQL queries, and then parallelize processing
        foreach (List<int> partialList in partialKeyLists)
        {
            DatabaseConnection DB = new DatabaseConnection();
            Task<Dictionary<int, FishPacket>> task = Task.Run(() => DB.GetFishMetadata(partialList));
            tasks.Add(task);
        }

        // Synch fall-back condition (all tasks finished immediately)
        bool syncRunThrough = true;
        foreach (Task<Dictionary<int, FishPacket>> item in tasks) { if (item.Status != TaskStatus.RanToCompletion) { syncRunThrough = false; break; } }
        if (syncRunThrough) { Debug.Log("Sync runthrough condition"); return Task.FromResult( AsyncConstructor(tasks.Select(i => i.Result).ToArray(), managerObject) ); }

        return AwaitFetching(tasks.ToArray());
    }

    private bool AsyncConstructor(Dictionary<int, FishPacket>[] packetDictArray, GameObject managerObject)
    {
        // Async init
        // https://stackoverflow.com/questions/15907356/how-to-initialize-an-object-using-async-await-pattern

        // Assemble the parallelized partial dictionaries into one
        Dictionary<int, FishPacket> packetDict = new Dictionary<int, FishPacket>();
        foreach (Dictionary<int, FishPacket> partialDict in packetDictArray) { partialDict.ToList().ForEach(x => packetDict.Add(x.Key, x.Value)); }

        // Assemble the fish
        foreach (int key in packetDict.Keys)
        {
            Fish newFish = managerObject.AddComponent<Fish>() as Fish;
            fishDict.Add(key, newFish);
            newFish.CreateFish(packetDict[key], managerObject);

            // Extreme value assessments
            if (DateTime.Compare(newFish.earliestTime, FishManager.earliestOverallTime) < 0) FishManager.earliestOverallTime = newFish.earliestTime;
            if (DateTime.Compare(newFish.latestTime, FishManager.latestOverallTime) > 0) FishManager.latestOverallTime = newFish.latestTime;
            minLength = newFish.length == null ? minLength : (int)newFish.length < minLength ? (int)newFish.length : minLength;
            maxLength = newFish.length == null ? maxLength : (int)newFish.length > maxLength ? (int)newFish.length : maxLength;
            minWeight = newFish.weight == null ? minWeight : (int)newFish.weight < minWeight ? (int)newFish.weight : minWeight;
            maxWeight = newFish.weight == null ? maxWeight : (int)newFish.weight > maxWeight ? (int)newFish.weight : maxWeight;
            if (!listOfSexes.Any(s => s.Contains(string.IsNullOrEmpty(newFish.male?.ToString() ?? "") ? "Undefined" : (bool)newFish.male ? "Male" : "Female"))) { listOfSexes.Add(string.IsNullOrEmpty(newFish.male?.ToString() ?? "") ? "Undefined" : (bool)newFish.male ? "Male" : "Female"); }
            if (!listOfCaptureTypes.Any(s => s.Contains(string.IsNullOrEmpty(newFish.captureType?.ToString() ?? "") ? "Undefined" : newFish.captureType.ToString()))) { listOfCaptureTypes.Add(string.IsNullOrEmpty(newFish.captureType?.ToString() ?? "") ? "Undefined" : newFish.captureType.ToString()); }
        }

        TimeManager.instance.SetBoundingDates(FishManager.earliestOverallTime, FishManager.latestOverallTime);
        return true;
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