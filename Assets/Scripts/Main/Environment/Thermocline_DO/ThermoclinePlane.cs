using UnityEngine;

public class ThermoclinePlane
{
    private GameObject planeObject;
    private Mesh mesh;

    public ThermoclinePlane(Material material)
    {
        planeObject = new GameObject("ThermoclinePlane");
        MeshFilter meshFilter = planeObject.AddComponent(typeof(MeshFilter)) as MeshFilter;
        MeshRenderer meshRenderer = planeObject.AddComponent(typeof(MeshRenderer)) as MeshRenderer;

        mesh = new Mesh();
        mesh.vertices = new Vector3[]
        {
            new Vector3(0,0,0),
            new Vector3(LocalMeshData.resolution,0,0),
            new Vector3(LocalMeshData.resolution,0,LocalMeshData.resolution),
            new Vector3(0,0,LocalMeshData.resolution),
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

    public void RecalculatePlane(float currentDepth)
    {
        mesh.vertices = new Vector3[]
        {
            new Vector3(0,((float)-currentDepth + UserSettings.waterLevel) * UserSettings.verticalScalingFactor,0),
            new Vector3(LocalMeshData.resolution,((float)-currentDepth + UserSettings.waterLevel) * UserSettings.verticalScalingFactor,0),
            new Vector3(LocalMeshData.resolution,((float)-currentDepth + UserSettings.waterLevel) * UserSettings.verticalScalingFactor,LocalMeshData.resolution),
            new Vector3(0,((float)-currentDepth + UserSettings.waterLevel) * UserSettings.verticalScalingFactor,LocalMeshData.resolution),
        };

        planeObject.GetComponent<MeshFilter>().mesh = mesh;
        mesh.RecalculateBounds();
        mesh.RecalculateNormals();
    }

    public GameObject GetObject() { return this.planeObject; }

    public void TogglePlane(bool status) { planeObject.SetActive(status); }
}