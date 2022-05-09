using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class FishBox : ListBox
{
    public Fish fish {get; private set;}
    public float contentSize { get { return 215f; } }
    private SpeciesBox parentBox;

    // Text updating
    private string initText;
    private TextMeshProUGUI gameText;
    public bool greyedOut { get; private set;} = false;

    // For coloring
    private FishListColoringButton colorHandler;
    public bool colorApplied { get { return this.colorHandler.colorApplied; } }

    // For toggles
    public bool tagStatus { get { return this.tagToggle.isOn; } }
    public bool depthStatus { get { return this.depthToggle.isOn; } }
    public bool trailStatus { get { return this.trailToggle.isOn; } }

    // ----------    METHODS    -------------
    public void SetUpBox(Fish fish)
    {
        this.fish = fish;
        this.rect = this.GetComponent<RectTransform>();
        this.parentRect = this.transform.parent.GetComponent<RectTransform>();
        this.parentBox = this.transform.parent.transform.parent.transform.parent.GetComponent<SpeciesBox>();
        this.colorHandler = this.GetComponent<FishListColoringButton>();
        this.colorHandler.DefineParameters(this.fish.id);

        gameText = this.transform.Find("Content").transform.Find("FishDetails").GetComponent<TextMeshProUGUI>();
        this.headerText = this.transform.Find("Header").transform.Find("FishID").GetComponent<TextMeshProUGUI>();
        headerText.text = string.Format("{0}", fish.id);

        (bool? male, string speciesName, int? weight, int? length, float currentDepth, bool fishActive) = FishManager.GetFishStats(this.fish.id);

        // This information never changes
        initText = string.Format("Sex: {0}\nSpecies: {1}\nWeight: {2}g\nSize: {3}mm\nDepth: ",
            male == null ? "?" : (male == true ? "M" : "F"), 
            string.IsNullOrEmpty(speciesName) ? "?" : speciesName,
            weight == null ? "?" : ((int)weight).ToString(),
            length == null ? "?" : ((int)length).ToString());

        gameText.text = string.Format("{0}{1:0.00}m", initText, currentDepth);
    }

    public void OpenCloseBox()
    {
        // Only register the toggle if the box isn't currently mid-animation
        if (this.opening == null)
        {
            if (this.open) this.opening = false;
            else this.opening = true;

            StartCoroutine(AnimateChange(30f, this.contentSize));
        }
    }

    protected override IEnumerator AnimateChange(float headerSize, float contentSize)
    {       
        // if currently open, the position differential is negative
        float diff = this.open ? headerSize - contentSize : contentSize - headerSize;
        float rotDiff = this.open ? 90f : -90f;

        // rotation of arrowhead occurs in the first 1/3 of the animation, always
        float targetTime = 0.1f;
        float totalIncrements = 60f * targetTime; // 60fps
        float rotIncrements = Mathf.RoundToInt(totalIncrements / 3);
        float period = targetTime / totalIncrements;
        
        float increment = diff / totalIncrements;
        float rotIncr = rotDiff / rotIncrements;

        for (float i = 0; i < totalIncrements; i ++)
        {
            if (i < rotIncrements) arrowImage.transform.localEulerAngles += new Vector3(0f, 0f, rotIncr);
            this.rect.sizeDelta += new Vector2(0, increment);
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            parentBox.ExpandMenu(increment);
            yield return new WaitForSeconds(period);
        }

        if (this.opening == true) this.open = true;
        else this.open = false;
        this.opening = null;
    }

    public void Activate(bool state)
    {
        if (state) this.activeToggle.isOn = true;
        else this.activeToggle.isOn = false;
        ToggleActivate();
    }

    public void ToggleActivate()
    {
        if (this.activeToggle.isOn) { this.fish.spawnOverride = false; this.fish.Activate(); }
        else { this.fish.spawnOverride = true; this.fish.Deactivate(); }
    }

    public void SetIndividualColor(Color color)
    {
        RemoveIndividualColor(); // remove any existing coloring
        this.colorHandler.SetColor(color);
    }

    public void RemoveIndividualColor() { if (this.colorApplied) this.colorHandler.ButtonPress(); }

    public void UpdateText(bool active=true)
    {
        if (active)
        {
            (bool? male, string speciesName, int? weight, int? length, float currentDepth, bool fishActive) = FishManager.GetFishStats(this.fish.id);
            gameText.text = string.Format("{0}{1:0.00}m", initText, currentDepth);
        }
        else { gameText.text = string.Format("{0}{1}", initText, "-"); }
    }

    public void Greyout()
    {
        if (!colorHandler.disabled)
        {
            colorHandler.DisableButton();
            greyedOut = true;
            headerText.text = this.fish.id + " (inactive)";
        }
    }

    public void RestoreColor()
    {
        if (colorHandler.disabled)
        {
            colorHandler.EnableButton();
            greyedOut = false;
            headerText.text = string.Format("{0}", this.fish.id);
        }
    }

    public void SetRank(int rank) { this.rank = rank; }

    public void ActivateTag() { this.fish.ActivateUtil("tag", this.tagToggle.isOn); }
    public void ActivateDepthLine() { this.fish.ActivateUtil("line", this.depthToggle.isOn); }
    public void ActivateTrail() { this.fish.ActivateUtil("trail", this.trailToggle.isOn); }

    public void ToggleTag(bool active) { this.tagToggle.isOn = active; }
    public void ToggleDepthLine(bool active) { this.depthToggle.isOn = active; }
    public void ToggleTrail(bool active) { this.trailToggle.isOn = active; }
}