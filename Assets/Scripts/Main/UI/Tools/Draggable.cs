using UnityEngine;
using UnityEngine.EventSystems;

public class Draggable : MonoBehaviour, IDragHandler
{
    private RectTransform m_DraggingPlane;

    private void Start() { m_DraggingPlane = this.transform.parent.GetComponent<RectTransform>() as RectTransform; }
    public void OnDrag(PointerEventData data) { m_DraggingPlane.anchoredPosition += data.delta; }
}
