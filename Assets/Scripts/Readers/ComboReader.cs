using UnityEngine;
using System;

public class ComboReader : MonoBehaviour
{
    public float speedUpCoefficient = 10f;

    void Start(){

    MeshGenerator meshGenerator = GameObject.Find("MeshGenerator").GetComponent<MeshGenerator>();
    FishDataReader fishReader = GameObject.Find("ScriptObject").GetComponent<FishDataReader>();
    FishGenerator fishGenerator = GameObject.Find("SpriteTest").GetComponent<FishGenerator>();

    meshGenerator.StartMeshGen();

    fishReader.ReadFishData();
    fishReader.ConvertStructure();
    fishGenerator.Initialize();

    TimeManager.StartTime(FishDataReader.earliestTimeStamp);

    }

    void Update(){
        TimeManager.speedUpCoefficient = speedUpCoefficient;
        TimeManager.UpdateTime();

        FishGenerator fishGenerator = GameObject.Find("SpriteTest").GetComponent<FishGenerator>();
        fishGenerator.UpdateFishPositions();
    }
}