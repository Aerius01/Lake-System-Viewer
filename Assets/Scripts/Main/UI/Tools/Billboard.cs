using UnityEngine;
 
public class Billboard : MonoBehaviour
{
    private void Update() { this.transform.forward = Camera.main.transform.forward; }
}