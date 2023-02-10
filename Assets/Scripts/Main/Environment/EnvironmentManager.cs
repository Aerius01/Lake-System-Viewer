using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    private static MeshManager meshManager;
    [SerializeField] private GameObject waterBlock;
    [SerializeField] private MeshManager _meshManager;
    // [SerializeField] private TerrainManager _terrainManager;
    // private static TerrainManager terrainManager;

    private void Awake()
    {
        // So that the static variables are assignable in the inspector
        meshManager = _meshManager;
        // terrainManager = _terrainManager;
    }

    // event handler
    public void AdjustScales()
    {
        // Scale & position mesh
        meshManager.gameObject.transform.localScale = new Vector3(1f, UserSettings.verticalScalingFactor, 1f);
        meshManager.ReZeroMesh();

        // // Scale Terrain
        // terrainManager.ResizeTerrain();
        // terrainManager.ReZeroTerrain();

        // Adjust water level (scale only)
        AdjustWaterLevel();
    }

    public void AdjustWaterLevel()
    {
        // Water level has already been updated in UserSettings to new value
        Vector3 newPos = LocalMeshData.meshCenter;
        newPos.y = UserSettings.waterLevel * UserSettings.verticalScalingFactor;
        waterBlock.transform.position = newPos;
    }

    public static void ToggleSatelliteImage(bool sat)
    {
        // terrainManager.gameObject.SetActive(sat);
        meshManager.gameObject.SetActive(!sat);
    }
}