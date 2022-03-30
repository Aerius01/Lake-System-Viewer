using UnityEngine;
using System.Collections.Generic;

public class TerrainManager : MonoBehaviour
{
    private int resolution = 1025;
    private TerrainData terrainData;
    private float offset = 0.3f, max = float.MinValue, min = float.MaxValue, nonOverlap = 0.5f;

    private void Start()
    {
        terrainData = this.GetComponent<Terrain>().terrainData;
        Dictionary<string, int> cutoffs = new Dictionary<string, int>
        {
            {"minHeight", Mathf.FloorToInt((resolution - LocalMeshData.lakeHeight) / 2)},
            {"maxHeight", Mathf.FloorToInt((resolution - LocalMeshData.lakeHeight) / 2 + LocalMeshData.lakeHeight)},
            {"minWidth", Mathf.FloorToInt((resolution - LocalMeshData.lakeWidth) / 2)},
            {"maxWidth", Mathf.FloorToInt((resolution - LocalMeshData.lakeWidth) / 2 + LocalMeshData.lakeWidth)},
        };

        // Create and apply height map to terrain
        // Heightmap entries are inverted on row entries for proper orientation in-game
        // All Perlin entries have minimum depth of 0f, and so the 0-1 ratio of the heightmap is scaled
        // // such that 0 to 0.3 is reserved for Perlin noise above lake level, and 0.3 to 1.0 is
        // // reserved for lake depths below base ground level. To achieve this, we need to offset the lake
        // // depth by maxDiff / 0.7f
        terrainData.heightmapResolution = resolution;
        
        float[,] heightMap = new float[resolution, resolution];
        for (int row = 0; row < resolution; row++)
        {
            if (row >= cutoffs["minHeight"] && row < cutoffs["maxHeight"])
            {
                for (int column = 0; column < resolution; column++)
                {
                    if (column >= cutoffs["minWidth"] && column < cutoffs["maxWidth"])
                    {
                        float entryVal = -float.Parse(LocalMeshData.stringTable.Rows[row - cutoffs["minHeight"]][column - cutoffs["minWidth"]].ToString().Trim());
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

        terrainData.size = new Vector3(resolution, -(LocalMeshData.maxDiff / (1f - offset)) * 3, resolution);
        terrainData.SetHeights(0, 0, heightMap);

        // Create and implement splat maps
        terrainData.alphamapResolution = resolution;
        terrainData.SetAlphamaps(0, 0, CreateSplatMap());


        // Apply the terrain data to terrain object/collider
        this.GetComponent<Terrain>().terrainData = this.GetComponent<TerrainCollider>().terrainData = terrainData;
        Vector3 position = this.transform.position;
        position.y = position.y + (offset * LocalMeshData.maxDiff);
        this.transform.position = position;
    }

    private float GetPerlinVal(int row, int col)
    {
        float scale = 10f;
        float perlinVal = Mathf.PerlinNoise(((float)row / (float)resolution) * scale, ((float)col / (float)resolution) * scale);

        return perlinVal * offset;
    }
    
    private float[,,] CreateSplatMap()
    {
        float[,,] map = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, terrainData.terrainLayers.Length];

        // with 6 textures, we have 6 tributary lengths for each of the 6 divisions
        Dictionary<int, List<float>> texRanges = new Dictionary<int, List<float>>()
        {
            {0, new List<float>() {0f, 0.065f, 0.12f}},
            {1, new List<float>() {0.12f, 0.185f, 0.25f}},
            {2, new List<float>() {0.25f, 0.45f, 0.65f}},
            {3, new List<float>() {0.65f, 0.82f, 0.99f}},
            {4, new List<float>() {0.99f, 0.995f, 1f}},
        };

        // For each point on the alphamap...
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                float ndviVal = GetGreyscaleFloat(x, y);
                for (int tex = 0; tex < terrainData.terrainLayers.Length; tex++)
                {
                    if (ndviVal >= texRanges[tex][0] && ndviVal < texRanges[tex][2])
                    { 
                        if (ndviVal >= texRanges[tex][1] - (texRanges[tex][1] - texRanges[tex][0]) * nonOverlap 
                            && ndviVal <= texRanges[tex][1] + (texRanges[tex][2] - texRanges[tex][1]) * nonOverlap)
                        {
                            map[y, x, tex] = 1f; 
                            break; // the rest default to 0f
                        }
                        else if (ndviVal < texRanges[tex][1] - (texRanges[tex][1] - texRanges[tex][0]) * nonOverlap)
                        {
                            if (tex != 0)
                            {
                                float start = texRanges[tex - 1][1] + (texRanges[tex - 1][2] - texRanges[tex - 1][1]) * nonOverlap;
                                float end = texRanges[tex][1] - (texRanges[tex][1] - texRanges[tex][0]) * nonOverlap;
                                map[y, x, tex] = 1f / (end - start) * (ndviVal - start);
                                map[y, x, tex - 1] = 1f - map[y, x, tex];
                                break; // the rest default to 0f
                            }
                            else { map[y, x, tex] = 1f; break; } // the rest default to 0f
                        }
                        else if (ndviVal > texRanges[tex][1] + (texRanges[tex][2] - texRanges[tex][1]) * nonOverlap)
                        {
                            if (tex != terrainData.terrainLayers.Length - 1)
                            {
                                float start = texRanges[tex][1] + (texRanges[tex][2] - texRanges[tex][1]) * nonOverlap;
                                float end = texRanges[tex + 1][1] - (texRanges[tex + 1][1] - texRanges[tex + 1][0]) * nonOverlap;
                                map[y, x, tex] = 1f - (1f / (end - start) * (ndviVal - start));
                                map[y, x, tex + 1] = 1f - map[y, x, tex];
                                break; // the rest default to 0f
                            }
                            else { map[y, x, tex] = 1f; break; } // the rest default to 0f
                        }
                    }
                    else if (tex == (terrainData.terrainLayers.Length - 1) && ndviVal == 1f) { map[y, x, tex] = 1f; } // Edge case 
                    else { map[y, x, tex] = 0f;}
                }

                // // Get the normalized terrain coordinate that
                // // corresponds to the the point.
                // float normX = x * 1.0f / (terrainData.alphamapWidth - 1);
                // float normY = y * 1.0f / (terrainData.alphamapHeight - 1);

                // // Get the steepness value at the normalized coordinate.
                // var angle = terrainData.GetSteepness(normX, normY);

                // // Steepness is given as an angle, 0..90 degrees. Divide
                // // by 90 to get an alpha blending value in the range 0..1.
                // var frac = angle / 90.0;
                // map[x, y, 0] = (float)frac;
                // map[x, y, 1] = (float)(1 - frac);
            }
        }

        return map;
    }

    private float GetGreyscaleFloat(int x, int y)
    {
        Texture2D NDVI = LocalMeshData.NDVI;
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

        float val = (NDVI.GetPixel(x, y).r - LocalMeshData.ndviMin) / (LocalMeshData.ndviMax - LocalMeshData.ndviMin);
        max = Mathf.Max(max, val);
        min = Mathf.Min(min, val);

        // Return value is remapped to [0, 1]
        return (NDVI.GetPixel(x, y).r - LocalMeshData.ndviMin) / (LocalMeshData.ndviMax - LocalMeshData.ndviMin);
    }
}