using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;
using System;

public class FishListColoringButton : MonoBehaviour
{
    // Inputs
    [SerializeField] private GameObject colorButton;
    [SerializeField] private ButtonClickHandler clickHandler;

    // For button disabling
    public bool colorApplied {get; private set;} = false;
    public bool disabled {get; private set;} = false;
    private Button trackingButton;

    // Core functionality
    private TextMeshProUGUI colorButtonText;
    private Image headerImage;
    private Fish fish;
    private Color trackingColor;
    private string stringColor;
    private Coroutine flashing;

    public void DefineParameters(Fish fish)
    {
        this.fish = fish;
        colorButtonText = colorButton.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();

        headerImage = this.gameObject.transform.Find("Header").GetComponent<Image>();
        trackingButton = colorButton.GetComponent<Button>();
    }

    public void ButtonPress()
    {
        if (!colorApplied)
        {
            // Open the CP
            ColorPicker.ShowMenu(true);
            ColorPicker.colorAcceptedEvent += this.SetColor;
        }
        else
        {
            // if the color-changing coroutine is running, stop it first.
            if (this.flashing != null)
            {
                StopCoroutine(this.flashing);
                this.flashing = null;
            }

            // Remove the color
            this.fish.ResetFishColor();
            colorButtonText.text = "Apply Tracking Color";
            colorApplied = false;
            ResetHeaderColor();
        }
    }

    public void SetColor(string color)
    {
        // if the color-changing coroutine is running, stop it first.
        if (this.flashing != null)
        {
            StopCoroutine(this.flashing);
            this.flashing = null;
        }

        this.stringColor = color;
        this.fish.SetFishColor(this.stringColor);
        this.trackingColor = this.fish.color;

        this.colorApplied = true;
        this.colorButtonText.text = "Remove Color";

        this.trackingColor.a = 100f/255f;
        this.headerImage.color = this.trackingColor;

        // Unsubscribe event listener
        ColorPicker.colorAcceptedEvent -= this.SetColor;
    }

    public void ResetHeaderColor()
    {
        if (colorApplied) { SetColor(this.stringColor); }
        else { headerImage.color = ColorPicker.standardHeaderColor; }
    }

    public void DisableButton()
    {
        disabled = true;
        colorButton.GetComponent<Image>().color = headerImage.color = ColorPicker.disabledColor;
        trackingButton.interactable = false;
        if (clickHandler != null) clickHandler.DisableZoom(true);
    }

    public void EnableButton()
    {
        disabled = false;
        ResetHeaderColor();
        colorButton.GetComponent<Image>().color = ColorPicker.standardButtonColor;
        trackingButton.interactable = true;
        if (clickHandler != null) clickHandler.DisableZoom(false);
    }

    public void FlashHeader() { this.flashing = StartCoroutine(this.FlashCoroutine()) as Coroutine; }

    private float ExpF(float x) { return (float)(Math.Pow(Math.E, x) - 1); }
    private float ExpFNorm(float x) { return (ExpF(x) - ExpF(0f)) / (ExpF(1f) - ExpF(0f)); }
    private IEnumerator FlashCoroutine()
    {
        Color currentColor = this.headerImage.color;
        float slowRamp = 0.3f;
        float fastRamp = 0.1f;

        float rDist = 1f - currentColor.r;
        float gDist = 1f - currentColor.g;
        float bDist = 1f - currentColor.b;
        float aDist = 1f - currentColor.a;

        // One slow ramp up
        int numberOfSlowUpdates = Mathf.RoundToInt(slowRamp / 0.05f);
        for (int i = 0; i <= numberOfSlowUpdates; i++)
        {
            float r = currentColor.r + this.ExpFNorm((float)i / (float)numberOfSlowUpdates) * rDist;
            float g = currentColor.g + this.ExpFNorm((float)i / (float)numberOfSlowUpdates) * gDist;
            float b = currentColor.b + this.ExpFNorm((float)i / (float)numberOfSlowUpdates) * bDist;
            float a = currentColor.a + this.ExpFNorm((float)i / (float)numberOfSlowUpdates) * aDist;

            Color newColor = new Color(r > 1f ? 1f : r, g > 1f ? 1f : g, b > 1f ? 1f : b, a > 1f ? 1f : a);
            this.headerImage.color = newColor;
            yield return new WaitForSeconds(0.05f);
        }

        // One slow ramp down
        for (int i = numberOfSlowUpdates; i >= 0; i--)
        {
            float r = currentColor.r + this.ExpFNorm((float)i / (float)numberOfSlowUpdates) * rDist;
            float g = currentColor.g + this.ExpFNorm((float)i / (float)numberOfSlowUpdates) * gDist;
            float b = currentColor.b + this.ExpFNorm((float)i / (float)numberOfSlowUpdates) * bDist;
            float a = currentColor.a + this.ExpFNorm((float)i / (float)numberOfSlowUpdates) * aDist;

            Color newColor = new Color(r > 1f ? 1f : r, g > 1f ? 1f : g, b > 1f ? 1f : b, a > 1f ? 1f : a);
            this.headerImage.color = newColor;
            yield return new WaitForSeconds(0.05f);
        }

        // Two fast ramp ups/down
        for (int j = 0; j < 2; j++)
        {
            int numberOfFastUpdates = Mathf.RoundToInt(fastRamp / 0.05f);
            for (int i = 0; i <= numberOfFastUpdates; i++)
            {
                float r = currentColor.r + this.ExpFNorm((float)i / (float)numberOfFastUpdates) * rDist;
                float g = currentColor.g + this.ExpFNorm((float)i / (float)numberOfFastUpdates) * gDist;
                float b = currentColor.b + this.ExpFNorm((float)i / (float)numberOfFastUpdates) * bDist;
                float a = currentColor.a + this.ExpFNorm((float)i / (float)numberOfSlowUpdates) * aDist;

                Color newColor = new Color(r > 1f ? 1f : r, g > 1f ? 1f : g, b > 1f ? 1f : b, a > 1f ? 1f : a);
                this.headerImage.color = newColor;
                yield return new WaitForSeconds(0.05f);
            }

            for (int i = numberOfFastUpdates; i >= 0; i--)
            {
                float r = currentColor.r + this.ExpFNorm((float)i / (float)numberOfFastUpdates) * rDist;
                float g = currentColor.g + this.ExpFNorm((float)i / (float)numberOfFastUpdates) * gDist;
                float b = currentColor.b + this.ExpFNorm((float)i / (float)numberOfFastUpdates) * bDist;
                float a = currentColor.a + this.ExpFNorm((float)i / (float)numberOfSlowUpdates) * aDist;

                Color newColor = new Color(r > 1f ? 1f : r, g > 1f ? 1f : g, b > 1f ? 1f : b, a > 1f ? 1f : a);
                this.headerImage.color = newColor;
                yield return new WaitForSeconds(0.05f);
            }
        }
        
        this.headerImage.color = currentColor;
        this.flashing = null;
    }
}
