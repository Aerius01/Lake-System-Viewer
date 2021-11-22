using UnityEngine;
using TMPro;

[RequireComponent(typeof(MeshFilter))]
public class MeshGeneratorNew : MonoBehaviour
{
    private Mesh mesh;
    private int[] triangles;
    private Color[] colors;
    private Vector3[] vertices;
    private Vector2[] uv;
    public GameObject waterObject;
    public Gradient gradient;

    // Start is called before the first frame update
    public void SetUpMesh()
    {   
        Debug.Log("starting mesh");
        mesh = new Mesh();
        this.gameObject.GetComponent<MeshFilter>().mesh = mesh;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        CreateShape();
        UpdateMesh();
        PlaceWater();
        Debug.Log("mesh done");
    }

    void CreateShape()
    {
        int numberOfCols = MeshData.instance.columnCount;
        int numberOfRows = MeshData.instance.rowCount; 
        int totalEntries = MeshData.instance.columnCount * MeshData.instance.columnCount;

        vertices = new Vector3[totalEntries];

        for (int r = 0; r < numberOfRows; r++)
        {
            for (int c = 0; c < numberOfCols; c++)
            {
                // Create list as though reading from bottom left to right and then up (invert it)
                vertices[(numberOfRows - r) * numberOfCols + c] = new Vector3(r, float.Parse(MeshData.instance.stringTable.Rows[r][c].ToString()), c);
                // vertices[(r * numberOfCols) + c] = new Vector3(c, float.Parse(MeshData.instance.stringTable.Rows[r][c].ToString()), r);
            }
        }

        colors = new Color[totalEntries];
        triangles = new int [numberOfCols * numberOfRows * 6];

        // Set the UVs
        uv = new Vector2[totalEntries];
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
        float waterLevel = MeshData.instance.waterLevel;
        waterObject.SetActive(true);
        waterObject.transform.position = new Vector3((MeshData.instance.rowCount) / 2, waterLevel, (MeshData.instance.columnCount / 2));

        Vector3 scale = transform.localScale;
        scale.Set((MeshData.instance.rowCount)/waterObject.GetComponent<MeshRenderer>().bounds.size.x, 1, (MeshData.instance.columnCount)/waterObject.GetComponent<MeshRenderer>().bounds.size.z);
        transform.localScale = scale;

        // Set the text in the settings menu
        GameObject.Find("Canvas").transform.Find("MainPanel").transform.Find("Settings").transform.Find("SettingsMenu").
            transform.Find("Inputs").transform.Find("WaterLevel").transform.Find("WaterLevelInput").
            GetComponent<TMP_InputField>().text = string.Format("{0}", waterObject.transform.position.y);
    }
}
