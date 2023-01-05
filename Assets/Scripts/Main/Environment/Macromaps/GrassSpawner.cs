using UnityEngine;
using System.Collections.Generic;
using System;

public class GrassSpawner: MonoBehaviour
{
    [SerializeField] private GameObject grassPrefab;
    private List<GameObject> currentPrefabs;

    // Singleton variables
    private static GrassSpawner _instance;
    [HideInInspector]
    public static GrassSpawner instance {get { return _instance; } set {_instance = value; }}

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }
    }

    public void SpawnGrass()
    {
        if (UserSettings.macrophyteMaps)
        {
            if (this.currentPrefabs == null) this.currentPrefabs = new List<GameObject>();
            else if (this.currentPrefabs.Count != 0) foreach (GameObject grass in currentPrefabs) { Destroy(grass); } 

            this.currentPrefabs = new List<GameObject>();
            System.Random rand = new System.Random();

            for (int r = 0; r < LocalMeshData.resolution; r++)
            {
                if (r >= LocalMeshData.cutoffs["minHeight"] && r < LocalMeshData.cutoffs["maxHeight"])
                {
                    for (int c = 0; c < LocalMeshData.resolution; c++)
                    {
                        if (c >= LocalMeshData.cutoffs["minWidth"] && c < LocalMeshData.cutoffs["maxWidth"])
                        {
                            if (MacromapManager.intensityMap[c, r] / 10f >= (float)rand.NextDouble())
                            {
                                GameObject grass = (Instantiate (this.grassPrefab, new Vector3(c, MeshManager.PointDepth(c, r) * UserSettings.verticalScalingFactor, r), new Quaternion()) as GameObject);
                                grass.transform.parent = this.gameObject.transform;
                                grass.transform.localScale = new Vector3(5f, 30f * (float)rand.NextDouble(), 5f);
                                this.currentPrefabs.Add(grass);
                            }
                        }
                    }
                }
            }
        }
        else
        {
            foreach (GameObject grass in this.currentPrefabs) { Destroy(grass); } 
            this.currentPrefabs = new List<GameObject>();
        }
    }
}