using UnityEngine;
using UnityEngine.UI;

public class LayoutRefresher : MonoBehaviour
{
        private void Update()
        {
                LayoutRebuilder.ForceRebuildLayoutImmediate(this.gameObject.GetComponent<RectTransform>());
        }
}



