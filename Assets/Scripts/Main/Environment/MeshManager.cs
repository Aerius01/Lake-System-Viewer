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
    private int resolution;
    private float _depthOffset = 0.2f;
    public float depthOffset
    {
        get{ return _depthOffset; }
        private set { _depthOffset = value; }
    }
    public GameObject waterObject;
    public Gradient gradient;

    public void SetUpMesh()
    {   
        mesh = new Mesh();
        this.gameObject.GetComponent<MeshFilter>().mesh = mesh;
        mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

        resolution = LocalMeshData.resolution;

        CreateShape();
        UpdateMesh();

        this.transform.eulerAngles = new Vector3(0f, 90f, 0f);
        this.transform.position = new Vector3(0f, depthOffset, LocalMeshData.resolution);
        this.transform.localScale = new Vector3(1f, UserSettings.verticalScalingFactor, 1f);

        // Size & position water
        Vector3 scale = waterObject.transform.localScale;
        scale.Set((LocalMeshData.rowCount)/waterObject.GetComponent<MeshRenderer>().bounds.size.x, 1f, (LocalMeshData.columnCount)/waterObject.GetComponent<MeshRenderer>().bounds.size.z);
        waterObject.transform.position = LocalMeshData.meshCenter;

        // Set the text in the settings menu
        GameObject.Find("Canvas").transform.Find("MainPanel").transform.Find("Settings").transform.Find("SettingsMenu").
            transform.Find("Inputs").transform.Find("WaterLevel").transform.Find("WaterLevelInput").
            GetComponent<TMP_InputField>().text = string.Format("{0}", waterObject.transform.position.y
        );
    }

    public void ReZeroMesh()
    {
        Vector3 meshPosition = this.gameObject.transform.position;
        meshPosition.y = this.depthOffset * UserSettings.verticalScalingFactor;
        this.gameObject.transform.position = meshPosition;
    }

    private void CreateShape()
    {
        int totalVertices = resolution * resolution;
        int totalQuads = (resolution - 1) * (resolution - 1);

        vertices = new Vector3[totalVertices];
        float maxDepth = float.MaxValue;
        float minDepth = float.MinValue;

        for (int r = 0; r < resolution; r++)
        {
            if (r >= LocalMeshData.cutoffs["minHeight"] && r < LocalMeshData.cutoffs["maxHeight"])
            {
                for (int c = 0; c < resolution; c++)
                {
                    if (c >= LocalMeshData.cutoffs["minWidth"] && c < LocalMeshData.cutoffs["maxWidth"])
                    {
                        float depthVal = float.Parse(LocalMeshData.stringTable.Rows[r - LocalMeshData.cutoffs["minHeight"]][c - LocalMeshData.cutoffs["minWidth"]].ToString());
                        if (depthVal != 0f)
                        {
                            // Create list as though reading from bottom left to right and then up (to invert it)
                            vertices[(resolution - 1 - r) * resolution + c] = new Vector3(
                                resolution - (resolution - r),
                                depthVal - depthOffset,
                                resolution - (resolution - c)
                            );

                            // Depth entries are negative values
                            if (depthVal < maxDepth) maxDepth = depthVal;
                            if (depthVal > minDepth) minDepth = depthVal;
                        }
                        else
                        {
                            vertices[(resolution - 1 - r) * resolution + c] = new Vector3(
                                resolution - (resolution - r),
                                0f,
                                resolution - (resolution - c)
                            );
                        }
                    }
                    // Outside of meaningful resolution
                    else
                    {
                        vertices[(resolution - 1 - r) * resolution + c] = new Vector3(
                            resolution - (resolution - r),
                            0f,
                            resolution - (resolution - c)
                        );
                    }
                }
            }
            else
            {
                // Outside of meaningful resolution
                for (int c = 0; c < resolution; c++) 
                {
                    vertices[(resolution - 1 - r) * resolution + c] = new Vector3(
                        resolution - (resolution - r),
                        0f,
                        resolution - (resolution - c)
                    );
                }
            }
        }

        // Set the UVs & colors
        uv = new Vector2[totalVertices];
        colors = new Color[totalVertices];

		for (int i = 0, y = 0; y < resolution; y++) {
			for (int x = 0; x < resolution; x++, i++) {
				uv[i] = new Vector2((float)x / resolution, (float)y / resolution);
                colors[i] = gradient.Evaluate(Mathf.InverseLerp(minDepth, maxDepth, vertices[i].y));
			}
		}

        triangles = new int [totalQuads * 6];
        for (int vert = 0, tris = 0, z = 0; z < resolution - 1; z++)
        {
            for (int x = 0; x < resolution - 1; x++)
            {
                triangles[tris + 0] = vert + resolution - 1;
                triangles[tris + 1] = vert + resolution;
                triangles[tris + 2] = vert;
                triangles[tris + 3] = vert;
                triangles[tris + 4] = vert + resolution;
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
}
