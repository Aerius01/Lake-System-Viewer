using System.Collections.Generic;
using UnityEngine;
using System;

public class FishManager : MonoBehaviour
{
    public static Dictionary<int, Fish> fishDict {get; private set;}
    private static bool jumpingInTime = false;
    public static bool vertScaleChange = false, fishScaleChange = false;

    public static void ActivateAll(string util, bool activationStatus)
    {
        foreach (var key in fishDict.Keys)
        {
            if (fishDict[key].fishShouldExist) fishDict[key].ActivateUtil(util, activationStatus);
        }
    }

    public static void JumpInTime()
    {
        jumpingInTime = true;
    }

    public static void ChangeVerticalScale()
    {
        vertScaleChange = true;
    }

    public static void ChangeFishScale(float newVal)
    {
        foreach (var key in fishDict.Keys)
        {
            Fish currentFish = fishDict[key];
            currentFish.UpdateFishScale(newVal);
        }
    }

    public static void ResetFishColor(int id)
    {
        fishDict[id].ResetFishColor();
    }

    public static void SetFishColor(int id, Color color)
    {
        fishDict[id].SetFishColor(color);
    }

    public static (bool?, string, int?, int?, float, bool) GetFishStats(int fishID)
    {
        return fishDict[fishID].stats;
    }

    public static Vector3 GetFishPosition(int fishID)
    {
        return fishDict[fishID].currentPosition;
    }

    public static void LookAtFish(int fishID)
    {
        fishDict[fishID].LookAtFish();
    }

    public void SetUpFish()
    {
        fishDict = new Dictionary<int, Fish>();
        foreach (int key in LocalPositionData.positionDict.Keys)
        {
            Fish fish = new Fish(key);

            GameObject prefab = Species.prefabDict.ContainsKey(fish.speciesName) ? Species.prefabDict[fish.speciesName] : Species.prefabDict["Roach"];
            GameObject obj = (Instantiate (prefab, fish.startPos, fish.startOrient) as GameObject);
            obj.transform.parent = this.gameObject.transform;
            obj.name = string.Format("{0}", fish.id);

            fish.SetFishUtils(obj.GetComponent<FishUtils>());
            fish.SetFishHighlighter(obj.GetComponent<FishHighlighter>());
            fish.SetFishGameObject(obj);

            if (fish.length != null)
            {
                GameObject scaleDummy = obj.transform.Find("ScaleDummy").gameObject;
                BoxCollider collider = scaleDummy.GetComponent<BoxCollider>();

                string name = Species.prefabDict.ContainsKey(fish.speciesName) ? fish.speciesName.ToLower() : "roach";

                float requiredScale = (float)fish.length / 1000f / fish.maxExtent * UserSettings.fishScalingFactor;
                scaleDummy.transform.localScale = new Vector3(requiredScale, requiredScale, requiredScale);

                // Adjust collider size
                collider.size = fish.extents * 1.2f;
            }

            obj.SetActive(false);
            fishDict.Add(key, fish);
        }
    }

    public void UpdateFish()
    {
        // localScaler prevents the scale change going into effect halfway through an update
        bool localScaler = vertScaleChange ? true : false;

        foreach (var key in fishDict.Keys)
        {
            Fish currentFish = fishDict[key];
            
            // Check whether the fish should be currently spawned or not
            if (!currentFish.fishShouldExist)
            {
                if (currentFish.FishIsActive()) currentFish.Deactivate();
            }
            else
            {
                // spawn the fish if it isn't already
                if (!currentFish.FishIsActive()) currentFish.Activate();

                // Update position if already spawned
                else currentFish.UpdateFishPosition(jumpingInTime, localScaler);
            }
        }

        if (jumpingInTime) jumpingInTime = false;
        if (vertScaleChange && localScaler) vertScaleChange = false;
    }
}
