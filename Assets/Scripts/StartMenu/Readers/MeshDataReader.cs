using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MeshDataReader : CSVReader
{    
    public Vector3[] vertices;
    public static float maxDepth;

    public void ReadData(GameObject referenceObject, bool Headers, bool IdCol) {
        hasHeaders = Headers;
        removeIdCol = IdCol;
        vertices = readCSVOutputVector3(referenceObject.GetComponent<LocalFileBrowser>().csvFile);

        maxDepth = 0f;
        foreach (Vector3 entry in vertices)
        {
            if (Mathf.Abs(entry.y) > maxDepth)
            {
                maxDepth = Mathf.Abs(entry.y);
            }
        }
    }
}