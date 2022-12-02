using System.Collections.Generic;
using UnityEngine;

public class MacromapPolygon
{
    private List<Vector2> coordinates;
    public int polygonID { get; private set; }
    private int lowerCoverage, upperCoverage;

    public int vertexCount { get { return coordinates.Count; } }


    public MacromapPolygon(int id, int lower, int upper)
    {
        // Coverage only needs to be attributed once for the entire polygon
        this.lowerCoverage = lower;
        this.upperCoverage = upper;
        this.polygonID = id;

        this.coordinates = new List<Vector2>();
    }

    public void AddPoint(Vector2 newPoint) { coordinates.Add(newPoint); }
}
