using UnityEngine;
using UnityEngine.UI;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using TMPro;

public class ThermoclineDOMain : MonoBehaviour
{
    // In-game exposed
    [SerializeField] private Material planeMaterial;
    [SerializeField] private Toggle toggle;
    [SerializeField] private TextMeshProUGUI thermoText;
    [SerializeField] private GameObject thermoDepth, thermoRootObject, widgetText, bufferIcon;
    [SerializeField] private ColorBar TempCB, DOCB;
    [SerializeField] private List<TextMeshProUGUI> textObjects;

    private static ThermoclineDOMain _instance;
    [HideInInspector]
    public static ThermoclineDOMain instance {get { return _instance; } set {_instance = value; }}


    public bool initialized { get; private set; }
    private bool updating = false, performSyncUpdate = false;

    // Metadata
    public DateTime earliestThermoTimestamp { get; private set; }
    public DateTime latestThermoTimestamp { get; private set; }
    public float deepestReading { get; private set; }
    public ThermoclinePlane thermoclinePlane {get; private set;}
    public float? currentThermoDepth { get; private set; }
    public float increment { get; private set; }

    
    // Other
    private float incrementalHeight;
    private Vector3 originPositionBar, originContainer;

    // Update logic
    private ThermoPacket currentPacket = null;
    private static readonly object locker = new object();
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
            if (DateTime.Compare(TimeManager.instance.currentTime, this.earliestThermoTimestamp) < 0) return true;
            else return false;
        }
    }

    private void Awake()
    {
        this.initialized = false;
        this.currentPacket = null;

        // Destroy duplicate instances
        if (_instance != null && _instance != this) { Destroy(this.gameObject); }
        else { _instance = this; }
    }

    public async Task WakeUp()
    {
        try
        {
            // Other inits
            this.thermoclinePlane = new ThermoclinePlane(planeMaterial);
            float height = this.TempCB.GetComponent<RectTransform>().rect.height;
            incrementalHeight = height / 10f;
            originPositionBar = this.thermoDepth.GetComponent<RectTransform>().anchoredPosition;
            originContainer = this.thermoRootObject.GetComponent<RectTransform>().anchoredPosition;

            // Get timestamp extremes
            Tuple<DateTime, DateTime, float> returnPacket = DatabaseConnection.GetThermoMinMaxes(); 
            this.earliestThermoTimestamp = returnPacket.Item1;
            this.latestThermoTimestamp = returnPacket.Item2;
            this.deepestReading = returnPacket.Item3;

            if (this.earliestThermoTimestamp == DateTime.MinValue || this.latestThermoTimestamp == DateTime.MaxValue || this.deepestReading == float.MinValue) throw new Exception();

            this.SetupWidget();

            // Update
            await this.UpdateThermoclineDOMain();

            this.ToggleThermocline();
            this.initialized = true;
        }
        catch (Exception) { this.EnableThermoclineDOMain(false); }
    }

    public void Clear()
    {
        this.initialized = false;
        this.currentPacket = null;
        this.EnableThermoclineDOMain(false);
        this.bufferIcon.SetActive(false);
        lock(ThermoclineDOMain.locker) this.performSyncUpdate = false;

        if (this.thermoclinePlane != null)
        {
            Destroy(this.thermoclinePlane.GetObject());
            this.thermoclinePlane = null;
        }

        this.TempCB.Clear();
        this.DOCB.Clear();
        this.gameObject.SetActive(false);
    }


    private void SetupWidget()
    {
        // Update the in-game element
        this.increment = this.deepestReading / 10f;
        int count = 0;
        foreach(TextMeshProUGUI textObj in this.textObjects) 
        {
            count += 2;
            textObj.text = string.Format("-- {0:#0} -- ", count * this.increment);
        }

        this.TempCB.StartUp();
        this.DOCB.StartUp();
    }

    private async Task<bool> FetchNewBounds()
    {
        // if an exception is thrown or no data is returned (null), the method returns false
        try 
        { 
            this.currentPacket = await DatabaseConnection.GetThermoData(); 
            if (this.currentPacket == null) throw new Exception();
            else return true;
        }
        catch (Exception) { return false; }
    }

    public async Task UpdateThermoclineDOMain()
    {
        // Handle asynchronous updates
        // We don't want to senselessly overload the system with queries that return nothing
        if (!this.beforeFirstTS && !this.updating && !this.performSyncUpdate)
        {
            // Secure the multi-threading
            lock(ThermoclineDOMain.locker) this.updating = true;
            if (!this.timeBounded) { if (await FetchNewBounds()) this.CallSyncUpdate(); }
            lock(ThermoclineDOMain.locker) this.updating = false;
        }

        if (this.beforeFirstTS) this.currentPacket = null;
    }

    private void CallSyncUpdate()
    {
        // Execute thermo-related updates
        lock(ThermoclineDOMain.locker) 
        {
            this.currentThermoDepth = this.CalculateDepth();
            if (this.currentThermoDepth != null) this.performSyncUpdate = true;
        }
    }

    private void Update()
    {
        if (this.initialized)
        {
            // Handle synchronous updates
            if (this.currentPacket == null) { if (this.toggle.interactable) this.EnableThermoclineDOMain(false); }
            else if (this.currentThermoDepth == null) { if (this.toggle.interactable) { this.EnableThermoclineDOMain(false); thermoText.text = "Thermocline Depth:\n-"; } }
            else if (this.currentPacket != null) { if (!this.toggle.interactable) { this.EnableThermoclineDOMain(true); } }

            // Decide whether or not to show the buffering icon
            if (this.updating && !this.bufferIcon.activeSelf) this.bufferIcon.SetActive(true);
            else if (!this.updating && this.bufferIcon.activeSelf) this.bufferIcon.SetActive(false);

            lock(ThermoclineDOMain.locker) 
            {
                if (this.performSyncUpdate)
                {
                    this.performSyncUpdate = false;
                    if (!this.toggle.interactable) this.EnableThermoclineDOMain(true);

                    thermoclinePlane.RecalculatePlane((float)currentThermoDepth);
                    thermoText.text = string.Format("Thermocline Depth:\n{0:0.00}m", currentThermoDepth);

                    // Update position of depth indicator on bar widget
                    Vector3 newPosition = originPositionBar;
                    float yPos = - incrementalHeight * (float)currentThermoDepth;
                    newPosition.y += yPos;
                    thermoDepth.GetComponent<RectTransform>().anchoredPosition = newPosition;

                    // Call color bars to update
                    this.TempCB.UpdateCells("temp", this.currentPacket.readings);
                    this.DOCB.UpdateCells("oxygen", this.currentPacket.readings);
                }
            }
        }
    }

    public void ToggleThermocline()
    {
        if (UserSettings.showThermocline)
        {
            this.thermoRootObject.SetActive(true);
            if (this.thermoclinePlane != null) this.thermoclinePlane.TogglePlane(true);
        }
        else
        {
            this.thermoRootObject.SetActive(false);
            this.thermoRootObject.GetComponent<RectTransform>().localPosition = this.originContainer;
            if (this.thermoclinePlane != null) this.thermoclinePlane.TogglePlane(false);
        }
    }

    public void EnableThermoclineDOMain(bool status)
    {
        if (status) this.toggle.interactable = true; 
        else
        {
            this.toggle.isOn = false;
            UserSettings.showThermocline = false;
            this.toggle.interactable = false;
        }
    }

    public float? CalculateDepth()
    {
        // Suggested by Georgiy Kirillin
        // https://github.com/GLEON/rLakeAnalyzer/blob/ef6de8c3e86d24f1d4190dbb1370afeb4e3181cb/R/thermo.depth.R
        // Removed null handling as this is handled in dictionary creation
        // Find peaks function made by hand instead of GIT version

        List<ThermoReading> refinedList = new List<ThermoReading>();
        foreach (ThermoReading reading in currentPacket.readings)
        { if (reading.density != null) refinedList.Add(reading); }

        // If max is null, then min is also null, only need to check one
        if (this.currentPacket.maxTemp == null) return null;
  
        // Ensure adequate temperature differential
        else if(this.currentPacket.maxTemp - this.currentPacket.minTemp < 1) return null;
  
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