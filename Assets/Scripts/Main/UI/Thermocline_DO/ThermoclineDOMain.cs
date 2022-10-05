using UnityEngine;
using UnityEngine.UI;
using System;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class ThermoclineDOMain : MonoBehaviour
{
    // In-game exposed
    [SerializeField]
    private Material planeMaterial;
    [SerializeField]
    private Toggle toggle;
    [SerializeField]
    private TextMeshProUGUI thermoText;
    [SerializeField]
    private GameObject thermoDepth;
    [SerializeField]
    private ColorBar TempCB, DOCB;

    // Singleton variables
    private static ThermoclineDOMain _instance;
    [HideInInspector]
    public static ThermoclineDOMain instance {get { return _instance; } set {_instance = value; }}

    // Metadata
    public static DateTime earliestWeatherTimestamp { get; private set; }
    public static DateTime latestWeatherTimestamp { get; private set; }
    public ThermoclinePlane thermoclinePlane {get; private set;}
    public static float? currentThermoDepth { get; private set; }
    
    // Other
    private float incrementalHeight;
    private Vector3 originPositionBar, originContainer;

    // Update logic
    private static ThermoPacket currentPacket = null;
    private static readonly object locker = new object();
    private static bool timeBounded
    {
        get
        {
            if (ThermoclineDOMain.currentPacket == null) return false; // no times to be bounded by
            else if (DateTime.Compare(TimeManager.instance.currentTime, ThermoclineDOMain.currentPacket.timestamp) < 0) return false; // the current time is earlier than the packet's timestamp
            else if (ThermoclineDOMain.currentPacket.nextTimestamp == null) return true; // we are in bounds (passed prev condition), but there is no future packet coming
            else
            {
                if (DateTime.Compare(TimeManager.instance.currentTime, ThermoclineDOMain.currentPacket.timestamp) > 0
                && DateTime.Compare(TimeManager.instance.currentTime, Convert.ToDateTime(ThermoclineDOMain.currentPacket.nextTimestamp)) < 0)
                return true; // traditional bounds (middle condition)
                else return false;
            }
        }
    }
    private static bool beforeFirstTS
    {
        get
        {
            if (DateTime.Compare(TimeManager.instance.currentTime, ThermoclineDOMain.earliestWeatherTimestamp) < 0) return true;
            else return false;
        }
    }



    private void Awake()
    {
        // Destroy duplicates instances
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }
    }

    private void Start()
    {
        // Get TS extremes
        DateTime[] boundingDates = DatabaseConnection.GetThermoMinMaxTimes();
        ThermoclineDOMain.earliestWeatherTimestamp = boundingDates[0];
        ThermoclineDOMain.latestWeatherTimestamp = boundingDates[1];

        thermoclinePlane = new ThermoclinePlane(planeMaterial);

        float height = TempCB.GetComponent<RectTransform>().rect.height;
        incrementalHeight = height / 10f;
        originPositionBar = thermoDepth.GetComponent<RectTransform>().anchoredPosition;
        originContainer = instance.transform.parent.GetComponent<RectTransform>().position;

        ToggleThermocline();
        FetchNewBounds();
    }

    private static void FetchNewBounds()
    {
        if (!ThermoclineDOMain.beforeFirstTS)
        {
            try { lock(ThermoclineDOMain.locker) { ThermoclineDOMain.currentPacket = DatabaseConnection.GetThermoData(); } }
            catch (Npgsql.NpgsqlOperationInProgressException)
            {
                lock(ThermoclineDOMain.locker) { ThermoclineDOMain.currentPacket = null; }
                Debug.Log("ThermoclineDOMain; DB Operation already in progress");
            }
        }
        else ThermoclineDOMain.currentPacket = null;

        ThermoclineDOMain.instance.PerformUpdate();
    }

    public void UpdateThermoclineDOMain()
    {
        if (ThermoclineDOMain.currentPacket == null) FetchNewBounds();
        else if (!ThermoclineDOMain.timeBounded) FetchNewBounds();
    }

    private void PerformUpdate()
    {
        if (ThermoclineDOMain.currentPacket == null) { if (ThermoclineDOMain.instance.toggle.interactable) ThermoclineDOMain.EnableThermoclineDOMain(false); }
        else if (ThermoclineDOMain.currentPacket != null)
        {
            // Execute thermo-related updates
            lock(ThermoclineDOMain.locker) 
            {
                currentThermoDepth = CalculateDepth();

                if (currentThermoDepth == null)
                {
                    if (ThermoclineDOMain.instance.toggle.interactable) ThermoclineDOMain.EnableThermoclineDOMain(false);
                    thermoText.text = "Thermocline Depth:\n-";
                }
                else
                {
                    if (!ThermoclineDOMain.instance.toggle.interactable) ThermoclineDOMain.EnableThermoclineDOMain(true);

                    thermoclinePlane.RecalculatePlane((float)currentThermoDepth);
                    thermoText.text = string.Format("Thermocline Depth:\n{0:0.00}m", currentThermoDepth);

                    // Update position of depth indicator on bar widget
                    Vector3 newPosition = originPositionBar;
                    float yPos = - incrementalHeight * (float)currentThermoDepth;
                    newPosition.y += yPos;
                    thermoDepth.GetComponent<RectTransform>().anchoredPosition = newPosition;

                    // Call color bars to update
                    TempCB.UpdateCells("temp", ThermoclineDOMain.currentPacket.readings);
                    DOCB.UpdateCells("oxygen", ThermoclineDOMain.currentPacket.readings);
                }
            }
        }
    }

    public void ToggleThermocline()
    {
        if (UserSettings.showThermocline)
        {
            instance.transform.parent.GetComponent<CanvasGroup>().alpha = 1;
            thermoclinePlane.TogglePlane(true);
        }
        else
        {
            instance.transform.parent.GetComponent<CanvasGroup>().alpha = 0;
            instance.transform.parent.GetComponent<RectTransform>().position = originContainer;
            thermoclinePlane.TogglePlane(false);
        }
    }

    public static void EnableThermoclineDOMain(bool status)
    {
        if (status) ThermoclineDOMain.instance.toggle.interactable = true; 
        else
        {
            ThermoclineDOMain.instance.toggle.isOn = false;
            UserSettings.showThermocline = false;
            ThermoclineDOMain.instance.toggle.interactable = false;
        }
    }

    public static float? CalculateDepth()
    {
        // Suggested by Georgiy Kirillin
        // https://github.com/GLEON/rLakeAnalyzer/blob/ef6de8c3e86d24f1d4190dbb1370afeb4e3181cb/R/thermo.depth.R
        // Removed null handling as this is handled in dictionary creation
        // Find peaks function made by hand instead of GIT version

        List<ThermoReading> refinedList = new List<ThermoReading>();
        foreach (ThermoReading reading in currentPacket.readings)
        { if (reading.density != null) refinedList.Add(reading); }

        // If max is null, then min is also null, only need to check one
        if (ThermoclineDOMain.currentPacket.maxTemp == null) return null;
  
        // Ensure adequate temperature differential
        else if(ThermoclineDOMain.currentPacket.maxTemp - ThermoclineDOMain.currentPacket.minTemp < 1) return null;
  
        // We can't determine anything with less than 3 measurements
        int numberOfEntries = refinedList.Count;
        if(numberOfEntries < 3) return null;

        // Calculate the first derivative of density
        List<double> drho_dz = new List<double>();
        for(int i = 0; i < numberOfEntries - 1; i++)
        {
            ThermoReading entry1 = refinedList[i+1];
            ThermoReading entry2 = refinedList[i];

            drho_dz.Add(((double)entry1.density - (double)entry2.density) / (entry1.depth - entry2.depth));
        }
  
        // look for two distinct maximum slopes, lower one assumed to be seasonal
        int thermoInd = drho_dz.IndexOf(drho_dz.Max());
        double mDrhoZ = drho_dz[thermoInd];
        double thermoD = (refinedList[thermoInd].depth + refinedList[thermoInd+1].depth) / 2;
  
        if(thermoInd > 1 && thermoInd < (numberOfEntries - 2))
        {  
            // if within range
            double Sdn = -(refinedList[thermoInd+1].depth - refinedList[thermoInd].depth)/
            (drho_dz[thermoInd+1] - drho_dz[thermoInd]);
            
            double Sup = (refinedList[thermoInd].depth - refinedList[thermoInd-1].depth)/
                (drho_dz[thermoInd] - drho_dz[thermoInd-1]);
                
            float upD  = refinedList[thermoInd].depth;
            float dnD  = refinedList[thermoInd+1].depth;
            
            if(!Double.IsInfinity(Sup) & !Double.IsInfinity(Sdn)) { thermoD = dnD*(Sdn/(Sdn+Sup))+upD*(Sup/(Sdn+Sup)); }
        }

        float dRhoPerc = 0.15f; // in percentage max for unique thermocline step
        float Smin = 0.1f;
        double dRhoCut = Math.Max(dRhoPerc * mDrhoZ, Smin);

        List<int> inflectionPoints = GetInflectionPoints(drho_dz.ToArray());
        List<int> locs = new List<int>();
        List<double> pks = new List<double>();

        foreach (int point in inflectionPoints)
        {
            // Ensure it's not the last point (point + 1 doesn't exist)
            if (point != numberOfEntries - 1)
            {
                // Ensure it's a peak inflection point and not a valley
                if (refinedList[point].density < refinedList[point + 1].density && refinedList[point].density > dRhoCut)
                {
                    locs.Add(point);
                    pks.Add(drho_dz[point]);
                }
            }
        }

        int SthermoInd = int.MaxValue;
        double SthermoD = int.MaxValue;
        if(locs.Count == 0)
        {
            SthermoD = thermoD;
            SthermoInd = thermoInd;
        }
        else
        {
            mDrhoZ = pks[pks.Count - 1];
            SthermoInd = locs[pks.Count - 1];

            if(SthermoInd > (thermoInd + 1))
            {
                SthermoD = (refinedList[SthermoInd].depth + refinedList[SthermoInd+1].depth) / 2;
                
                if(SthermoInd > 1 && SthermoInd < (numberOfEntries - 2))
                {
                    double Sdn = -(refinedList[thermoInd+1].depth - refinedList[thermoInd].depth)/
                    (drho_dz[thermoInd+1] - drho_dz[thermoInd]);
                    
                    double Sup = (refinedList[thermoInd].depth - refinedList[thermoInd-1].depth)/
                        (drho_dz[thermoInd] - drho_dz[thermoInd-1]);
                        
                    float upD  = refinedList[thermoInd].depth;
                    float dnD  = refinedList[thermoInd+1].depth;
                    
                    if(!Double.IsInfinity(Sup) & !Double.IsInfinity(Sdn)) { SthermoD = dnD*(Sdn/(Sdn+Sup))+upD*(Sup/(Sdn+Sup)); }
                }
            }
            else
            {
                SthermoD = thermoD;
                SthermoInd = thermoInd;
            }
        }
  
        if(SthermoD < thermoD)
        {
            SthermoD = thermoD;
            SthermoInd = thermoInd;
        }

        return (float?)thermoD;
    }

    public static List<int> GetInflectionPoints(double[] source)
    {
        var polarity = true;
        List<int> inflectionPoints = new List<int>();
        for (var i = 1; i < source.Length; i++)
        {
            if (polarity ? source[i] < source[i-1] : source[i] > source[i-1])
            {
                polarity = !polarity;
                inflectionPoints.Add(i - 1);
            }
        }

        return inflectionPoints;
    }
}