using UnityEngine;
using System;

public class ComboReader : MonoBehaviour
{
    void Start(){

    MeshGenerator meshGenerator = GameObject.Find("MeshGenerator").GetComponent<MeshGenerator>();
    FishDataReader fishReader = GameObject.Find("ScriptObject").GetComponent<FishDataReader>();
    FishGenerator fishGenerator = GameObject.Find("SpriteTest").GetComponent<FishGenerator>();

    meshGenerator.StartMeshGen();

    fishReader.ReadFishData();
    fishReader.ConvertStructure();
    }

    void Update(){
        FishGenerator fishGenerator = GameObject.Find("SpriteTest").GetComponent<FishGenerator>();
        fishGenerator.UpdateFishPositions();
    }
}