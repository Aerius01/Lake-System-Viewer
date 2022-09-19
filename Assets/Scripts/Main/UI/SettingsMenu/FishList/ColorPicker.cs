using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public delegate void ColorAccepted(string color);

public class ColorPicker : MonoBehaviour
{
    [SerializeField] private CanvasGroup canvasGroup;
    public static event ColorAccepted colorAcceptedEvent;
    private static FishListColoringButton fishListColoringButton;
    public static Color currentColor {get; private set;}
    public static Color standardHeaderColor {get; private set;}
    public static Color standardButtonColor {get; private set;}
    public static Color disabledColor {get; private set;}

    // Singleton Framework
    private static ColorPicker _instance;
    [HideInInspector]
    public static ColorPicker instance {get { return _instance; } set {_instance = value; }}

    private void Awake()
    {
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }

        standardHeaderColor = new Color(28f/255f, 27f/255f, 55f/255f, 100f/255f);
        standardButtonColor = new Color(28f/255f, 27f/255f, 55f/255f, 192f/255f);
        disabledColor = new Color(1f, 1f, 1f, 100f/255f);

        instance.gameObject.SetActive(false);
    }

    private void Update() { HideIfClickedOutside(this.gameObject); }

    public static void ShowMenu(bool status)
    {
        instance.canvasGroup.interactable = status;
        instance.canvasGroup.alpha = status ? 1 : 0;
        instance.gameObject.SetActive(status);
    }

    public void SelectColor(string color)
    {
        ColorPicker.colorAcceptedEvent?.Invoke(color);
        ColorPicker.colorAcceptedEvent = null;
        ShowMenu(false);
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
            ColorPicker.colorAcceptedEvent = null;
        }
    }
}