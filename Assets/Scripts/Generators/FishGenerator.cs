using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FishGenerator : MonoBehaviour
{
    public int fishID;
    public Texture2D tex;
    

    private Sprite mySprite;
    private SpriteRenderer sr;
    private int currentRung;
    // private bool timeSeriesFinished = false;

    // Start is called before the first frame update
    public void Start()
    {
        sr = gameObject.AddComponent<SpriteRenderer>() as SpriteRenderer;
        mySprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        sr.sprite = mySprite;

    }

    // Update is called once per frame
    public void UpdateFishPositions()
    {
        DateTime currentTime = TimeManager.dateTimer;

        currentRung = 1;
        int arrayLength = FishDataReader.parsedData[fishID].GetLength(0);
        
        // Find the closest DateTime value & interpolate the position
        while (currentRung < arrayLength)
        {
            if (FishDataReader.parsedData[fishID][currentRung].obsTime >= currentTime)
            {

                Vector3 startPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                Vector3 endPos = new Vector3(FishDataReader.parsedData[fishID][currentRung].x, FishDataReader.parsedData[fishID][currentRung].z, FishDataReader.parsedData[fishID][currentRung].y);
                float ratio = Convert.ToSingle((double)(currentTime - FishDataReader.parsedData[fishID][currentRung - 1].obsTime).Ticks / (double)(FishDataReader.parsedData[fishID][currentRung].obsTime - FishDataReader.parsedData[fishID][currentRung - 1].obsTime).Ticks);
                
                transform.position = Vector3.Lerp(startPos, endPos, ratio);

                if (currentRung == arrayLength - 1)
                {
                   // despawn sprite?
                }
                break;
            }

            currentRung++;
        }
    }
}
