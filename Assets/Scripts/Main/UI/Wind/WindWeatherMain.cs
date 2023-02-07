using UnityEngine.EventSystems;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;
using System.Threading.Tasks;

public delegate void WindChangeEvent(Vector2 newDir, float windSpeed);

public class WindWeatherMain : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    // Required game objects
    [SerializeField] private TextMeshProUGUI windSpeedText, toolTipDeg;
    [SerializeField] private GameObject[] particleObject;
    [SerializeField] private GameObject compassDial, compassArrow, weatherText, rootObject, windObject, genObject;
    [SerializeField] private Toggle toggle;
    [SerializeField] private CanvasGroup rootCanvas, toolTipCanvas, compassCanvas;

    // Singleton
    private static WindWeatherMain _instance;
    [HideInInspector]
    public static WindWeatherMain instance {get { return _instance; } set {_instance = value; }}

    // Update logic
    private WeatherPacket currentPacket = null;
    private Vector3 windStartPos, weatherStartPos;
    private static readonly object locker = new object();
    public bool initialized { get; private set; }
    private bool updating = false, performSyncUpdate = false;

    // Metadata
    public DateTime earliestWeatherTimestamp { get; private set;}
    public DateTime latestWeatherTimestamp { get; private set;}

    // For the particle controller
    private WindChangeEvent windChanged;

    // Properties
    private bool timeBounded
    {
        get
        {
            if (this.currentPacket == null) return false; // no times to be bounded by
            else if (DateTime.Compare(TimeManager.instance.currentTime, this.currentPacket.timestamp) < 0) return false; // the current time is earlier than the packet's timestamp
            else if (this.currentPacket.nextTimestamp == null) return true; // we are in bounds (passed prev condition), but there is no future packet coming
            else
            {
                if (DateTime.Compare(TimeManager.instance.currentTime, this.currentPacket.timestamp) > 0
                && DateTime.Compare(TimeManager.instance.currentTime, Convert.ToDateTime(this.currentPacket.nextTimestamp)) < 0)
                return true; // traditional bounds (middle condition)
                else return false;
            }
        }
    }
    private bool beforeFirstTS
    {
        get
        {
            if (DateTime.Compare(TimeManager.instance.currentTime, this.earliestWeatherTimestamp) < 0) return true;
            else return false;
        }
    }



    private void Awake()
    {
        this.initialized = false;
        this.currentPacket = null;

        // Singleton
        if (_instance != null && _instance != this) Destroy(this.gameObject); 
        else _instance = this; 
    }

    public async Task WakeUp()
    {
        try
        {
            foreach (GameObject obj in particleObject)
            { 
                WindParticleController controller = obj.GetComponent<WindParticleController>();
                controller.GetSystem();
                windChanged += controller.UpdateDirection;
            }

            this.windStartPos = this.windObject.GetComponent<RectTransform>().anchoredPosition;
            this.weatherStartPos = this.genObject.GetComponent<RectTransform>().anchoredPosition;
            
            // Get TS extremes
            DateTime[] boundingDates = await DatabaseConnection.GetWeatherMinMaxTimes();
            this.earliestWeatherTimestamp = boundingDates[0];
            this.latestWeatherTimestamp = boundingDates[1];

            if (this.earliestWeatherTimestamp == DateTime.MaxValue || this.latestWeatherTimestamp == DateTime.MinValue) throw new Exception();

            this.ToggleWind();
            await this.UpdateWindWeather();

            this.rootObject.SetActive(true);
            this.initialized = true;
        }
        catch (Exception e) { this.EnableWindWeather(false); Debug.Log(e.Message); Debug.Log(e.StackTrace); }
    }

    public void Clear()
    {
        this.gameObject.SetActive(false);
        this.EnableWindWeather(false);
        this.windChanged = null;
        this.currentPacket = null;
        this.initialized = false;
    }


    private async Task<bool> FetchNewBounds()
    {
        // if an exception is thrown or no data is returned (null), the method returns false
        try 
        { 
            this.currentPacket = await DatabaseConnection.GetWeatherData(); 
            if (this.currentPacket == null) throw new Exception();
            else return true;
        }
        catch (Exception) { return false; }
    }

    public async Task UpdateWindWeather()
    {
        // Handle asynchronous updates
        // We don't want to senselessly overload the system with queries that return nothing
        if (!this.beforeFirstTS && !this.updating && !this.performSyncUpdate)
        {
            // Secure the multi-threading
            lock(WindWeatherMain.locker) this.updating = true;
            if (!this.timeBounded) { if (await this.FetchNewBounds()) lock(WindWeatherMain.locker) { this.performSyncUpdate = true; } }
            lock(WindWeatherMain.locker) this.updating = false;
        }
        
        if (this.beforeFirstTS) this.currentPacket = null;

    }

    private void Update()
    {
        if (this.initialized)
        {
            this.compassDial.transform.localEulerAngles = new Vector3(0f, 0f, Camera.main.transform.eulerAngles.y);

            // Handle synchronous updates
            if (this.currentPacket == null) { if (this.toggle.interactable) this.EnableWindWeather(false); }
            else if (this.currentPacket != null) { if (!this.toggle.interactable) this.EnableWindWeather(true); }

            lock(WindWeatherMain.locker) 
            {
                if (this.performSyncUpdate)
                {
                    this.performSyncUpdate = false;
                    if (!this.toggle.interactable) this.EnableWindWeather(true);

                    // Execute wind-related updates
                    if (this.currentPacket.windspeed == null || this.currentPacket.windDirection == null)
                    {
                        this.compassCanvas.alpha = 0f;
                        this.toolTipDeg.text = " - °";
                        this.windSpeedText.text = "Wind Speed: - m/s";
                    }
                    else
                    {
                        this.compassCanvas.alpha = 1f;
                        this.compassArrow.transform.localEulerAngles = new Vector3(0f, 0f, 360 - (float)this.currentPacket.windDirection);
                        this.windSpeedText.text = string.Format("Wind Speed: {0:#0.00} m/s", (float)this.currentPacket.windspeed);
                        this.toolTipDeg.text = string.Format("{0:###}°", (float)this.currentPacket.windDirection);

                        // Invoke the event for particle systems to update
                        double radDir = (double)(360f - (float)this.currentPacket.windDirection) * Math.PI / 180f + Math.PI/2;
                        Vector2 unitVector = new Vector2((float)Math.Cos(radDir), (float)Math.Sin(radDir));
                        this.windChanged?.Invoke(unitVector, (float)this.currentPacket.windspeed);
                    }

                    // Manage other weather data
                    string strTemp = this.currentPacket.temperature == null ? " - \n" : string.Format("{0:#0.0} °C\n", (float)this.currentPacket.temperature);
                    string strHumidity = this.currentPacket.humidity == null ? " - \n" : string.Format("{0:#0.0} %\n", (float)this.currentPacket.humidity);
                    string strAirPressure = this.currentPacket.airPressure == null ? " - \n" : string.Format("{0:###0.0} hPa\n", (float)this.currentPacket.airPressure);
                    string strPrecip = this.currentPacket.precipitation == null ? " - \n" : string.Format("{0:#0.0} mm/h", (float)this.currentPacket.precipitation);

                    this.weatherText.GetComponent<TextMeshProUGUI>().text = strTemp + strHumidity + strAirPressure + strPrecip;
                }
            }
        }
    }

    public void OnPointerEnter(PointerEventData eventData) { this.toolTipCanvas.alpha = 1f; }
    public void OnPointerExit(PointerEventData eventData) { this.toolTipCanvas.alpha = 0f; }

    public void ToggleWind()
    {
        if (UserSettings.showWindWeather)
        {
            this.rootCanvas.alpha = 1;
            foreach (GameObject obj in particleObject) { obj.SetActive(true); }
        }
        else
        {
            this.rootCanvas.alpha = 0;
            this.windObject.GetComponent<RectTransform>().localPosition = windStartPos;
            this.genObject.GetComponent<RectTransform>().localPosition = weatherStartPos;
            foreach (GameObject obj in particleObject) { obj.SetActive(false); }
        }
    }

    public void EnableWindWeather(bool status)
    {
        if (status) this.toggle.interactable = true; 
        else
        {
            this.toggle.isOn = false;
            UserSettings.showWindWeather = false;
            this.toggle.interactable = false;

            // Null out wind
            this.compassCanvas.alpha = 0f;
            this.toolTipDeg.text = " - °";
           this. windSpeedText.text = "Wind Speed: - m/s";

            // Null out other weather data
            string strTemp = " - \n";
            string strHumidity = " - \n";
            string strAirPressure = " - \n";
            string strPrecip = " - \n";

           this. weatherText.GetComponent<TextMeshProUGUI>().text = strTemp + strHumidity + strAirPressure + strPrecip;
        }
    }
}
