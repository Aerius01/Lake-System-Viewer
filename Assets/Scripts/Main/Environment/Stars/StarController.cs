using UnityEngine;

public class StarController : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        this.transform.position = LocalMeshData.meshCenter;
        this.GetComponent<ParticleSystem>().Clear();
    }
}
