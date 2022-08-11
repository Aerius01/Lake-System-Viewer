using UnityEngine;
using System.Collections.Generic;

public class Species : MonoBehaviour
{
    public static Dictionary<string, GameObject> prefabDict {get; private set;}
    [SerializeField]
    private GameObject perch, roach, pike, tench, carp, walleye;

    private void Awake()
    {
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