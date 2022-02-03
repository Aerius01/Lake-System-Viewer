using UnityEngine;
using TMPro;

[RequireComponent(typeof(MeshFilter))]
public class MeshManager : MonoBehaviour
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
        mesh = new Mesh();
        this.gameObject.GetComponent<MeshFilter>().mesh = mesh;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        CreateShape();
        UpdateMesh();
        PlaceWater();
    }

    void CreateShape()
    {
        int vertexCols = LocalMeshData.columnCount;
        int vertexRows = LocalMeshData.rowCount; 
        int totalVertices = vertexCols * vertexRows;
        int totalQuads = (vertexRows - 1) * (vertexCols - 1);

        vertices = new Vector3[totalVertices];
        float maxDepth = float.MaxValue;
        float minDepth = float.MinValue;

        for (int r = 0; r < vertexRows; r++)
        {
            for (int c = 0; c < vertexCols; c++)
            {
                // Create list as though reading from bottom left to right and then up (invert it)
                float depthVal = float.Parse(LocalMeshData.stringTable.Rows[r][c].ToString());
                vertices[(vertexRows - 1 - r) * vertexCols + c] = new Vector3(r, depthVal, c);
                // vertices[(r * numberOfCols) + c] = new Vector3(c, float.Parse(LocalMeshData.stringTable.Rows[r][c].ToString()), r);

                // Depth entries are negative values
                if (depthVal < maxDepth) maxDepth = depthVal;
                if (depthVal > minDepth) minDepth = depthVal;
            }
        }

        // Set the UVs & colors
        uv = new Vector2[totalVertices];
        colors = new Color[totalVertices];

		for (int i = 0, y = 0; y < vertexRows; y++) {
			for (int x = 0; x < vertexCols; x++, i++) {
				uv[i] = new Vector2((float)x / vertexCols, (float)y / vertexRows);
                colors[i] = gradient.Evaluate(Mathf.InverseLerp(minDepth, maxDepth, vertices[i].y));
			}
		}

        triangles = new int [totalQuads * 6];
        for (int vert = 0, tris = 0, z = 0; z < vertexRows - 1; z++)
        {
            for (int x = 0; x < vertexCols - 1; x++)
            {
                triangles[tris + 0] = vert + vertexCols - 1;
                triangles[tris + 1] = vert + vertexCols;
                triangles[tris + 2] = vert;
                triangles[tris + 3] = vert;
                triangles[tris + 4] = vert + vertexCols;
                triangles[tris + 5] = vert + 1;

                vert++;
                tris+=6;
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
        float waterLevel = LocalMeshData.waterLevel;
        waterObject.SetActive(true);
        waterObject.transform.position = new Vector3((LocalMeshData.rowCount) / 2, waterLevel, (LocalMeshData.columnCount / 2));

        Vector3 scale = transform.localScale;
        scale.Set((LocalMeshData.rowCount)/waterObject.GetComponent<MeshRenderer>().bounds.size.x, 1, (LocalMeshData.columnCount)/waterObject.GetComponent<MeshRenderer>().bounds.size.z);
        transform.localScale = scale;

        // Set the text in the settings menu
        GameObject.Find("Canvas").transform.Find("MainPanel").transform.Find("Settings").transform.Find("SettingsMenu").
            transform.Find("Inputs").transform.Find("WaterLevel").transform.Find("WaterLevelInput").
            GetComponent<TMP_InputField>().text = string.Format("{0}", waterObject.transform.position.y);
    }
}
