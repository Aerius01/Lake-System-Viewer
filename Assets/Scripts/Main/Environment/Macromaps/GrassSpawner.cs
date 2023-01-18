using UnityEngine;
using System.Collections.Generic;

public class GrassSpawner: MonoBehaviour
{
    [SerializeField] private GameObject grassPrefab;
    private List<GameObject> currentPrefabs;

    private float baseExtent;

    // Singleton variables
    private static GrassSpawner _instance;
    [HideInInspector]
    public static GrassSpawner instance {get { return _instance; } set {_instance = value; }}

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }

        this.currentPrefabs = new List<GameObject>();
        this.baseExtent = grassPrefab.transform.GetChild(0).GetComponent<MeshRenderer>().localBounds.extents.y;
    }

    public void SpawnGrass()
    {
        // We need both components for the exercise
        if (HeightManager.currentPacket == null || MacromapManager.intensityMap == null) HeightManager.DisableMaps(); 

        else if (UserSettings.macrophyteHeights)
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
                                if (HeightManager.currentPacket.heightArray[c, r] > 0.1f)
                                {
                                    if (MacromapManager.intensityMap[c, r] / 20f >= (float)rand.NextDouble())
                                    {
                                        GameObject grass = (Instantiate (this.grassPrefab, new Vector3(c, MeshManager.PointDepth(c, r) * UserSettings.verticalScalingFactor, r), new Quaternion()) as GameObject);
                                        grass.transform.parent = this.gameObject.transform;

                                        float scalingFactor = HeightManager.currentPacket.heightArray[c, r] / this.baseExtent * UserSettings.verticalScalingFactor;
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
        else
        {
            foreach (GameObject grass in this.currentPrefabs) { Destroy(grass); } 
            this.currentPrefabs = new List<GameObject>();
        }
    }
}