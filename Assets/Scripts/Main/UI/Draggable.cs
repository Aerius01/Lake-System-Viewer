using UnityEngine;
using UnityEngine.EventSystems;

public class Draggable : MonoBehaviour, IDragHandler
{
    public GameObject container;
    private RectTransform m_DraggingPlane;

    private void Start()
    {
        m_DraggingPlane = container.transform as RectTransform;
    }

    public void OnDrag(PointerEventData data)
	{
        m_DraggingPlane.anchoredPosition += data.delta;
	}
}
