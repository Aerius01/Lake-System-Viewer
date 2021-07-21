using UnityEngine;
using TMPro;
using System;

[RequireComponent(typeof(MeshFilter))]
public class MeshGenerator : MonoBehaviour
{
    Mesh mesh;
    int[] triangles;
    Color[] colors;
    public Gradient gradient;
    Vector3[] vertices;
    Vector2[] uv;
    MeshDataReader meshReader;
    public GameObject heightMapUploadObject, waterObject;
    public float waterLevel;

    // Start is called before the first frame update
    void Start()
    {   
        mesh = new Mesh();
        this.gameObject.GetComponent<MeshFilter>().mesh = mesh;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        waterLevel = heightMapUploadObject.GetComponent<LocalFileBrowser>().waterLevel;

        meshReader = new MeshDataReader();
        meshReader.ReadData(heightMapUploadObject, heightMapUploadObject.GetComponent<LocalFileBrowser>().headers, heightMapUploadObject.GetComponent<LocalFileBrowser>().colIDs);

        CreateShape();
        UpdateMesh();
        PlaceWater();
    }

    void CreateShape()
    {
        int numberOfCols = meshReader.totalColumns - meshReader.intRemoveIdCol;
        int numberOfRows = meshReader.totalRows - meshReader.intHasHeaders; 

        vertices = new Vector3[meshReader.vertices.Length];

        for (int i = 0; i < meshReader.vertices.Length; i++)
        {
            vertices[i] = meshReader.vertices[i];
        }

        colors = new Color[vertices.Length];
        triangles = new int [numberOfCols * numberOfRows * 6];

        // Set the UVs
        uv = new Vector2[vertices.Length];
		for (int i = 0, y = 0; y < numberOfRows; y++) {
			for (int x = 0; x < numberOfCols; x++, i++) {
				uv[i] = new Vector2((float)x / numberOfCols, (float)y / numberOfRows);
			}
		}

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
        mesh.uv = uv;
        mesh.RecalculateNormals();
    }

    void PlaceWater()
    {
        waterObject.SetActive(true);
        waterObject.transform.position = new Vector3((meshReader.totalColumns - meshReader.intRemoveIdCol - 1) / 2, waterLevel, (meshReader.totalRows - meshReader.intHasHeaders - 1) / 2);

        Vector3 scale = transform.localScale;
        scale.Set((meshReader.totalColumns - meshReader.intRemoveIdCol - 1)/waterObject.GetComponent<MeshRenderer>().bounds.size.x, 1, (meshReader.totalRows - meshReader.intHasHeaders - 1)/waterObject.GetComponent<MeshRenderer>().bounds.size.z);
        transform.localScale = scale;

        // Set the text in the settings menu
        GameObject.Find("Canvas").transform.Find("MainPanel").transform.Find("Settings").transform.Find("SettingsMenu").
            transform.Find("Inputs").transform.Find("WaterLevel").transform.Find("WaterLevelInput").
            GetComponent<TMP_InputField>().text = string.Format("{0}", waterObject.transform.position.y);
    }
}
