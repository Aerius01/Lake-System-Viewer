using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshDataReader : CSVReader
{    
    public static Vector3[] vertices {get; private set;}

    public static Vector3 centeringVector;

    // Start is called before the first frame update
    private void Awake() {
        vertices = readCSVOutputVector3(csvFile.text);

        centeringVector = new Vector3(totalColumns/2, 0, totalRows/2);
    }
}