using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public delegate void WindChangeEvent(Vector2 newDir, float windSpeed);

public class WindWeatherMain : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Required game objects
    [SerializeField]
    private TextMeshProUGUI windSpeedText;
    [SerializeField]
    private GameObject[] particleObject;
    [SerializeField]
    private GameObject compassDial, compassArrow, toolTip, weatherText;
    [SerializeField]
    private Toggle toggle;

    // Singleton
    private static WindWeatherMain _instance;
    [HideInInspector]
    public static WindWeatherMain instance {get { return _instance; } set {_instance = value; }}

    // Update logic
    private static WeatherPacket currentPacket = null;
    private static readonly object locker = new object();
    private static bool timeBounded
    {
        get
        {
            if (WindWeatherMain.currentPacket == null) return false; // no times to be bounded by
            else if (DateTime.Compare(TimeManager.instance.currentTime, WindWeatherMain.currentPacket.timestamp) < 0) return false; // the current time is earlier than the packet's timestamp
            else if (WindWeatherMain.currentPacket.nextTimestamp == null) return true; // we are in bounds (passed prev condition), but there is no future packet coming
            else
            {
                if (DateTime.Compare(TimeManager.instance.currentTime, WindWeatherMain.currentPacket.timestamp) > 0
                && DateTime.Compare(TimeManager.instance.currentTime, Convert.ToDateTime(WindWeatherMain.currentPacket.nextTimestamp)) < 0)
                return true; // traditional bounds (middle condition)
                else return false;
            }
        }
    }
    private static bool beforeFirstTS
    {
        get
        {
            if (DateTime.Compare(TimeManager.instance.currentTime, WindWeatherMain.earliestWeatherTimestamp) < 0) return true;
            else return false;
        }
    }

    // Metadata
    public static DateTime earliestWeatherTimestamp { get; private set;}
    public static DateTime latestWeatherTimestamp { get; private set;}

    // Other
    private Vector3 windStartPos, weatherStartPos;

    // For the particle controller
    private WindChangeEvent windChanged;

    private void Awake()
    {
        // Singleton
        if (_instance != null && _instance != this) Destroy(this.gameObject); 
        else _instance = this; 
    }

    private void Start()
    {
        foreach (GameObject obj in particleObject)
        { 
            WindParticleController controller = obj.GetComponent<WindParticleController>();
            controller.GetSystem();
            windChanged += controller.UpdateDirection;
        }

        windStartPos = instance.transform.Find("Wind").GetComponent<RectTransform>().position;
        weatherStartPos = instance.transform.Find("General").GetComponent<RectTransform>().position;
        
        // Get TS extremes
        DateTime[] boundingDates = DatabaseConnection.GetWeatherMinMaxTimes();
        WindWeatherMain.earliestWeatherTimestamp = boundingDates[0];
        WindWeatherMain.latestWeatherTimestamp = boundingDates[1];

        ToggleWind();
        FetchNewBounds();
    }

    private static void FetchNewBounds()
    {
        if (!WindWeatherMain.beforeFirstTS)
        {
            try { lock(WindWeatherMain.locker) { WindWeatherMain.currentPacket = DatabaseConnection.GetWeatherData(); } }
            catch (Npgsql.NpgsqlOperationInProgressException)
            {
                lock(WindWeatherMain.locker) { WindWeatherMain.currentPacket = null; }
                Debug.Log("WindWeather; DB Operation already in progress");
            }
        }
        else WindWeatherMain.currentPacket = null;

        WindWeatherMain.instance.PerformUpdate();
    }

    public void UpdateWindWeather()
    {
        if (WindWeatherMain.currentPacket == null) FetchNewBounds();
        else if (!WindWeatherMain.timeBounded) FetchNewBounds();

        compassDial.transform.localEulerAngles = new Vector3(0f, 0f, Camera.main.transform.eulerAngles.y);
    }

    private void PerformUpdate()
    {
        if (WindWeatherMain.currentPacket == null)
        {
            if (WindWeatherMain.instance.toggle.interactable) WindWeatherMain.EnableWindWeather(false);

            // Null out wind
            compassArrow.GetComponent<CanvasGroup>().alpha = 0f;
            toolTip.transform.Find("DegText").GetComponent<TextMeshProUGUI>().text = " - °";
            windSpeedText.text = "Wind Speed: - m/s";

            // Null out other weather data
            string strTemp = " - \n";
            string strHumidity = " - \n";
            string strAirPressure = " - \n";
            string strPrecip = " - \n";

            weatherText.GetComponent<TextMeshProUGUI>().text = strTemp + strHumidity + strAirPressure + strPrecip;

        }
        else if (WindWeatherMain.currentPacket != null)
        {
            // TODO
            if (!WindWeatherMain.instance.toggle.interactable) WindWeatherMain.EnableWindWeather(true);

            // Execute wind-related updates
            if (WindWeatherMain.currentPacket.windspeed == null || WindWeatherMain.currentPacket.windDirection == null)
            {
                compassArrow.GetComponent<CanvasGroup>().alpha = 0f;
                toolTip.transform.Find("DegText").GetComponent<TextMeshProUGUI>().text = " - °";
                windSpeedText.text = "Wind Speed: - m/s";
            }
            else
            {
                compassArrow.GetComponent<CanvasGroup>().alpha = 1f;
                compassArrow.transform.localEulerAngles = new Vector3(0f, 0f, 360 - (float)WindWeatherMain.currentPacket.windDirection);
                windSpeedText.text = string.Format("Wind Speed: {0:#0.00} m/s", (float)WindWeatherMain.currentPacket.windspeed);
                toolTip.transform.Find("DegText").GetComponent<TextMeshProUGUI>().text = string.Format("{0:###}°", (float)WindWeatherMain.currentPacket.windDirection);

                // Invoke the event for particle systems to update
                double radDir = (double)(360f - (float)WindWeatherMain.currentPacket.windDirection) * Math.PI / 180f + Math.PI/2;
                Vector2 unitVector = new Vector2((float)Math.Cos(radDir), (float)Math.Sin(radDir));
                windChanged?.Invoke(unitVector, (float)WindWeatherMain.currentPacket.windspeed);
            }

            // Manage other weather data
            string strTemp = WindWeatherMain.currentPacket.temperature == null ? " - \n" : string.Format("{0:#0.0} °C\n", (float)WindWeatherMain.currentPacket.temperature);
            string strHumidity = WindWeatherMain.currentPacket.humidity == null ? " - \n" : string.Format("{0:#0.0} %\n", (float)WindWeatherMain.currentPacket.humidity);
            string strAirPressure = WindWeatherMain.currentPacket.airPressure == null ? " - \n" : string.Format("{0:###0.0} hPa\n", (float)WindWeatherMain.currentPacket.airPressure);
            string strPrecip = WindWeatherMain.currentPacket.precipitation == null ? " - \n" : string.Format("{0:#0.0} mm/h", (float)WindWeatherMain.currentPacket.precipitation);

            weatherText.GetComponent<TextMeshProUGUI>().text = strTemp + strHumidity + strAirPressure + strPrecip;
        }
    }

    public void OnPointerEnter(PointerEventData eventData) { toolTip.GetComponent<CanvasGroup>().alpha = 1f; }
    public void OnPointerExit(PointerEventData eventData) { toolTip.GetComponent<CanvasGroup>().alpha = 0f; }

    public void ToggleWind()
    {
        if (UserSettings.showWindWeather)
        {
            instance.gameObject.GetComponent<CanvasGroup>().alpha = 1;
            foreach (GameObject obj in particleObject) { obj.SetActive(true); }
        }
        else
        {
            instance.gameObject.GetComponent<CanvasGroup>().alpha = 0;
            instance.transform.Find("Wind").GetComponent<RectTransform>().position = windStartPos;
            instance.transform.Find("General").GetComponent<RectTransform>().position = weatherStartPos;
            foreach (GameObject obj in particleObject) { obj.SetActive(false); }
        }
    }

    public static void EnableWindWeather(bool status)
    {
        if (status) WindWeatherMain.instance.toggle.interactable = true; 
        else
        {
            WindWeatherMain.instance.toggle.isOn = false;
            UserSettings.showWindWeather = false;
            WindWeatherMain.instance.toggle.interactable = false;
        }
    }
}
