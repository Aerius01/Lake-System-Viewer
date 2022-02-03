using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FishListColoringButton : MonoBehaviour
{
    private TextMeshProUGUI colorButtonText;
    private Image headerImage;
    public GameObject colorButton;
    private int fishID;
    private bool colorApplied = false;

    private Color _color, standardHeaderColor;
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

    private void Start()
    {
        fishID = int.Parse(this.gameObject.transform.Find("Header").transform.Find("FishID").GetComponent<TextMeshProUGUI>().text);
        colorButtonText = colorButton.transform.Find("Text (TMP)").GetComponent<TextMeshProUGUI>();
        standardHeaderColor = new Color(28f/255f, 27f/255f, 55f/255f, 100f/255f);
        headerImage = this.gameObject.transform.Find("Header").GetComponent<Image>();
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
            headerImage.color = standardHeaderColor;
        }
    }

    public void SetNewColor(Color color)
    {
        this.color = color;

        color.a = 100f/255f;
        headerImage.color = color;
    }
}
