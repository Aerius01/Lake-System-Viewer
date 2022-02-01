using UnityEngine;

public class ThermoclinePlane
{
    private GameObject planeObject;
    private Mesh mesh;
    public float? currentDepth {get; private set;}

    public void CreatePlane(Material material)
    {
        planeObject = new GameObject("ThermoclinePlane");
        MeshFilter meshFilter = planeObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
        MeshRenderer meshRenderer = planeObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;

        mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(0,0,0),
            new Vector3(LocalMeshData.rowCount,0,0),
            new Vector3(LocalMeshData.rowCount,0,LocalMeshData.columnCount),
            new Vector3(0,0,LocalMeshData.columnCount),
        };

        mesh.uv = new Vector2[]
        {
            new Vector2(0,0),
            new Vector2(0,1),
            new Vector2(1,1),
            new Vector2(1,0),
        };

        mesh.triangles = new int[] {0,2,1,0,3,2};
        meshFilter.mesh = mesh;

        meshRenderer.material = material;

        (planeObject.AddComponent(typeof(MeshCollider)) as MeshCollider).sharedMesh = mesh;

        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    public void RecalculatePlane()
    {
        ThermoclDepth depthCalc = new ThermoclDepth();
        var (depth, index) = depthCalc.ThermoDepth();
        currentDepth = depth;

        if (depth != null)
        {
            if (!planeObject.activeSelf && UserSettings.showThermocline)
            {
                planeObject.SetActive(true);
            }

            mesh.vertices = new Vector3[]
            {
                new Vector3(0,(float)-depth * UserSettings.verticalScalingFactor,0),
                new Vector3(LocalMeshData.rowCount,(float)-depth * UserSettings.verticalScalingFactor,0),
                new Vector3(LocalMeshData.rowCount,(float)-depth * UserSettings.verticalScalingFactor,LocalMeshData.columnCount),
                new Vector3(0,(float)-depth * UserSettings.verticalScalingFactor,LocalMeshData.columnCount),
            };

            planeObject.GetComponent<MeshFilter>().mesh = mesh;
            mesh.RecalculateBounds();
            mesh.RecalculateNormals();
        }
        else
        {
            if (planeObject.activeSelf)
            {
                planeObject.SetActive(false);
            }
        }
    }

    public void TogglePlane()
    {
        planeObject.SetActive(!planeObject.activeSelf);
    }
}