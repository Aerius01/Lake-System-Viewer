using UnityEngine.UI;
using UnityEngine;
using TMPro;

public delegate void AlertScaleChange(float newVal);

public class EventSystemManager : MonoBehaviour
{
    public GameObject settingsMenu, heightMapObject;
    private Toggle tagToggle, depthLineToggle, trailToggle, GISToggle, datePickerToggle, thermoToggle, windWeatherToggle;
    private TMP_InputField scalingFactorInput, speedUpInput, waterLevelInput;

    [SerializeField]
    private EnvironmentManager environmentManager;

    public static event AlertScaleChange scaleChangeEvent;

    private void Awake()
    { 
        Transform baseToggles = settingsMenu.transform.Find("Toggles");
        Transform baseInputs = settingsMenu.transform.Find("Inputs");

        tagToggle = baseToggles.transform.Find("TagToggle").GetComponent<Toggle>();
        depthLineToggle = baseToggles.transform.Find("DepthLineToggle").GetComponent<Toggle>();
        trailToggle = baseToggles.transform.Find("TrailToggle").GetComponent<Toggle>();
        thermoToggle = baseToggles.transform.Find("ThermoclineToggle").GetComponent<Toggle>();
        windWeatherToggle = baseToggles.transform.Find("WindWeatherToggle").GetComponent<Toggle>();

        scalingFactorInput = baseInputs.transform.Find("ScalingFactor").transform.Find("ScalingFactorInput").GetComponent<TMP_InputField>();
        speedUpInput = baseInputs.transform.Find("SpeedUpCoeff").transform.Find("SpeedUpInput").GetComponent<TMP_InputField>();
        waterLevelInput = baseInputs.transform.Find("WaterLevel").transform.Find("WaterLevelInput").GetComponent<TMP_InputField>();
    }

    private void Start()
    {
        UserSettings.verticalScalingFactor = 1;
        scalingFactorInput.text = UserSettings.verticalScalingFactor.ToString();
        TimeManager.instance.speedUpCoefficient = 10;
        speedUpInput.text = TimeManager.instance.speedUpCoefficient.ToString();

        scaleChangeEvent += environmentManager.AdjustScales;
    }

    public void TagToggle()
    {
        if (tagToggle.isOn)
        {
            UserSettings.showFishTags = true;
        }
        else
        {
            UserSettings.showFishTags = false;
        }
    }

    public void DepthLineToggle()
    {
        if (depthLineToggle.isOn)
        {
            UserSettings.showFishDepthLines = true;
        }
        else
        {
            UserSettings.showFishDepthLines = false;
        }
    }

    public void TrailToggle()
    {
        if (trailToggle.isOn)
        {
            UserSettings.showFishTrails = true;
        }
        else
        {
            UserSettings.showFishTrails = false;
        }
    }

    public void ThermoToggle()
    {
        if (thermoToggle.isOn)
        {
            UserSettings.showThermocline = true;
        }
        else
        {
            UserSettings.showThermocline = false;
        }
    }

    public void WindWeatherToggle()
    {
        if (windWeatherToggle.isOn)
        {
            UserSettings.showWindWeather = true;
        }
        else
        {
            UserSettings.showWindWeather = false;
        }
    }

    public void AdjustWaterHeight()
    {
        if (!string.IsNullOrEmpty(waterLevelInput.text) || !string.IsNullOrWhiteSpace(waterLevelInput.text))
        {
            GameObject waterObject = heightMapObject.transform.Find("WaterBlock").gameObject;

            waterObject.transform.position = new Vector3(
                waterObject.transform.position.x, 
                float.Parse(waterLevelInput.text), 
                waterObject.transform.position.z);
        }
    }

    public void AdjustScalingFactor()
    {
        if (!string.IsNullOrEmpty(scalingFactorInput.text) || !string.IsNullOrWhiteSpace(scalingFactorInput.text))
        {
            GameObject meshObject = heightMapObject.transform.Find("MeshMap").gameObject;
            float scaleValue = float.Parse(scalingFactorInput.text);

            if (scaleValue <= 0)
            {
                // we only want positive scaling factors
                scaleValue = 1f;
                scalingFactorInput.text = string.Format("{0}", scaleValue);
            }

            scaleChangeEvent?.Invoke(scaleValue);

            UserSettings.verticalScalingFactor = scaleValue;
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

            TimeManager.instance.speedUpCoefficient = enteredValue;
        }
    }
}
