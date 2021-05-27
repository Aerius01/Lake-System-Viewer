using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshDataReader : CSVReader
{    
    public static Vector3[] vertices {get; private set;}

    // Start is called before the first frame update
    private void Awake() {
        vertices = readCSVOutputVector3(csvFile.text);
    }
}