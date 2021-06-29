using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshDataReader : CSVReader
{    
    [HideInInspector]
    public Vector3[] vertices;

    [HideInInspector]
    public Vector3 centeringVector;

    public void ReadData(GameObject referenceObject, bool Headers, bool IdCol) {
        hasHeaders = Headers;
        removeIdCol = IdCol;
        vertices = readCSVOutputVector3(referenceObject.GetComponent<LocalFileBrowser>().csvFile);

        centeringVector = new Vector3(totalColumns/2, 0, totalRows/2);
    }
}