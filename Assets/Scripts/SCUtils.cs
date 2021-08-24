using UnityEngine;
using UnityEngine.EventSystems;

public class SCUtils : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler 
{
    [HideInInspector]
    public bool mouse_over = false, clicked = false;

    public void OnPointerEnter(PointerEventData eventData)
    {
        mouse_over = true;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        mouse_over = false;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        clicked = true;
        Debug.Log("clicked");
    }
}