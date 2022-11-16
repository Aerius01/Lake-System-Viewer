using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

// https://gamedev.stackexchange.com/questions/93592/graphics-raycaster-of-unity-how-does-it-work
public class CanvasRaycast : MonoBehaviour
{
    // Class variables
    private static GraphicRaycaster raycaster;

    // Class Properties
    public static bool hoveringOverUI { get { return CanvasRaycast.CastRay().Count > 0 ? true : false; } }
    public static List<RaycastResult> clickedUIElements { get { return CanvasRaycast.CastRay(); } }

    // Methods
    private void Awake() { CanvasRaycast.raycaster = this.gameObject.GetComponent<GraphicRaycaster>(); }

    private static List<RaycastResult> CastRay()
    {
        List<RaycastResult> results = new List<RaycastResult>();
        PointerEventData pointerEventData = new PointerEventData(null);
        pointerEventData.position = Input.mousePosition;
        CanvasRaycast.raycaster.Raycast(pointerEventData, results);

        return results;
    }
}
    
   