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
    private Color _color, standardHeaderColor, standardButtonColor, disabledColor;
    public Color color
    {
        get {return _color;}
        set 
        {
            _color = value;
            FishManager.SetFishColor(fishID, value);
            colorApplied = true;
            colorButtonText.text = "Remove Color";
        }
    }

    public void DefineParameters(int id)
    {
        fishID = id;
        colorButtonText = colorButton.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
        standardHeaderColor = new Color(28f/255f, 27f/255f, 55f/255f, 100f/255f);
        standardButtonColor = new Color(28f/255f, 27f/255f, 55f/255f, 192f/255f);
        disabledColor = new Color(1f, 1f, 1f, 100f/255f);
        headerImage = this.gameObject.transform.Find("Header").GetComponent<Image>();
        trackingButton = colorButton.GetComponent<Button>();
    }

    public void ButtonPress()
    {
        if (!colorApplied)
        {
            // Open the FCP with details on the requester
            FlexibleColorPickerUtils.instance.SetNewTarget(this);
        }
        else
        {
            // Remove the color
            FishManager.ResetFishColor(fishID);
            colorButtonText.text = "Apply Tracking Color";
            colorApplied = false;
            ResetHeaderColor();
        }
    }

    public void SetNewHeaderColor(Color color)
    {
        this.color = color;
        color.a = 100f/255f;
        headerImage.color = color;
    }

    public void ResetHeaderColor()
    {
        if (colorApplied) { SetNewHeaderColor(color); }
        else { headerImage.color = standardHeaderColor; }
    }

    public void DisableButton()
    {
        disabled = true;
        colorButton.GetComponent<Image>().color = headerImage.color = disabledColor;
        trackingButton.interactable = false;
        clickHandler.DisableZoom(true);
    }

    public void EnableButton()
    {
        disabled = false;
        ResetHeaderColor();
        colorButton.GetComponent<Image>().color = standardButtonColor;
        trackingButton.interactable = true;
        clickHandler.DisableZoom(false);
    }
}
