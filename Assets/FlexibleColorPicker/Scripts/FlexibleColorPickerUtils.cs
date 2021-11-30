using UnityEngine;
using System;
using TMPro;

public class FlexibleColorPickerUtils : MonoBehaviour
{
    private static FlexibleColorPickerUtils _instance;
    [HideInInspector]
    public static FlexibleColorPickerUtils instance {get { return _instance; } set {_instance = value; }}
    private static FishListColoringButton fishListColoringButton;

    private void Awake()
    {
        // Destroy duplicates instances
        if (_instance != null && _instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            _instance = this;
        }
    }

    private void Start()
    {
        instance.gameObject.SetActive(false);
    }

    public void SetNewTarget(FishListColoringButton fLButton)
    {
        fishListColoringButton = fLButton;
        if (this.gameObject.activeSelf != true)
        {
            this.gameObject.SetActive(true);
        }
    }

    public void OkayButton()
    {
        fishListColoringButton.SetNewColor(instance.gameObject.GetComponent<FlexibleColorPicker>().color);
        this.gameObject.SetActive(false);
    }

    private void HideIfClickedOutside(GameObject panel)
    {
        // If clicking outside of the FCP, have the box disappear
        if (Input.GetMouseButton(0) && panel.activeSelf && 
            !RectTransformUtility.RectangleContainsScreenPoint(
                panel.GetComponent<RectTransform>(), 
                Input.mousePosition, 
                null))
        {
            panel.SetActive(false);
        }
    }

    private void Update()
    {
        HideIfClickedOutside(instance.gameObject);
    }
}