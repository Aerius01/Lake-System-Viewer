using System.Collections.Generic;
using UnityEngine;

public class MacromapPolygon
{
    public List<Vector2> coordinates { get; private set; }
    public int polygonID { get; private set; }
    public int lowerCoverage { get; private set; }
    public int upperCoverage { get; private set; }

    // Max and mins
    private float minX = float.MaxValue;
    private float maxX = float.MinValue;
    private float minY = float.MaxValue;
    private float maxY = float.MinValue;

    public int vertexCount { get { return coordinates.Count; } }


    public MacromapPolygon(int id, int lower, int upper)
    {
        // Coverage only needs to be attributed once for the entire polygon
        this.lowerCoverage = lower;
        this.upperCoverage = upper;
        this.polygonID = id;

        this.coordinates = new List<Vector2>();
    }

    // Need to transform from lake-local coords to mesh-local coords [LocalMeshData.resolution, LocalMeshData.resolution]
    // The origin of the local macrophyte data is the bottom left corner
    // Since we're drawing submeshes on the existing mesh, and the existing mesh is oriented in [y, x] coords, we need to mimic this
    public void AddPoint(Vector2 newPoint)
    {
        float newY = newPoint.y + LocalMeshData.cutoffs["minHeight"];
        float newX = newPoint.x + LocalMeshData.cutoffs["minWidth"];

        this.coordinates.Add(new Vector2(newY, newX));

        // Update maxes and mins
        if (newY > this.maxY) this.maxY = newY;
        if (newY < this.minY) this.minY = newY;
        if (newX > this.maxX) this.maxX = newX;
        if (newX < this.minX) this.minX = newX;
    }

    public void SortPoints() { this.coordinates = MacromapPolygon.ConvexHull(coordinates); }

    public bool PointInPolygon(Vector2 testPoint)
    {
        // https://stackoverflow.com/questions/4243042/c-sharp-point-in-polygon

        // Determines if the given point is inside the polygon
        // The function counts the number of sides of the polygon that intersect the Y coordinate of the point
        // (the first if() condition) and are to the left of it (the second if() condition). If the number of such
        // sides is odd, then the point is inside the polygon.
        // <param name="polygon">the vertices of polygon</param>
        // <param name="testPoint">the given point</param>
        // <returns>true if the point is inside the polygon; otherwise, false</returns>

        // textPoint is using mesh coordinates which are ordered [y, x], and so testPoint.x is actually the mesh y-coordinate
        bool result = false;
        if (testPoint.x < this.minY || testPoint.x > this.maxY || testPoint.y < this.minX || testPoint.y > this.maxX) return result; // Definitely not within the polygon!
        else
        {
            int j = this.coordinates.Count - 1;
            for (int i = 0; i < this.coordinates.Count; i++)
            {
                if (this.coordinates[i].y < testPoint.y && this.coordinates[j].y >= testPoint.y || this.coordinates[j].y < testPoint.y && this.coordinates[i].y >= testPoint.y)
                {
                    if (this.coordinates[i].x + (testPoint.y - this.coordinates[i].y) / (this.coordinates[j].y - this.coordinates[i].y) * (this.coordinates[j].x - this.coordinates[i].x) < testPoint.x)
                    {
                        result = !result;
                    }
                }
                j = i;
            }
            return result;
        }
    }


    // CONVEX HULL CALCS
    // https://www.geeksforgeeks.org/orientation-3-ordered-points/

    // To find orientation of ordered triplet (p, q, r).
    // The function returns following values
    // 0 --> p, q and r are collinear
    // 1 --> Clockwise
    // 2 --> Counterclockwise
    public static int orientation(Vector2 p, Vector2 q, Vector2 r)
    {
        float val = (q.y - p.y) * (r.x - q.x) - (q.x - p.x) * (r.y - q.y);
    
        if (val == 0) return 0; // collinear
        return (val > 0)? 1: 2; // clock or counterclock wise
    }
    
    // Prints convex hull of a set of n points.
    public static List<Vector2> ConvexHull(List<Vector2> points)
    {
        // Initialize Result
        List<Vector2> hull = new List<Vector2>();

        // There must be at least 3 points
        if (points.Count < 3) return hull;
    
        // Find the leftmost point
        int l = 0;
        for (int i = 1; i < points.Count; i++)
            if (points[i].x < points[l].x)
                l = i;
    
        // Start from leftmost point, keep moving 
        // counterclockwise until reach the start point
        // again. This loop runs O(h) times where h is
        // number of points in result or output.
        int p = l, q;
        do
        {
            // Add current point to result
            hull.Add(points[p]);
    
            // Search for a point 'q' such that 
            // orientation(p, q, x) is counterclockwise 
            // for all points 'x'. The idea is to keep 
            // track of last visited most counterclock-
            // wise point in q. If any point 'i' is more 
            // counterclock-wise than q, then update q.
            q = (p + 1) % points.Count;
            
            for (int i = 0; i < points.Count; i++)
            {
            // If i is more counterclockwise than 
            // current q, then update q
            if (orientation(points[p], points[i], points[q]) == 2) q = i;
            }
    
            // Now q is the most counterclockwise with
            // respect to p. Set p as q for next iteration, 
            // so that q is added to result 'hull'
            p = q;
    
        } while (p != l); // While we don't come to first 
                        // point
        
        return hull;
    }
}
