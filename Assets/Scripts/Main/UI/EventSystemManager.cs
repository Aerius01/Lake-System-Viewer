using UnityEngine.UI;
using UnityEngine;
using TMPro;

public delegate void AlertScaleChange();
public delegate void FishScaleChange(float newVal);
public delegate void WaterLevelChangeEvent();

public class EventSystemManager : MonoBehaviour
{
    public GameObject settingsMenu, heightMapObject;
    private Toggle GISToggle, datePickerToggle;
    private TMP_InputField scalingFactorInput, speedUpInput, waterLevelInput, fishScaleInput;

    [SerializeField]
    private EnvironmentManager environmentManager;

    [SerializeField]
    private Toggle tagToggle, depthLineToggle, trailToggle, thermoToggle, windWeatherToggle, satelliteToggle;

    public static event AlertScaleChange scaleChangeEvent;
    public static event FishScaleChange fishScaleEvent;
    public static event WaterLevelChangeEvent waterLevelEvent;

    private void Awake()
    { 
        Transform baseToggles = settingsMenu.transform.Find("Toggles");
        Transform baseInputs = settingsMenu.transform.Find("Inputs");

        scalingFactorInput = baseInputs.transform.Find("ScalingFactor").transform.Find("ScalingFactorInput").GetComponent<TMP_InputField>();
        speedUpInput = baseInputs.transform.Find("SpeedUpCoeff").transform.Find("SpeedUpInput").GetComponent<TMP_InputField>();
        waterLevelInput = baseInputs.transform.Find("WaterLevel").transform.Find("WaterLevelInput").GetComponent<TMP_InputField>();
        fishScaleInput = baseInputs.transform.Find("FishScale").transform.Find("FishScaleInput").GetComponent<TMP_InputField>();
    }

    private void Start()
    {
        // Set up event listeners
        scaleChangeEvent += environmentManager.AdjustScales;
        scaleChangeEvent += FishManager.ChangeVerticalScale;
        fishScaleEvent += FishManager.ChangeFishScale;
        waterLevelEvent += environmentManager.AdjustWaterLevel;
    }

    public void TagToggle()
    {
        UserSettings.showFishTags = tagToggle.isOn ? true : false;
    }

    public void DepthLineToggle()
    {
        UserSettings.showFishDepthLines = depthLineToggle.isOn ? true : false;
    }

    public void TrailToggle()
    {
        UserSettings.showFishTrails = trailToggle.isOn ? true : false;
    }

    public void ThermoToggle()
    {
        UserSettings.showThermocline = thermoToggle.isOn ? true : false;
    }

    public void WindWeatherToggle()
    {
        UserSettings.showWindWeather = windWeatherToggle.isOn ? true : false;
    }

    public void SatelliteToggle()
    {
        UserSettings.showSatelliteImage = satelliteToggle.isOn ? true : false;
    }

    public void AdjustWaterHeight()
    {
        if (!string.IsNullOrEmpty(waterLevelInput.text) || !string.IsNullOrWhiteSpace(waterLevelInput.text))
        {
            UserSettings.waterLevel = float.Parse(waterLevelInput.text);
            waterLevelEvent?.Invoke();
        }
    }

    public void AdjustScalingFactor()
    {
        if (!string.IsNullOrEmpty(scalingFactorInput.text) || !string.IsNullOrWhiteSpace(scalingFactorInput.text))
        {
            // GameObject meshObject = heightMapObject.transform.Find("MeshMap").gameObject;
            float scaleValue = float.Parse(scalingFactorInput.text);

            if (scaleValue <= 0)
            {
                // we only want positive scaling factors
                scaleValue = 3f;
                scalingFactorInput.text = string.Format("{0}", scaleValue);
            }
            UserSettings.verticalScalingFactor = scaleValue;
            scaleChangeEvent?.Invoke();
        }
    }

    public void AdjustTimeSpeed()
    {
        if (!string.IsNullOrEmpty(speedUpInput.text) || !string.IsNullOrWhiteSpace(speedUpInput.text))
        {
            float enteredValue = float.Parse(speedUpInput.text);

            if (enteredValue <= 0)
            {
                // we only want positive scaling factors
                enteredValue = 10f;
                speedUpInput.text = string.Format("{0}", enteredValue);
            }

            UserSettings.speedUpCoefficient = enteredValue;
        }
    }

    public void AdjustFishScale()
    {
        float scaleValue = float.Parse(fishScaleInput.text);
        if (scaleValue <= 0)
        {
            // we only want positive scaling factors
            scaleValue = 5f;
            scalingFactorInput.text = string.Format("{0}", scaleValue);
        }

        // Need to invoke the event first to use the old scale for relative
        // // scaling in the Fish UpdateScale method
        fishScaleEvent?.Invoke(scaleValue);
        UserSettings.fishScalingFactor = scaleValue;
    }
}
