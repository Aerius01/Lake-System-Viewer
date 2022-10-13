using System.Data;
using System;
using UnityEngine;
using System.Collections.Generic;

public class LocalMeshData
{
    public static float maxDepth, minDepth, maxDiff, lakeDepthOffset = float.MinValue, ndviMax, ndviMin;
    public static DataTable meshMap;
    public static int rowCount, columnCount, resolution;
    public static Vector3 meshCenter;
    public static Texture2D NDVI;
    public static Dictionary<string, int> cutoffs;

    public LocalMeshData(DataTable table, Texture2D NDVI)
    {
        LocalMeshData.meshMap = table;
        LocalMeshData.NDVI = NDVI;
        LocalMeshData.columnCount = meshMap.Columns.Count;
        LocalMeshData.rowCount = meshMap.Rows.Count;

        // Max-min depths
        LocalMeshData.maxDepth = float.MinValue;
        LocalMeshData.minDepth = float.MaxValue;
        LocalMeshData.lakeDepthOffset = float.MaxValue;
        for (int c=0; c<columnCount; c++)
        {
            float iterationMax = Convert.ToSingle(meshMap.Compute(string.Format("Max([{0}])", c), ""));
            float iterationMin = Convert.ToSingle(meshMap.Compute(string.Format("Min([{0}])", c), ""));

            // Lake-depth offset
            DataColumn col = table.Columns[c];
            foreach (DataRow row in table.Rows)
            { if (Convert.ToSingle(row[col]) != 0) lakeDepthOffset = Math.Min(Math.Abs(Convert.ToSingle(row[col])), lakeDepthOffset); }

            maxDepth = Math.Max(iterationMax, maxDepth);
            minDepth = Math.Min(iterationMin, minDepth);
        }

        LocalMeshData.maxDiff = Math.Abs(maxDepth - minDepth);

        // Get the resolution
        int maxDim = Mathf.Max(rowCount, columnCount);
        for (int i = 0; i < 12; i++)
        {
            if (Mathf.Pow(2, i) >= maxDim)
            {
                resolution = Mathf.RoundToInt(Mathf.Pow(2, i)) + 1;
                break;
            }
        }

        cutoffs = new Dictionary<string, int>
        {
            {"minHeight", Mathf.FloorToInt((resolution - rowCount) / 2)},
            {"maxHeight", Mathf.FloorToInt((resolution - rowCount) / 2 + rowCount)},
            {"minWidth", Mathf.FloorToInt((resolution - columnCount) / 2)},
            {"maxWidth", Mathf.FloorToInt((resolution - columnCount) / 2 + columnCount)},
        };

        LocalMeshData.meshCenter = new Vector3((cutoffs["minWidth"] + cutoffs["maxWidth"])/ 2, 0f, (cutoffs["minHeight"] + cutoffs["maxHeight"]) / 2);

        // Create NDVI table
        ndviMax = float.MinValue;
        ndviMin = float.MaxValue;
        for (int i = 0; i < NDVI.height; i++)
        {
            for (int j = 0; j < NDVI.width; j++)
            {
                Color temp = NDVI.GetPixel(i, j);
                ndviMax = Math.Max(temp.r, ndviMax);
                ndviMin = Math.Min(temp.r, ndviMin);
            }
        }
    }
}
