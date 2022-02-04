using UnityEngine;
using System.Collections.Generic;

public class Species : MonoBehaviour
{
    public static float conversionFactor {get; private set;}
    public static Dictionary<string, GameObject> prefabDict {get; private set;}
    [SerializeField]
    private GameObject perch, roach, pike, tench;

    private void Start()
    {
        float lakeLength = 800;
        float lakeWidth = 500;

        conversionFactor = (LocalMeshData.columnCount / lakeLength + LocalMeshData.rowCount / lakeWidth) / 2;
        prefabDict = new Dictionary<string, GameObject>()
        {
            {"Perch", perch},
            {"Roach", roach},
            {"Pike", pike},
            {"Tench", tench}
        };
    }


}