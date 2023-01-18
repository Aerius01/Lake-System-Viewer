using System;
using System.Collections.Generic;
using UnityEngine;

// Support class
public class PolygonPacket
{
    public List<MacromapPolygon> polygons { get; private set; }
    public DateTime? timestamp {get; private set;}
    public DateTime? nextTimestamp {get; private set;}


    public PolygonPacket(DateTime? timestamp, DateTime? nextTimestamp)
    {
        polygons = new List<MacromapPolygon>();

        this.timestamp = timestamp;
        this.nextTimestamp = nextTimestamp;
    }

    public void CataloguePacket(int polygonID, int lowerBound, int upperBound, float x, float y)
    {
        MacromapPolygon relevantPolygon = PolygonExists(polygonID);
        if (relevantPolygon != null) relevantPolygon.AddPoint(new Vector2(x, y)); 
        else
        {
            // Create a new polygon if it doesn't exist
            MacromapPolygon newPolygon = new MacromapPolygon(polygonID, lowerBound, upperBound);
            newPolygon.AddPoint(new Vector2(x, y));
            polygons.Add(newPolygon);
        }
    }

    private MacromapPolygon PolygonExists(int id)
    {
        MacromapPolygon returnPolygon = null;
        foreach (MacromapPolygon polygon in polygons) { if (polygon.polygonID == id) { returnPolygon = polygon; break; } }

        return returnPolygon;
    }

    public void ProcessConvexHulls() { foreach (MacromapPolygon polygon in polygons) { polygon.SortPoints(); } }
}