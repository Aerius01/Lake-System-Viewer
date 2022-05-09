using UnityEngine;
using System;

public delegate void ColorAccepted(Color color);

public class FlexibleColorPickerUtils : MonoBehaviour
{
    private static FishListColoringButton fishListColoringButton;
    public static Color currentColor {get; private set;}
    public static Color standardHeaderColor {get; private set;}
    public static Color standardButtonColor {get; private set;}
    public static Color disabledColor {get; private set;}
    public static event ColorAccepted colorAcceptedEvent;

    private static FlexibleColorPickerUtils _instance;
    [HideInInspector]
    public static FlexibleColorPickerUtils instance {get { return _instance; } set {_instance = value; }}

    private void Awake()
    {
        // Destroy duplicates instances
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }

        standardHeaderColor = new Color(28f/255f, 27f/255f, 55f/255f, 100f/255f);
        standardButtonColor = new Color(28f/255f, 27f/255f, 55f/255f, 192f/255f);
        disabledColor = new Color(1f, 1f, 1f, 100f/255f);
    }

    private void Start() { this.gameObject.SetActive(false); }
    public static void SetNewColor() { if (instance.gameObject.activeSelf != true) { instance.gameObject.SetActive(true); } }

    public void OkayButton()
    {
        currentColor = instance.gameObject.GetComponent<FlexibleColorPicker>().color;
        colorAcceptedEvent?.Invoke(currentColor);
        instance.gameObject.SetActive(false);
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
            colorAcceptedEvent = null;
        }
    }

    private void Update()
    {
        HideIfClickedOutside(this.gameObject);
    }
}