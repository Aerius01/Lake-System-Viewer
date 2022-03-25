using UnityEngine;
using System.Collections.Generic;

public class TerrainManager : MonoBehaviour
{
    private int resolution = 1025;
    private TerrainData terrainData;
    [SerializeField]
    private Texture2D[] terrainTextures;
    private float offset = 0.3f;

    private void Start()
    {
        terrainData = new TerrainData();
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
                            heightMap[resolution - row - 1, column] = ((entryVal - LocalMeshData.maxDepth) / (LocalMeshData.maxDiff) * (1f - offset) + offset);
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
        // terrainData.SyncHeightmap();

        // // Create and implement splat maps
        // terrainData.alphamapResolution = resolution;

        // TerrainLayer[] terrainLayers = new TerrainLayer[terrainTextures.Length];
        // for (int i = 0; i < terrainTextures.Length; i++)
        // {
        //     terrainLayers[i].diffuseTexture = terrainTextures[i];
        // }
        // terrainData.terrainLayers = terrainLayers;
        // terrainData.SetAlphamaps(0, 0, CreateSplatMap());


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
        float[,,] map = new float[terrainData.alphamapWidth, terrainData.alphamapHeight, 2];

        

        // For each point on the alphamap...
        for (int y = 0; y < terrainData.alphamapHeight; y++)
        {
            for (int x = 0; x < terrainData.alphamapWidth; x++)
            {
                // Get the normalized terrain coordinate that
                // corresponds to the the point.
                float normX = x * 1.0f / (terrainData.alphamapWidth - 1);
                float normY = y * 1.0f / (terrainData.alphamapHeight - 1);

                // Get the steepness value at the normalized coordinate.
                var angle = terrainData.GetSteepness(normX, normY);

                // Steepness is given as an angle, 0..90 degrees. Divide
                // by 90 to get an alpha blending value in the range 0..1.
                var frac = angle / 90.0;
                map[x, y, 0] = (float)frac;
                map[x, y, 1] = (float)(1 - frac);
            }
        }
        
        return map;
    }

}