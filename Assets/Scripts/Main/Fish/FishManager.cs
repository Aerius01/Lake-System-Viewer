using System.Collections.Generic;
using UnityEngine;

public class FishManager : MonoBehaviour
{
    private static Dictionary<int, Fish> fishDict {get; set;}
    public GameObject fishPrefab;

    public static void ActivateAll(string util, bool activationStatus)
    {
        foreach (var key in fishDict.Keys)
        {
            fishDict[key].ActivateUtil(util, activationStatus);
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

    public static (bool?, string, int?, int?, float) GetFishStats(int fishID)
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
            
            GameObject obj = (Instantiate (fishPrefab, fish.startPos, fish.startOrient) as GameObject);
            obj.transform.parent = this.gameObject.transform;
            obj.name = string.Format("{0}", fish.id);

            fish.SetFishUtils(obj.GetComponent<FishUtils>());
            fish.SetFishHighlighter(obj.GetComponent<FishHighlighter>());
            fish.SetFishGameObject(obj);

            obj.SetActive(false);
            fishDict.Add(key, fish);
        }
    }

    public void UpdateFish()
    {
        bool jumpingInTime = false;
        if (PlaybackController.sliderHasChanged)
        {
            jumpingInTime = true;
        }

        foreach (var key in fishDict.Keys)
        {
            Fish currentFish = fishDict[key];
            
            // if before first time step or after last and fish exists, despawn it
            if (currentFish.fishShouldExist)
            {
                if (currentFish.FishIsActive()) currentFish.Deactivate();
            }
            else
            {
                // spawn the fish if it isn't already
                if (!currentFish.FishIsActive()) currentFish.Activate();

                // Update position if already spawned
                else currentFish.UpdateFishPosition(jumpingInTime);
            }
        }

        if (jumpingInTime)
        {
            PlaybackController.sliderHasChanged = false;
        }
    }
}
