using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FishGenerator : MonoBehaviour
{
    public Texture2D tex;
    public int[] fishIDs;
    List<FishSprite> fishSprites;


    // private bool timeSeriesFinished = false;

    // Start is called before the first frame update
    public void Initialize()
    {
        fishSprites = new List<FishSprite>();
        foreach (int fishID in fishIDs)
        {
            FishSprite newSprite = new FishSprite(fishID, tex);
            fishSprites.Add(newSprite);
        }

    }

    // Update is called once per frame
    public void UpdateFishPositions()
    {
        foreach (FishSprite sprite in fishSprites)
        {
            sprite.UpdateFish();
        }
    }
}
