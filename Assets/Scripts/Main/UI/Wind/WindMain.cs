using UnityEngine.EventSystems;
using UnityEngine;
using TMPro;
using System;
using System.Data;

public class WindMain : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField]
    private TextMeshProUGUI windSpeedText;

    [SerializeField]
    private GameObject compassDial, compassArrow, toolTip;

    [SerializeField]
    private CanvasGroup canvasGroup;

    private static WindMain _instance;
    [HideInInspector]
    public static WindMain instance {get { return _instance; } set {_instance = value; }}

    private int lastIndex = -1, currentIndex;
    private bool dataisNull;
    [HideInInspector]
    public bool jumpingInTime = false;

    private Vector3 boxStartPos;

    private float? windDirection = null, windSpeed = null;

    // For the particle controller
    public bool isNull { get { return dataisNull; } }
    public (float?, float?) windData { get { return (windDirection, windSpeed); } }
    private bool _newData = false;
    public bool newData
    {
        get
        {   
            bool temp = _newData;
            _newData = false;
            return temp;
        }
        private set { _newData = value;}
    }

    
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
        boxStartPos = instance.transform.parent.GetComponent<RectTransform>().position;
        ToggleWind();
    }

    public void UpdateWind()
    {
        // Find most recent timestamp for which there is data
        currentIndex = Array.BinarySearch(LocalWeatherData.uniqueTimeStamps, TimeManager.instance.currentTime);
        // currentIndex = Array.BinarySearch(LocalThermoclineData.uniqueTimeStamps, DateTime.Parse("2015-05-10 00:00:00"));
        if (currentIndex < 0)
        {
            currentIndex = Mathf.Abs(currentIndex) - 2;
        }

        compassDial.transform.localEulerAngles = new Vector3(0f, 0f, Camera.main.transform.eulerAngles.y + 90f);

        // Only update if something is different
        if (jumpingInTime || currentIndex != lastIndex)
        {
            PerformUpdate();
        }

        // End-of-update attributions
        if (jumpingInTime) jumpingInTime = false;
        lastIndex = currentIndex;
    }

    private void PerformUpdate()
    {
        // Retrieve the relevant data
        string searchExp = string.Format("time = #{0}#", LocalWeatherData.uniqueTimeStamps[currentIndex]);
        DataRow[] foundRows = LocalWeatherData.stringTable.Select(searchExp);

        windDirection = windSpeed = null;
        try
        {
            windSpeed = float.Parse(foundRows[0]["windSpeed"].ToString());
            windDirection = float.Parse(foundRows[0]["windDir"].ToString());

            dataisNull = windSpeed == 0f ? true : false;
        }
        catch (FormatException)
        {
            dataisNull = true;
        }

        // Apply required transforms if there is data
        if (dataisNull)
        {
            ApplyNullSettings();
            windSpeedText.text = windSpeed == null ? "Wind Speed: - m/s" : "Wind Speed: 0 m/s";
        }
        else
        {
            compassArrow.GetComponent<CanvasGroup>().alpha = 1f;
            compassArrow.transform.localEulerAngles = new Vector3(0f, 0f, 360 - (float)windDirection);
            windSpeedText.text = string.Format("Wind Speed: {0:#0.00} m/s", windSpeed);
            toolTip.transform.Find("DegText").GetComponent<TextMeshProUGUI>().text = string.Format("{0:###}°", windDirection);
        }

        newData = true;
    }

    private void ApplyNullSettings()
    {
        compassArrow.GetComponent<CanvasGroup>().alpha = 0f;
        toolTip.transform.Find("DegText").GetComponent<TextMeshProUGUI>().text = " - °";
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        toolTip.GetComponent<CanvasGroup>().alpha = 1f;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        toolTip.GetComponent<CanvasGroup>().alpha = 0f;
    }

    public void ToggleWind()
    {
        if (UserSettings.showWind)
        {
            instance.gameObject.GetComponent<CanvasGroup>().alpha = 1;
        }
        else
        {
            instance.gameObject.GetComponent<CanvasGroup>().alpha = 0;
            instance.gameObject.GetComponent<RectTransform>().position = boxStartPos;
        }
    }
}
