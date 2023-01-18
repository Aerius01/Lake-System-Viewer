using System;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

// Support class
public class MacroHeightPacket
{
    public float[,] heightArray {get; private set;}
    private Dictionary<int, Dictionary<int, List<float>>> assemblyDict;

    public DateTime? timestamp {get; private set;}
    public DateTime? nextTimestamp {get; private set;}


    public MacroHeightPacket(DateTime? timestamp, DateTime? nextTimestamp)
    {
        this.assemblyDict = new Dictionary<int, Dictionary<int, List<float>>>();
        this.heightArray = new float[LocalMeshData.resolution, LocalMeshData.resolution];

        this.timestamp = timestamp;
        this.nextTimestamp = nextTimestamp;
    }

    public void AddPoint(float x, float y, float height)
    {
        // Already bring the coordinates from local to Meshmap coordinates
        int roundedX = (int)Math.Round(x) + LocalMeshData.cutoffs["minWidth"];
        int roundedY = (int)Math.Round(y) + LocalMeshData.cutoffs["minHeight"];

        if (!this.assemblyDict.ContainsKey(roundedX))
        {
            this.assemblyDict[roundedX] = new Dictionary<int, List<float>>();
        }

        if (!this.assemblyDict[roundedX].ContainsKey(roundedY))
        {
            this.assemblyDict[roundedX][roundedY] = new List<float>();
        }

        this.assemblyDict[roundedX][roundedY].Add(height);
    }

    public void CoalesceDictionary()
    {
        for (int y = 0; y < LocalMeshData.resolution; y++)
        {
            for (int x = 0; x < LocalMeshData.resolution; x++)
            {
                if (this.assemblyDict.ContainsKey(x))
                {
                    if (this.assemblyDict[x].ContainsKey(y))
                    {
                        // Switching to [y][x] notation, the notation of arrays
                        this.heightArray[y, x] = this.assemblyDict[x][y].Average();
                    }
                }
                else this.heightArray[y, x] = 0f;
            }
        }
    }
}