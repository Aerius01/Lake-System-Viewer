using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using System;

public class TooltipController : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerMoveHandler
{
    [SerializeField] private GameObject tooltip;
    [SerializeField] private RectTransform slideAreaRect;
    private RectTransform rect;
    private TextMeshProUGUI dateText;
    private float upperBound, lowerBound;

    private void Awake()
    {
        // Base attributions
        dateText = tooltip.GetComponentInChildren<TextMeshProUGUI>();
        rect = tooltip.GetComponent<RectTransform>();

        // Get slider bounds
        Vector3[] corners = new Vector3[4];
        slideAreaRect.GetWorldCorners(corners);
        lowerBound = corners[0].x;
        upperBound = corners[3].x;
    }

    public void OnPointerMove(PointerEventData pointerEventData)
    {
        // Update the position
        Vector3 position = new Vector3(pointerEventData.position.x, rect.position.y, rect.position.z);
        rect.position = position;

        // Update the text
        float ratio = (pointerEventData.position.x - lowerBound) / (upperBound - lowerBound);
        double tickDate = PlaybackController.totalTicks * ratio + FishManager.earliestOverallTime.Ticks;
        dateText.text = (new DateTime((long)tickDate)).ToString("dd.MM.yyyy HH:mm");
    }

    public void OnPointerEnter(PointerEventData pointerEventData)
    {
        tooltip.SetActive(true);

        // Update the position
        Vector3 position = new Vector3(pointerEventData.position.x, rect.position.y, rect.position.z);
        rect.position = position;

        // Update the text
        float ratio = (pointerEventData.position.x - lowerBound) / (upperBound - lowerBound);
        double tickDate = PlaybackController.totalTicks * ratio + FishManager.earliestOverallTime.Ticks;
        dateText.text = (new DateTime((long)tickDate)).ToString("dd.MM.yyyy HH:mm");
    }

    public void OnPointerExit(PointerEventData pointerEventData) { tooltip.SetActive(false); }
}