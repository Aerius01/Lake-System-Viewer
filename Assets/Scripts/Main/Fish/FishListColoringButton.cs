using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class FishListColoringButton : MonoBehaviour
{
    private GameObject fishObject;
    private FishHighlighter highlighter;
    private GameObject applyButton, removeButton;
    private int fishID;

    private void Start()
    {
        fishID = int.Parse(this.gameObject.transform.Find("Header").transform.Find("FishID").GetComponent<TextMeshProUGUI>().text);
        fishObject = FishGeneratorNew.GetFishObject(fishID);
        highlighter = fishObject.GetComponent<FishHighlighter>();

        applyButton = this.gameObject.transform.Find("Content").transform.Find("ColorApplyButton").gameObject;
        removeButton = this.gameObject.transform.Find("Content").transform.Find("ColorRemoveButton").gameObject;
        removeButton.SetActive(false);
    }

    // each fish to have its own FCP to reduce complexity

    // public void SetColor()
    // {
    //     FlexibleColorPickerUtils.instance.gameObject.SetActive(true);
    //     FlexibleColorPickerUtils.instance.applyButton = applyButton;
    //     FlexibleColorPickerUtils.instance.removeButton = removeButton;

    //     applyButton.GetComponent<Button>().interactable = false;
    // }

    // public void CloseFCP()
    // {
    //     FlexibleColorPickerUtils.instance.gameObject.SetActive(false);
    //     applyButton.GetComponent<Button>().interactable = true;
    // }

    // public void ColorSelected()
    // {
    //     highlighter.SetColor(FlexibleColorPickerUtils.instance.gameObject.GetComponent<FlexibleColorPicker>().color);
    //     FlexibleColorPickerUtils.instance.gameObject.SetActive(false);
    //     applyButton.GetComponent<Button>().interactable = true;
    //     applyButton.SetActive(false);
    //     removeButton.SetActive(true);
    // }

    // public void RemoveColor()
    // {
    //     highlighter.ResetColor();
    //     removeButton.SetActive(false);
    //     applyButton.SetActive(true);
    // }
}
