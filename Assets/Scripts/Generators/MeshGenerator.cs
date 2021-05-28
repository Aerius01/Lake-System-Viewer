using System.Collections;
using System.Collections.Generic;
using UnityEngine;

//[ExecuteInEditMode]
[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    Mesh mesh;
    int[] triangles;
    Color[] colors;
    public Gradient gradient;
    Vector3[] vertices;

    // Start is called before the first frame update
    public void StartMeshGen()
    {   
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        CreateShape();
        UpdateMesh();
    }

    void CreateShape()
    {
        // Initialize arrays
        vertices = MeshDataReader.vertices;

        MeshDataReader meshReader = GameObject.Find("ScriptObject").GetComponent<MeshDataReader>();

        int numberOfCols = meshReader.totalColumns - meshReader.intRemoveIdCol;
        int numberOfRows = meshReader.totalRows - meshReader.intHasHeaders;

        colors = new Color[vertices.Length];
        triangles = new int [numberOfCols * numberOfRows * 6];

        float maxTerrainHeight = 0;
        float minTerrainHeight = 0;
        for (int vert = 0, tris = 0, z = 0; z < numberOfRows; z++)
        {
            for (int x = 0; x < numberOfCols; x++)
            {
                if (z < numberOfRows - 1 && x < numberOfCols - 1)
                {
                    // 6 vertices per quad
                    triangles[tris + 0] = vert;
                    triangles[tris + 1] = vert + numberOfCols;
                    triangles[tris + 2] = vert + 1;
                    triangles[tris + 3] = vert + 1;
                    triangles[tris + 4] = vert + numberOfCols;
                    triangles[tris + 5] = vert + numberOfCols + 1;

                    vert++;
                    tris+=6;
                }
 
                if (maxTerrainHeight > vertices[(z * numberOfCols) + x].y)
                {
                    maxTerrainHeight = vertices[(z * numberOfCols) + x].y;
                }

                if (minTerrainHeight < vertices[(z * numberOfCols) + x].y)
                {
                    minTerrainHeight = vertices[(z * numberOfCols) + x].y;
                }

                colors[(z * numberOfCols) + x] = gradient.Evaluate(Mathf.InverseLerp(minTerrainHeight, maxTerrainHeight, vertices[(z * numberOfCols) + x].y));
            }
            
            if (z < numberOfRows - 1){
                vert++;
            }
        }
    }

    void UpdateMesh()
    {
        mesh.Clear();
        mesh.vertices = vertices;
        mesh.colors = colors;
        mesh.triangles = triangles;
        mesh.RecalculateNormals();
    }

     // http://codesaying.com/unity-parse-excel-in-unity3d/
    
    // Vector3[] getVector(string csvText)
    // {
    //     // split the data on split line character
    //     string[] lines = csvText.Split("\n"[0]);

    //     // find the max number of columns
    //     int totalColumns = 0;
    //     for (int i = 0; i < lines.Length; i++)
    //     {
    //         string[] row = lines[i].Split(',');
    //         totalColumns = Mathf.Max(totalColumns, row.Length);
    //     }

    //     // creates new 2D string grid to output to
    //     vertices = new Vector3[(totalColumns-1) * (lines.Length-2)];
    //     for (int iterator = 0, z = 1; z < lines.Length; z++)
    //     {
    //         string[] row = lines[z].Split(',');
    //         for (int x = 1; x < row.Length; x++)
    //         {
    //             vertices[iterator] = new Vector3(x, float.Parse(row[x]), z);

    //             iterator++;
    //         }
    //     }

    //     numberOfRows = lines.Length-2;
    //     numberOfCols = totalColumns-1;

    //     return vertices;
    // }

}