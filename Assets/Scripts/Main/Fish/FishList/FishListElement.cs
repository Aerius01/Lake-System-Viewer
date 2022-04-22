using TMPro;
using UnityEngine;

public class FishListElement
{
    private string initText;
    public bool fishActive
    {
        get
        {
            (bool? male, string speciesName, int? weight, int? length, float currentDepth, bool fishActive) = FishManager.GetFishStats(this.id);
            return fishActive;
        }
        private set { ;}
    }
    public bool greyedOut { get; private set;} = false;
    public int id { get; private set;}
    private TextMeshProUGUI gameText, dropdownHeader;
    private FishListColoringButton colorHandler;
    private GameObject elementObject;

    public FishListElement(int id, GameObject obj, FishListColoringButton colorHandler)
    {
        this.id = id;
        this.elementObject = obj;
        this.colorHandler = colorHandler;
        this.colorHandler.DefineParameters(this.id);

        gameText = elementObject.transform.Find("Content").transform.Find("FishDetails").GetComponent<TextMeshProUGUI>();
        dropdownHeader = elementObject.transform.Find("Header").transform.Find("FishID").GetComponent<TextMeshProUGUI>();

        (bool? male, string speciesName, int? weight, int? length, float currentDepth, bool fishActive) = FishManager.GetFishStats(id);

        // This information never changes
        initText = string.Format("Sex: {0}\nSpecies: {1}\nWeight: {2}g\nSize: {3}mm\nDepth: ",
            male == null ? "?" : (male == true ? "M" : "F"), 
            string.IsNullOrEmpty(speciesName) ? "?" : speciesName,
            weight == null ? "?" : ((int)weight).ToString(),
            length == null ? "?" : ((int)length).ToString());

        gameText.text = string.Format("{0}{1:0.00}m", initText, currentDepth);
    }

    public void UpdateText(bool active=true)
    {
        if (active)
        {
            (bool? male, string speciesName, int? weight, int? length, float currentDepth, bool fishActive) = FishManager.GetFishStats(id);
            gameText.text = string.Format("{0}{1:0.00}m", initText, currentDepth);
        }
        else
        {
            gameText.text = string.Format("{0}{1}", initText, "-");
        }
    }

    public void Greyout()
    {
        if (!colorHandler.disabled)
        {
            colorHandler.DisableButton();
            greyedOut = true;
            dropdownHeader.text = this.id + " (inactive)";
        }
    }

    public void RestoreColor()
    {
        if (colorHandler.disabled)
        {
            colorHandler.EnableButton();
            greyedOut = false;
            dropdownHeader.text = string.Format("{0}", this.id);
        }
    }
}