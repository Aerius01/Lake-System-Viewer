using UnityEngine;
using System.Collections.Generic;

public class GrassSpawner: MonoBehaviour
{
    [SerializeField] private GameObject grassPrefab;
    private List<GameObject> currentPrefabs;

    private float baseExtent;
    private readonly object locker = new object();

    // Singleton variables
    private static GrassSpawner _instance;
    [HideInInspector]
    public static GrassSpawner instance {get { return _instance; } set {_instance = value; }}

    private void Awake()
    {
        // Singleton
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }

        this.currentPrefabs = new List<GameObject>();
        this.baseExtent = grassPrefab.transform.GetChild(0).GetComponent<MeshRenderer>().localBounds.extents.y;
        EventSystemManager.scaleChangeEvent += this.SpawnGrass;
    }

    public void SpawnGrass()
    {
        lock (this.locker)
        {
            // We need both components for the exercise
            if (HeightManager.instance != null && MacromapManager.instance != null)
            {
                if (UserSettings.macrophyteHeights)
                {
                    if (this.currentPrefabs.Count != 0) foreach (GameObject grass in currentPrefabs) { Destroy(grass); } 

                    this.currentPrefabs = new List<GameObject>();
                    System.Random rand = new System.Random();

                    // The intensity map cannot change while the grass is being spawned
                    lock(MacromapManager.mapLocker)
                    {    
                        for (int r = 0; r < LocalMeshData.resolution; r++)
                        {
                            if (r >= LocalMeshData.cutoffs["minHeight"] && r < LocalMeshData.cutoffs["maxHeight"])
                            {
                                for (int c = 0; c < LocalMeshData.resolution; c++)
                                {
                                    if (c >= LocalMeshData.cutoffs["minWidth"] && c < LocalMeshData.cutoffs["maxWidth"])
                                    {
                                        // The model looks very weird if the macrophytes are too short
                                        if (HeightManager.instance.currentPacket.heightArray[c, r] > 0.1f)
                                        {
                                            // Empirical grass spawning roll of the die
                                            if (MacromapManager.instance.intensityMap[c, r] / 20f >= (float)rand.NextDouble())
                                            {
                                                GameObject grass = (Instantiate (this.grassPrefab, new Vector3(c, MeshManager.PointDepth(c, r) * UserSettings.verticalScalingFactor, r), new Quaternion()) as GameObject);
                                                grass.transform.parent = this.gameObject.transform;

                                                float scalingFactor = HeightManager.instance.currentPacket.heightArray[c, r] / this.baseExtent * UserSettings.verticalScalingFactor;
                                                grass.transform.localScale = new Vector3(5f, scalingFactor, 5f);
                                                this.currentPrefabs.Add(grass);
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
                else this.Clear(); 
            }
        }
    }

    public void Clear()
    { 
        foreach (GameObject grass in this.currentPrefabs) { Destroy(grass); } 
        this.currentPrefabs = new List<GameObject>();
    }
}