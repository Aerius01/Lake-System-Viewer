using System.Collections.Generic;
using UnityEngine;

public class FishManager 
{
    public static Dictionary<int, Fish> fishDict {get; private set;}
    private static bool jumpingInTime = false;
    public static bool vertScaleChange = false, fishScaleChange = false;

    public FishManager(GameObject managerObject)
    {
        fishDict = new Dictionary<int, Fish>();
        foreach (int key in LocalPositionData.positionDict.Keys)
        {
            FishPacket packet = DatabaseConnection.GetMetaData(key);
            if (packet != null)
            {
                Fish newFish = managerObject.AddComponent<Fish>() as Fish;
                newFish.CreateFish(packet, managerObject);
                fishDict.Add(key, newFish);
            }
        }
    }

    public static void ActivateAllTags(bool activationStatus) { foreach (Fish fish in fishDict.Values) { fish.ActivateTag(activationStatus); } }
    public static void ActivateAllDepths(bool activationStatus) { foreach (Fish fish in fishDict.Values) { fish.ActivateDepthLine(activationStatus); } }
    public static void ActivateAllTrails(bool activationStatus) { foreach (Fish fish in fishDict.Values) { fish.ActivateTrail(activationStatus); } }

    public static void JumpInTime() { jumpingInTime = true; } // ascribed to the event handled by PlaybackController.cs
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
                else currentFish.UpdateFishPosition(jumpingInTime, localScaler);
            }
        }

        if (jumpingInTime) jumpingInTime = false;
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