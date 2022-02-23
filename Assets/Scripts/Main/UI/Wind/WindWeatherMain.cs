using UnityEngine.EventSystems;
using UnityEngine;
using TMPro;
using System;
using System.Data;

public class WindWeatherMain : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Required game objects
    [SerializeField]
    private TextMeshProUGUI windSpeedText;
    [SerializeField]
    private GameObject particleObject, compassDial, compassArrow, toolTip, weatherText;
    [SerializeField]
    private CanvasGroup canvasGroup;

    // Singleton
    private static WindWeatherMain _instance;
    [HideInInspector]
    public static WindWeatherMain instance {get { return _instance; } set {_instance = value; }}

    // Update decision-making
    private int lastIndex = -1, currentIndex;
    private bool dataisNull;
    [HideInInspector]
    public bool jumpingInTime = false;

    // Other
    private Vector3 windStartPos, weatherStartPos;
    private float? windDirection, windSpeed, temp = null, airPressure = null, humidity = null, precip = null;

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
        windStartPos = instance.transform.Find("Wind").GetComponent<RectTransform>().position;
        weatherStartPos = instance.transform.Find("General").GetComponent<RectTransform>().position;
        ToggleWind();
    }

    public void UpdateWindWeather()
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
            // particles will update via the newData property
        }

        // End-of-update attributions
        if (jumpingInTime) jumpingInTime = false;
        lastIndex = currentIndex;
    }

    private void PerformUpdate()
    {
        // Retrieve the relevant wind data
        string searchExp = string.Format("time = #{0}#", LocalWeatherData.uniqueTimeStamps[currentIndex]);
        DataRow[] foundRows = LocalWeatherData.stringTable.Select(searchExp);

        // Get wind data
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

        // Get other weather data
        try { temp = float.Parse(foundRows[0]["temp"].ToString()); }
        catch (FormatException) { temp = null; }

        try { humidity = float.Parse(foundRows[0]["humidity"].ToString()); }
        catch (FormatException) { humidity = null; }

        try { airPressure = float.Parse(foundRows[0]["airPress"].ToString()); }
        catch (FormatException) { airPressure = null; }

        try { precip = float.Parse(foundRows[0]["precip"].ToString()); }
        catch (FormatException) { precip = null; }

        string strTemp = temp == null ? " - \n" : string.Format("{0:#0.0} °C\n", temp);
        string strHumidity = humidity == null ? " - \n" : string.Format("{0:#0.0} %\n", humidity);
        string strAirPressure = airPressure == null ? " - \n" : string.Format("{0:###0.0} hPa\n", airPressure);
        string strPrecip = precip == null ? " - \n" : string.Format("{0:#0.0} mm/h", precip);

        weatherText.GetComponent<TextMeshProUGUI>().text = strTemp + strHumidity + strAirPressure + strPrecip;
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
        if (UserSettings.showWindWeather)
        {
            instance.gameObject.GetComponent<CanvasGroup>().alpha = 1;
            particleObject.SetActive(true);
        }
        else
        {
            instance.gameObject.GetComponent<CanvasGroup>().alpha = 0;
            instance.transform.Find("Wind").GetComponent<RectTransform>().position = windStartPos;
            instance.transform.Find("General").GetComponent<RectTransform>().position = weatherStartPos;
            particleObject.SetActive(false);
        }
    }
}
