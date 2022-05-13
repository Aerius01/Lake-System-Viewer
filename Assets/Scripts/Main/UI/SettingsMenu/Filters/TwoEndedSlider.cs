using UnityEngine;
using UnityEngine.UI;

public class TwoEndedSlider : MonoBehaviour 
{
    [SerializeField] private bool minSlider;
    [SerializeField] private Slider complementarySlider;
    private Slider slider;

    private void Awake()
    {
        this.slider = this.GetComponent<Slider>();
    }

    public void ValueControl()
    {
        // Make sure the handles cannot cross each other
        if (minSlider)
        {
            if (slider.normalizedValue > complementarySlider.normalizedValue)
            { slider.normalizedValue = complementarySlider.normalizedValue; }
        }
        else
        {
            if (slider.normalizedValue < complementarySlider.normalizedValue)
            { slider.normalizedValue = complementarySlider.normalizedValue; }
        }
    }

}