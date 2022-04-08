using UnityEngine;

public class EnvironmentManager : MonoBehaviour
{
    [SerializeField]
    private MeshManager meshManager;
    [SerializeField]
    private TerrainManager terrainManager;
    [SerializeField]
    private GameObject waterBlock;

    // event handler
    public void AdjustScales(float scaleValue)
    {
        // Scale & position mesh
        meshManager.gameObject.transform.localScale = new Vector3(1f, scaleValue, 1f);
        Vector3 meshPosition = meshManager.gameObject.transform.position;
        meshPosition.y = meshManager.depthOffset * scaleValue;
        meshManager.gameObject.transform.position = meshPosition;


        // also adjust height

        // Scale Terrain
        // Vector3 terrainScaler = terrainManager.gameObject.transform.localScale;
        // terrainScaler.y = value;
        // terrainManager.gameObject.transform.localScale = terrainScaler;
    }
}