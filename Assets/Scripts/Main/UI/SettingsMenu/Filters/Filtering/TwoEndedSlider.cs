using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class TwoEndedSlider : MonoBehaviour 
{
    [SerializeField] private Toggle invertToggle;
    [SerializeField] private RectTransform lowerBar, middleBar, upperBar;
    [SerializeField] private Slider minSlider, maxSlider;
    [SerializeField] private RectTransform minHandle, maxHandle;
    [SerializeField] private TMP_InputField minInput, maxInput;

    private bool isMinNode;
    private float minValue = 0f, maxValue = 1f;

    // Exposed
    public float currentMin { get { return this.minSlider.normalizedValue * (this.maxValue - this.minValue) + this.minValue; } }
    public float currentMax { get { return this.maxSlider.normalizedValue * (this.maxValue - this.minValue) + this.minValue; } }
    public bool inverted { get { return this.invertToggle.isOn; } }


    private void Start() { UpdateMaskBars(); }

    public void SetRange(float min, float max)
    {
        this.minValue = min;
        this.maxValue = max;
        UpdateTextValues();
    }

    public void ValueControl()
    {
        // Make sure the handles cannot cross each other
        if (isMinNode)
        {
            if (maxSlider.normalizedValue < minSlider.normalizedValue)
            { maxSlider.normalizedValue = minSlider.normalizedValue; }
        }
        else
        {
            if (minSlider.normalizedValue > maxSlider.normalizedValue)
            { minSlider.normalizedValue = maxSlider.normalizedValue; }
        }

        UpdateTextValues();
        UpdateMaskBars();
    }

    public void UpdateTextValues()
    {
        minInput.text = string.Format("{0:###}", currentMin);
        maxInput.text = string.Format("{0:###}", currentMax);
    }

    public void UpdateMaskBars()
    {
        lowerBar.sizeDelta += new Vector2( minHandle.position.x - lowerBar.position.x - lowerBar.rect.width, 0f);
        middleBar.position = minHandle.position;
        middleBar.sizeDelta += new Vector2( (maxHandle.position.x - minHandle.position.x) - middleBar.rect.width, 0f);
        upperBar.sizeDelta += new Vector2( upperBar.position.x - maxHandle.position.x - upperBar.rect.width, 0f);

        MaskBarActivations();
    }

    public void MaskBarActivations()
    {
        lowerBar.gameObject.SetActive(!this.invertToggle.isOn);
        middleBar.gameObject.SetActive(this.invertToggle.isOn);
        upperBar.gameObject.SetActive(!this.invertToggle.isOn);
    }

    public void UpdateSliderFromInputMin() { minSlider.normalizedValue = (float.Parse(minInput.text) - this.minValue) / (this.maxValue - this.minValue); UpdateMaskBars(); }
    public void UpdateSliderFromInputMax() { maxSlider.normalizedValue = (float.Parse(maxInput.text) - this.minValue) / (this.maxValue - this.minValue); UpdateMaskBars(); }

    public void UpdateSelectedNode(bool min)
    {
        if (min) isMinNode = true;
        else isMinNode = false;
    }
}