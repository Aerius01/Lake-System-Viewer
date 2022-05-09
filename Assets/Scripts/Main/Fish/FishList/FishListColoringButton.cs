using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FishListColoringButton : MonoBehaviour
{
    // Inputs
    [SerializeField]
    private GameObject colorButton;
    [SerializeField]
    private ButtonClickHandler clickHandler;

    // For button disabling
    public bool colorApplied {get; private set;} = false;
    public bool disabled {get; private set;} = false;
    private Button trackingButton;

    // Core functionality
    private TextMeshProUGUI colorButtonText;
    private Image headerImage;
    private int fishID;
    private Color trackingColor;

    public void DefineParameters(int id)
    {
        fishID = id;
        colorButtonText = colorButton.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();

        headerImage = this.gameObject.transform.Find("Header").GetComponent<Image>();
        trackingButton = colorButton.GetComponent<Button>();
    }

    public void ButtonPress()
    {
        if (!colorApplied)
        {
            // Open the FCP
            FlexibleColorPickerUtils.SetNewColor();
            FlexibleColorPickerUtils.colorAcceptedEvent += this.SetColor;
        }
        else
        {
            // Remove the color
            FishManager.ResetFishColor(this.fishID);
            colorButtonText.text = "Apply Tracking Color";
            colorApplied = false;
            ResetHeaderColor();
        }
    }

    public void SetColor(Color color)
    {
        this.trackingColor = color;

        FishManager.SetFishColor(fishID, this.trackingColor);
        this.colorApplied = true;
        this.colorButtonText.text = "Remove Color";

        this.trackingColor.a = 100f/255f;
        this.headerImage.color = this.trackingColor;

        // Unsubscribe event listener
        FlexibleColorPickerUtils.colorAcceptedEvent -= this.SetColor;
    }

    public void ResetHeaderColor()
    {
        if (colorApplied) { SetColor(trackingColor); }
        else { headerImage.color = FlexibleColorPickerUtils.standardHeaderColor; }
    }

    public void DisableButton()
    {
        disabled = true;
        colorButton.GetComponent<Image>().color = headerImage.color = FlexibleColorPickerUtils.disabledColor;
        trackingButton.interactable = false;
        if (clickHandler != null) clickHandler.DisableZoom(true);
    }

    public void EnableButton()
    {
        disabled = false;
        ResetHeaderColor();
        colorButton.GetComponent<Image>().color = FlexibleColorPickerUtils.standardButtonColor;
        trackingButton.interactable = true;
        if (clickHandler != null) clickHandler.DisableZoom(false);
    }
}
