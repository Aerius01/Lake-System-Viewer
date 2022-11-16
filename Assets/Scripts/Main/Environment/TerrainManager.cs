using UnityEngine;
using System.Collections.Generic;
using sc.terrain.vegetationspawner;

public class TerrainManager : MonoBehaviour
{
    private int resolution;
    private TerrainData terrainData;
    private float offset = 0.3f, nonOverlap = 0.5f;

    private static TerrainManager _instance;
    [HideInInspector]
    public static TerrainManager instance {get { return _instance; } set {_instance = value; }}

    private void Awake()
    {
        // Destroy duplicates instances
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }

        this.gameObject.SetActive(false);
    }

    public void SetUpTerrain()
    {
        terrainData = this.GetComponent<Terrain>().terrainData;
        resolution = LocalMeshData.resolution;

        // Create and implement height map
        terrainData.heightmapResolution = resolution;
        terrainData.size = new Vector3(resolution, -(LocalMeshData.maxDiff / (1f - offset)) * UserSettings.verticalScalingFactor, resolution);
        terrainData.SetHeights(0, 0, CreateHeightmap());

        // Create and implement splat maps
        terrainData.alphamapResolution = resolution;
        terrainData.SetAlphamaps(0, 0, CreateSplatMap());

        // Apply the terrain data to terrain object/collider and position terrain at origin with water level at y=0
        this.GetComponent<Terrain>().terrainData = this.GetComponent<TerrainCollider>().terrainData = terrainData;
        ReZeroTerrain();

        // Respawn all trees and grass
        // this.transform.parent.transform.Find("VegeSpawner").GetComponent<VegetationSpawner>().Respawn();
    }

    public void ResizeTerrain()
    {
        Vector3 newSize = new Vector3(resolution, -(LocalMeshData.maxDiff / (1f - offset)) * UserSettings.verticalScalingFactor, resolution);
        terrainData.size = newSize;
        this.GetComponent<Terrain>().terrainData = this.GetComponent<TerrainCollider>().terrainData = terrainData;
    }

    public void ReZeroTerrain()
    {
        Vector3 position = this.transform.position;
        position = new Vector3(0f, offset * Mathf.Abs(terrainData.size.y), 0f);
        this.transform.position = position;
    }

    private float[,] CreateHeightmap()
    {
        // Create and apply height map to terrain
        // Heightmap entries are inverted on row entries for proper orientation in-game
        // All Perlin entries have minimum depth of 0f, and so the 0-1 ratio of the heightmap is scaled
        // // such that 0 to 0.3 is reserved for Perlin noise above lake level, and 0.3 to 1.0 is
        // // reserved for lake depths below base ground level. To achieve this, we need to offset the lake
        // // depth by maxDiff / 0.7f
        float[,] heightMap = new float[resolution, resolution];
        for (int row = 0; row < resolution; row++)
        {
            if (row >= LocalMeshData.cutoffs["minHeight"] && row < LocalMeshData.cutoffs["maxHeight"])
            {
                for (int column = 0; column < resolution; column++)
                {
                    if (column >= LocalMeshData.cutoffs["minWidth"] && column < LocalMeshData.cutoffs["maxWidth"])
                    {
                        float entryVal = -float.Parse(LocalMeshData.meshMap.Rows[row - LocalMeshData.cutoffs["minHeight"]][column - LocalMeshData.cutoffs["minWidth"]].ToString().Trim());
                        if (entryVal != 0f)
                        {
                            // Remap to offset range
                            heightMap[resolution - row - 1, column] = (((entryVal - LocalMeshData.lakeDepthOffset) - LocalMeshData.maxDepth) / (LocalMeshData.maxDiff) * (1f - offset) + offset);
                        }
                        else { heightMap[resolution - row - 1, column] = GetPerlinVal(row, column); }
                    }
                    // Outside of meaningful resolution
                    else { heightMap[resolution - row - 1, column] = GetPerlinVal(row, column); }
                }
            }
            else
            {
                // Outside of meaningful resolution
                for (int column = 0; column < resolution; column++) { heightMap[resolution - row - 1, column] = GetPerlinVal(row, column); }
            }
        }

        return heightMap;
    }

    private float[,,] CreateSplatMap()
    {
        float[,,] map = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.terrainLayers.Length];
        Dictionary<int, List<float>> texRanges = new Dictionary<int, List<float>>()
        {
            {0, new List<float>() {0f, 0.065f, 0.12f}}, // mud
            {1, new List<float>() {0.12f, 0.185f, 0.25f}}, // rocky mud
            {2, new List<float>() {0.25f, 0.45f, 0.65f}}, // rocky grass
            {3, new List<float>() {0.65f, 0.82f, 0.99f}}, // grass
            {4, new List<float>() {0.99f, 0.995f, 1f}}, // small rocks
        };

        // For each point on the alphamap...
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                float normX = x * 1.0f / (terrainData.alphamapWidth - 1);
                float normY = y * 1.0f / (terrainData.alphamapHeight - 1);
                float steepness = terrainData.GetSteepness(normX, normY); // angle in [0, 90+]

                // If not too steep, go through the normal texturing process, otherwise use mud/rock
                if (steepness <= 70f)
                {
                    float ndviVal = GetGreyscaleFloat(x, y);
                    for (int tex = 0; tex < terrainData.terrainLayers.Length; tex++)
                    {
                        if (ndviVal >= texRanges[tex][0] && ndviVal < texRanges[tex][2])
                        { 
                            float lowerCut = (texRanges[tex][1] - texRanges[tex][0]) * nonOverlap;
                            float upperCut = (texRanges[tex][2] - texRanges[tex][1]) * nonOverlap;
                            
                            // Within the inner range, no blending
                            if (ndviVal >= texRanges[tex][1] - lowerCut && ndviVal <= texRanges[tex][1] + upperCut)
                            { map[y, x, tex] = 1f; break; } // the rest default to 0f

                            // Within the lower range, linear blending with previous texture
                            else if (ndviVal < texRanges[tex][1] - lowerCut)
                            {
                                if (tex != 0)
                                {
                                    float start = texRanges[tex - 1][1] + (texRanges[tex - 1][2] - texRanges[tex - 1][1]) * nonOverlap;
                                    float end = texRanges[tex][1] - lowerCut;
                                    map[y, x, tex] = 1f / (end - start) * (ndviVal - start);
                                    map[y, x, tex - 1] = 1f - map[y, x, tex];
                                    break; // the rest default to 0f
                                }
                                else { map[y, x, tex] = 1f; break; } // the rest default to 0f
                            }
                            // Within the upper range, linear blending with next texture
                            else if (ndviVal > texRanges[tex][1] + upperCut)
                            {
                                if (tex != terrainData.terrainLayers.Length - 1)
                                {
                                    float start = texRanges[tex][1] + upperCut;
                                    float end = texRanges[tex + 1][1] - (texRanges[tex + 1][1] - texRanges[tex + 1][0]) * nonOverlap;
                                    map[y, x, tex] = 1f - (1f / (end - start) * (ndviVal - start));
                                    map[y, x, tex + 1] = 1f - map[y, x, tex];
                                    break; // the rest default to 0f
                                }
                                else { map[y, x, tex] = 1f; break; } // the rest default to 0f
                            }
                        }
                        else if (tex == (terrainData.terrainLayers.Length - 1) && ndviVal == 1f)
                        { map[y, x, tex] = 1f; } // Edge case 
                        else { map[y, x, tex] = 0f;}
                    }
                }
                else
                {
                    // really steep, use mud texture
                    map[y, x, 0] = 1f;
                }
            }
        }

        return map;
    }

    private float GetGreyscaleFloat(int x, int y)
    {
        Texture2D NDVI = LocalMeshData.NDVI;

        // Remap sampling position based on resolution of NDVI map against alpha map
        if (NDVI.height + 1 > resolution)
        {
            x = Mathf.RoundToInt(x * (NDVI.width / (resolution - 1)));
            y = Mathf.RoundToInt(y * (NDVI.height / (resolution - 1)));
        }
        else if (NDVI.height + 1 < resolution)
        {
            x = Mathf.RoundToInt(x / (NDVI.width / (resolution - 1)));
            y = Mathf.RoundToInt(y / (NDVI.height / (resolution - 1)));
        }

        // Return value is remapped to [0, 1]
        return (NDVI.GetPixel(x, y).r - LocalMeshData.ndviMin) / (LocalMeshData.ndviMax - LocalMeshData.ndviMin);
    }

    private float GetPerlinVal(int row, int col)
    {
        float scale = 10f;
        float perlinVal = Mathf.PerlinNoise(((float)row / (float)resolution) * scale, ((float)col / (float)resolution) * scale);

        return perlinVal * offset;
    }
}