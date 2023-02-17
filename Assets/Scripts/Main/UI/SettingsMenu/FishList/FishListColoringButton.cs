using UnityEngine;
using UnityEngine.UI;
using TMPro;

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
            // Remove the color
            this.fish.ResetFishColor();
            colorButtonText.text = "Apply Tracking Color";
            colorApplied = false;
            ResetHeaderColor();
        }
    }

    public void SetColor(string color)
    {
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
}
