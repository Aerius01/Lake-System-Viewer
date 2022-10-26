using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// https://gamedev.stackexchange.com/questions/93592/graphics-raycaster-of-unity-how-does-it-work
public class CanvasRaycast : MonoBehaviour
{
    private static GraphicRaycaster raycaster;

    public static bool hoveringOverUI
    {
        get
        {
            List<RaycastResult> results = new List<RaycastResult>();
            PointerEventData pointerEventData = new PointerEventData(null);
            pointerEventData.position = Input.mousePosition;
            CanvasRaycast.raycaster.Raycast(pointerEventData, results);

            return results.Count > 0 ? true : false;
        }
    }

    private void Awake() { CanvasRaycast.raycaster = this.gameObject.GetComponent<GraphicRaycaster>(); }
}
    
   