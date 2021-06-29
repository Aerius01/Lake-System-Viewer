// using System.Collections;
// using System.Collections.Generic;
// using UnityEngine;
// using System;

// public class FishSprite
// {
//     private int fishID;
//     GameObject fish; 
//     SpriteRenderer sr;
//     DateTime startTime, endTime;
//     int dataLength, currentRung;
//     private Camera mainCam;

//     public FishSprite(int FishID, Texture2D tex)
//     {
//         fish = new GameObject();
//         fishID = FishID;
//         fish.name = String.Format("Fish{0}", fishID);
//         sr = fish.AddComponent<SpriteRenderer>() as SpriteRenderer;

//         Sprite mySprite = Sprite.Create(tex, new Rect(0.0f, 0.0f, tex.width, tex.height), new Vector2(0.5f, 0.5f), 100.0f);
//         sr.sprite = mySprite;
//         sr.enabled = false;

//         dataLength = FishDataReader.parsedData[fishID].Length;
//         mainCam = Camera.main;

//         // Because each list is ordered
//         startTime = FishDataReader.parsedData[fishID][0].obsTime;
//         endTime = FishDataReader.parsedData[fishID][dataLength - 1].obsTime;
//     }

//     public void UpdateFish()
//     {
//         // TODO: have the fish move smoothly and constantly
//         // TODO: have the fish spawn at initial location
//         if (TimeManager.dateTimer >= startTime && TimeManager.dateTimer <= endTime)
//         {
//             if(sr.enabled == false)
//             {
//                 Vector3 startPos = new Vector3(FishDataReader.parsedData[fishID][0].x, FishDataReader.parsedData[fishID][0].z, FishDataReader.parsedData[fishID][0].y) - MeshDataReader.centeringVector;
//                 fish.transform.position = startPos;
//                 sr.enabled = true;
//             }

//             DateTime currentTime = TimeManager.dateTimer;
//             currentRung = 1;

//             while (currentRung < dataLength)
//             {
//                 if (FishDataReader.parsedData[fishID][currentRung].obsTime >= currentTime)
//                 {
//                     Vector3 startPos = new Vector3(fish.transform.position.x, fish.transform.position.y, fish.transform.position.z);
//                     Vector3 endPos = new Vector3(FishDataReader.parsedData[fishID][currentRung].x, FishDataReader.parsedData[fishID][currentRung].z, FishDataReader.parsedData[fishID][currentRung].y) - MeshDataReader.centeringVector;
//                     float ratio = Convert.ToSingle((double)(currentTime - FishDataReader.parsedData[fishID][currentRung - 1].obsTime).Ticks / (double)(FishDataReader.parsedData[fishID][currentRung].obsTime - FishDataReader.parsedData[fishID][currentRung - 1].obsTime).Ticks);
                    
//                     fish.transform.position = Vector3.Lerp(startPos, endPos, ratio);

//                     if (currentRung == dataLength - 1)
//                     {
//                         sr.enabled = false;
//                     }
//                     break;
//                 }

//                 currentRung++;
//             }
            
//             fish.transform.LookAt(mainCam.transform);

//         }
//         else if (TimeManager.dateTimer > endTime)
//         {
//             sr.enabled = false;
//         }
//     }
// }