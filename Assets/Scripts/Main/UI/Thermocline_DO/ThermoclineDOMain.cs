using UnityEngine;
using System;
using TMPro;

public class ThermoclineDOMain : MonoBehaviour
{
    public ColorBar TempCB, DOCB;
    public Material planeMaterial;
    public TextMeshProUGUI thermoText;
    public GameObject thermoDepth;
    private int lastIndex, currentIndex;
    public ThermoclinePlane thermoclinePlane {get; private set;}
    private float? oldScalingFactor = null;

    public bool jumpingInTime = false;

    private float incrementalHeight;
    private Vector3 originPositionBar, originContainer;

    private static ThermoclineDOMain _instance;
    [HideInInspector]
    public static ThermoclineDOMain instance {get { return _instance; } set {_instance = value; }}

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
        thermoclinePlane = new ThermoclinePlane();
        thermoclinePlane.CreatePlane(planeMaterial);

        float height = TempCB.GetComponent<RectTransform>().rect.height;
        incrementalHeight = height / 10f;
        originPositionBar = thermoDepth.GetComponent<RectTransform>().anchoredPosition;
        originContainer = instance.transform.parent.GetComponent<RectTransform>().position;

        ToggleThermocline();
    }

    public void UpdateBars()
    {
        // Find most recent timestamp for which there is data
        currentIndex = Array.BinarySearch(LocalThermoclineData.uniqueTimeStamps, TimeManager.instance.currentTime);
        // currentIndex = Array.BinarySearch(LocalThermoclineData.uniqueTimeStamps, DateTime.Parse("2015-05-10 00:00:00"));
        if (currentIndex < 0)
        {
            currentIndex = Mathf.Abs(currentIndex) - 2;
        }

        // Only update the bars if something is different or the scaling factor has changed
        if (jumpingInTime || currentIndex != lastIndex)
        {
            thermoclinePlane.RecalculatePlane();
            UpdateThermoclineUI();

            TempCB.UpdateCells(currentIndex, "temp");
            DOCB.UpdateCells(currentIndex, "oxygen");
        }
        else if (oldScalingFactor != UserSettings.verticalScalingFactor)
        {
            thermoclinePlane.RecalculatePlane();
            UpdateThermoclineUI();
        }

        if (jumpingInTime) jumpingInTime = false;

        lastIndex = currentIndex;
        oldScalingFactor = UserSettings.verticalScalingFactor;
    }

    public int CurrentIndex()
    {
        return currentIndex;
    }

    private void UpdateThermoclineUI()
    {
        if (thermoclinePlane.currentDepth != null)
        {
            thermoText.text = string.Format("Thermocline Depth:\n{0:0.00}m", thermoclinePlane.currentDepth);
            
            Vector3 newPosition = originPositionBar;
            float yPos = - incrementalHeight * (float)thermoclinePlane.currentDepth;
            newPosition.y += yPos;
            thermoDepth.GetComponent<RectTransform>().anchoredPosition = newPosition;
        }
        else
        {
            thermoText.text = "Thermocline Depth:\n-";
        }
    }

    public void ToggleThermocline()
    {
        thermoclinePlane.TogglePlane();
        if (UserSettings.showThermocline && thermoclinePlane.currentDepth != null)
        {
            instance.transform.parent.GetComponent<CanvasGroup>().alpha = 1;
        }
        else
        {
            instance.transform.parent.GetComponent<CanvasGroup>().alpha = 0;
            instance.transform.parent.GetComponent<RectTransform>().position = originContainer;
        }
    }
}


