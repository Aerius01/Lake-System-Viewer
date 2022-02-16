using UnityEngine;
using System.Collections.Generic;

public class Species : MonoBehaviour
{
    public static float conversionFactor {get; private set;}
    public static Dictionary<string, GameObject> prefabDict {get; private set;}
    [SerializeField]
    private GameObject perch, roach, pike, tench, carp, walleye;

    public void CreateDict()
    {
        float lakeLength = 800;
        float lakeWidth = 500;

        conversionFactor = (LocalMeshData.columnCount / lakeLength + LocalMeshData.rowCount / lakeWidth) / 2;
        prefabDict = new Dictionary<string, GameObject>()
        {
            {"Perch", perch},
            {"Roach", roach},
            {"Pike", pike},
            {"Mirror carp", carp},
            {"Catfish", walleye},
            {"Scaled carp", carp},
            {"Tench", tench}
        };
    }
}