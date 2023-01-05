using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Data;
using System.Threading.Tasks;

[RequireComponent(typeof(MeshFilter))]
public class MeshManager : MonoBehaviour
{
    private Mesh mesh;
    private int[] triangles;
    private Color[] colors;
    private Vector3[] vertices;
    private Vector2[] uv;
    private int resolution;
    private float depthOffset = 0.2f;
    public GameObject waterObject;

    private (float, float)[] contourBoundaries;
    private int numberOfContourPartitions = 10;

    [SerializeField] private Gradient gradient;
    [SerializeField] private Texture2D NDVI;
    [SerializeField] private TMP_InputField waterText;
    [SerializeField] private Slider weightSlider;

    private static MeshManager _instance;
    [HideInInspector]
    public static MeshManager instance {get { return _instance; } set {_instance = value; }}

    private void Awake()
    {
        // Destroy duplicates instances
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }

        weightSlider.normalizedValue = 0.07f;
    }

    private void CalculateContourBounds(float tolerance=0.07f)
    {
        // Contour operations; There are 10 partitions and 11 thresholds.
        this.contourBoundaries = new (float, float)[MeshManager.instance.numberOfContourPartitions]; // (min, max)
        float increment = LocalMeshData.maxDiff / (float)MeshManager.instance.numberOfContourPartitions;

        // The 11th threshold is the flat plane at depth == 0, do not fill it in
        for (int i = 0; i < 10; i++) this.contourBoundaries[i] = new (LocalMeshData.minDepth + increment * i - tolerance, LocalMeshData.minDepth + increment * i + tolerance);
    }

    public async Task<bool> SetUpMesh()
    {   
        DataTable meshTable = await DatabaseConnection.GetMeshMap();

        if(meshTable != null)
        {
            if(meshTable.Rows.Count > 0)
            {
                LocalMeshData meshData = new LocalMeshData(meshTable, NDVI);

                mesh = new Mesh();
                this.gameObject.GetComponent<MeshFilter>().mesh = mesh;
                mesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;

                resolution = LocalMeshData.resolution;

                this.CalculateContourBounds();
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
                waterText.text = string.Format("{0}", waterObject.transform.position.y);

                // Create gradient for height map toggling
                GradientPicker.Create(this.gradient, GradientFinished);

                TerrainManager.instance.SetUpTerrain();
                return true;
            }
            return false;
        }
        return false;
    }

    public void ReZeroMesh()
    {
        Vector3 meshPosition = this.gameObject.transform.position;
        meshPosition.y = this.depthOffset * UserSettings.verticalScalingFactor;
        this.gameObject.transform.position = meshPosition;
    }

    public static float PointDepth(int x, int y)
    {
        // the vertices and the intensity color map are both in mesh coordinates
        // both are also already oriented to the bottom left corner
        return MeshManager.instance.vertices[y * LocalMeshData.resolution + x].y;
    }

    private void CreateShape()
    {
        int totalVertices = resolution * resolution;
        int totalQuads = (resolution - 1) * (resolution - 1);

        vertices = new Vector3[totalVertices];

        for (int r = 0; r < resolution; r++)
        {
            if (r >= LocalMeshData.cutoffs["minHeight"] && r < LocalMeshData.cutoffs["maxHeight"])
            {
                for (int c = 0; c < resolution; c++)
                {
                    if (c >= LocalMeshData.cutoffs["minWidth"] && c < LocalMeshData.cutoffs["maxWidth"])
                    {
                        float depthVal = float.Parse(LocalMeshData.meshMap.Rows[r - LocalMeshData.cutoffs["minHeight"]][c - LocalMeshData.cutoffs["minWidth"]].ToString());
                        if (depthVal != 0f)
                        {
                            // Create list as though reading from bottom left to right and then up (to invert it)
                            vertices[(resolution - 1 - r) * resolution + c] = new Vector3(
                                resolution - (resolution - r),
                                depthVal - depthOffset,
                                resolution - (resolution - c)
                            );
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

        // // Perlin noise for investigation of smoothness
        // for (int i = 0, y = 0; y < this.resolution; y++)
        // {
        //     for (int x = 0; x < this.resolution; x++, i++)
        //     {
        //         float xCoord = ((float)x / (float)this.resolution);
        //         float yCoord = ((float)y / (float)this.resolution);
        //         this.vertices[i].y = - Mathf.PerlinNoise(xCoord * 20f, yCoord * 20f) * LocalMeshData.maxDiff;
        //     }
        // }

        // Set the UVs & colors
        uv = new Vector2[totalVertices];
        this.colors = new Color[totalVertices];

		for (int i = 0, y = 0; y < resolution; y++) {
			for (int x = 0; x < resolution; x++, i++) {
				uv[i] = new Vector2((float)x / resolution, (float)y / resolution);
			}
		}

        UserSettings.showContours = true; // Also executes EvaluateContours()

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
        mesh.colors = this.colors;
        mesh.triangles = triangles;
        mesh.uv = uv;
        mesh.RecalculateNormals();
    }

    private void GradientFinished(Gradient finishedGradient)
    {
        Debug.Log("You chose a Gradient with " + finishedGradient.colorKeys.Length + " Color keys");
        this.gradient = finishedGradient;
       
        for (int i = 0, y = 0; y < resolution; y++)
        {
            for (int x = 0; x < resolution; x++, i++)
            {
                this.colors[i] = this.gradient.Evaluate(Mathf.InverseLerp(LocalMeshData.maxDepth, LocalMeshData.minDepth, vertices[i].y));

                // Apply macrophyte color mix
                if (UserSettings.macrophyteMaps && MacromapManager.intensityMap != null && !MacromapManager.alreadyUpdating)
                {
                    if (MacromapManager.intensityMap[x, y] != 0f) this.colors[i] = new Color(0f, 1f, 0f) * MacromapManager.intensityMap[x, y] + this.colors[i] * (1f - MacromapManager.intensityMap[x, y]);
                }
            }
        }
        mesh.colors = this.colors;
    }

    // Called by toggle in Lakebed Topography menu
    public void EvaluateGradient()
    {
        if (UserSettings.showGradient)
        {
            for (int i = 0, y = 0; y < resolution; y++)
            { 
                for (int x = 0; x < resolution; x++, i++)
                {
                    this.colors[i] = this.gradient.Evaluate(Mathf.InverseLerp(LocalMeshData.maxDepth, LocalMeshData.minDepth, vertices[i].y)); 

                    // Apply macrophyte color mix
                    if (UserSettings.macrophyteMaps && MacromapManager.intensityMap != null && !MacromapManager.alreadyUpdating)
                    {
                        if (MacromapManager.intensityMap[x, y] != 0f) this.colors[i] = new Color(0f, 1f, 0f) * MacromapManager.intensityMap[x, y] + this.colors[i] * (1f - MacromapManager.intensityMap[x, y]);
                    }
                }
            }
            this.UpdateMesh();
        }
    }

    // Called by toggle in Lakebed Topography menu
    // IMPORTANT: The Doellnsee contours are discontinuous because the heightmap is discontinuous
    public void EvaluateContours(bool apply=true, bool graded=false)
    {
        if (UserSettings.showContours)
        {
            // Create color maps
            Color lakeBedColor = new Color(125f/255f, 90f/255f, 65f/255f);
            Color[] contourColors = new Color[10];
            if (graded) for (int c = 0; c < contourColors.Length; c++) contourColors[c] = new Color(1f-(1f-(float)c/10f), 1f-(1f-(float)c/10f), 1f-(1f-(float)c/10f));
            else for (int c = 0; c < contourColors.Length; c++) contourColors[c] = new Color(1f, 1f, 1f);

            // Apply based on depths
            for (int i = 0, y = 0; y < this.resolution; y++)
            {
                for (int x = 0; x < this.resolution; x++, i++)
                {
                    bool colorApplied = false;
                    int counter = 0;
                    foreach((float, float) bounds in this.contourBoundaries)
                    {
                        if (this.vertices[i].y >= bounds.Item1 && this.vertices[i].y <= bounds.Item2)
                        {
                            colorApplied = true;
                            this.colors[i] = contourColors[counter];
                            break;
                        }
                        counter++;
                    }
                    
                    if (!colorApplied) this.colors[i] = lakeBedColor;
                    
                    // Apply macrophyte color mix
                    if (UserSettings.macrophyteMaps && MacromapManager.intensityMap != null && !MacromapManager.alreadyUpdating)
                    {
                        if (MacromapManager.intensityMap[x, y] != 0f) this.colors[i] = new Color(0f, 1f, 0f) * MacromapManager.intensityMap[x, y] + this.colors[i] * (1f - MacromapManager.intensityMap[x, y]);
                    }
                }
            }

            if (apply) this.UpdateMesh();
        }
    }

    public void ContourWeightSlider()
    {
        float relativeTolerance = MeshManager.instance.weightSlider.normalizedValue;
        float absoluteTolerance = LocalMeshData.maxDiff / (float)MeshManager.instance.numberOfContourPartitions * relativeTolerance / 2f;

        this.CalculateContourBounds(absoluteTolerance);
        this.EvaluateContours(graded:UserSettings.gradedContours);
    }
}
