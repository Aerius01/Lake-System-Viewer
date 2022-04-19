using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    private static MeshManager meshManager;
    private static TerrainManager terrainManager;
    [SerializeField]
    private GameObject waterBlock;
    [SerializeField]
    private MeshManager _meshManager;
    [SerializeField]
    private TerrainManager _terrainManager;

    private void Awake()
    {
        // So that the static variables are assignable in the inspector
        meshManager = _meshManager;
        terrainManager = _terrainManager;
    }

    // event handler
    public void AdjustScales(float scaleValue)
    {
        // Scale & position mesh
        meshManager.gameObject.transform.localScale = new Vector3(1f, scaleValue, 1f);
        meshManager.ReZeroMesh();

        // Scale Terrain
        terrainManager.ResizeTerrain();
        terrainManager.ReZeroTerrain();
    }

    public static void ToggleSatelliteImage(bool sat)
    {
        terrainManager.gameObject.SetActive(sat);
        meshManager.gameObject.SetActive(!sat);
    }
}