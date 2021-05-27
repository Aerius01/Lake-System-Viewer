using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class FishGenerator : MonoBehaviour
{
    public string fishID;
    public Texture2D tex;
    

    private Sprite mySprite;
    private SpriteRenderer sr;
    private CSVReader csvReader = new CSVReader();
    private int currentRung;
    // private bool timeSeriesFinished = false;


    void Awake()
    {
        sr = gameObject.AddComponent<SpriteRenderer>() as SpriteRenderer;
        // transform.position = new Vector3(1.5f, 1.5f, 0.0f);
    }


    // Start is called before the first frame update
    void Start()
    {
        mySprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
        sr.sprite = mySprite;

    }

    // Update is called once per frame
    void Update()
    {
        DateTime currentTime = TimeManager.dateTimer;

        currentRung = 1;
        int arrayLength = FishDataReader.parsedData[fishID].GetLength(1);
        
        // Find the closest DateTime value & interpolate the position
        while (currentRung < arrayLength)
        {
            if (DateTime.Parse(FishDataReader.parsedData[fishID][3, currentRung]) >= currentTime)
            {

                Vector3 startPos = new Vector3(transform.position.x, transform.position.y, transform.position.z);
                Vector3 endPos = new Vector3(csvReader.convertStringLongValue(FishDataReader.parsedData[fishID][0,currentRung]), - float.Parse(FishDataReader.parsedData[fishID][2,currentRung]), csvReader.convertStringLatValue(FishDataReader.parsedData[fishID][1,currentRung]));
                float ratio = Convert.ToSingle((double)(currentTime - DateTime.Parse(FishDataReader.parsedData[fishID][3,currentRung - 1])).Ticks / (double)(DateTime.Parse(FishDataReader.parsedData[fishID][3,currentRung]) - DateTime.Parse(FishDataReader.parsedData[fishID][3,currentRung - 1])).Ticks);
                
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
