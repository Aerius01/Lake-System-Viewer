using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;

public class FishBox : ListBox
{
    public Fish fish {get; private set;}
    public float contentSize { get { return 266f; } }
    public SpeciesBox parentBox { get; private set; }

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

    [SerializeField] private GameObject contentWindow;


    // ----------    METHODS    -------------
    public void SetUpBox(Fish fish)
    {
        this.fish = fish;
        this.rect = this.GetComponent<RectTransform>();
        this.parentRect = this.transform.parent.GetComponent<RectTransform>();
        this.parentBox = this.transform.parent.transform.parent.transform.parent.transform.parent.GetComponent<SpeciesBox>();
        this.colorHandler = this.GetComponent<FishListColoringButton>();
        this.colorHandler.DefineParameters(fish);

        gameText = this.contentWindow.transform.Find("FishDetails").GetComponent<TextMeshProUGUI>();
        this.headerText = this.transform.Find("Canvas").transform.Find("Header").transform.Find("FishID").GetComponent<TextMeshProUGUI>();
        headerText.text = string.Format("{0}", fish.id);

        // This information never changes
        initText = string.Format("Sex: {0}\nSpecies: {1}\nWeight: {2}g\nSize: {3}mm\nDepth: ",
            fish.male == null ? "?" : (fish.male == true ? "M" : "F"), 
            string.IsNullOrEmpty(fish.speciesName) ? "?" : fish.speciesName,
            fish.weight == null ? "?" : ((int)fish.weight).ToString(),
            fish.length == null ? "?" : ((int)fish.length).ToString());

        gameText.text = string.Format("{0}{1:0.00}m", initText, fish.currentDepth);

        this.contentWindow.SetActive(false);
    }

    public void OpenCloseBox()
    {
        // Only register the toggle if the box isn't currently mid-animation
        if (this.opening == null)
        {
            if (this.open) this.opening = false;
            else this.opening = true;

            StartCoroutine(AnimateChange(40f, this.contentSize, this.contentWindow));
        }
    }

    protected override IEnumerator AnimateChange(float headerSize, float contentSize, GameObject contentWindow)
    {       
        // if currently closed, activate the canvases before opening
        if (!this.open) contentWindow.SetActive(true); 

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

        // if currently closed now, then deactivate the canvases
        if (!this.open) contentWindow.SetActive(false); 

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

    public void SetIndividualColor(string color)
    {
        RemoveIndividualColor(); // remove any existing coloring
        this.colorHandler.SetColor(color);
    }

    public void RemoveIndividualColor() { if (this.colorApplied) this.colorHandler.ButtonPress(); }

    public void UpdateText(bool active=true)
    {
        if (active) { gameText.text = string.Format("{0}{1:0.00}m", initText, this.fish.currentDepth); }
        else { gameText.text = string.Format("{0}{1}", initText, "-"); }
    }

    public void Greyout()
    {
        if (!colorHandler.disabled)
        {
            colorHandler.DisableButton();
            greyedOut = true;
            headerText.text = this.fish.id + " (inactive)";
            ToggleInteractable(false);
        }
    }

    public void RestoreColor()
    {
        if (colorHandler.disabled)
        {
            colorHandler.EnableButton();
            greyedOut = false;
            headerText.text = string.Format("{0}", this.fish.id);
            ToggleInteractable(true);
        }
    }

    public void FlashHeader() { this.colorHandler.FlashHeader(); }

    public void ActivateTag() { this.fish.ActivateTag(this.tagToggle.isOn, TimeManager.instance.currentTime); }
    public void ActivateDepthLine() { this.fish.ActivateDepthLine(this.depthToggle.isOn, TimeManager.instance.currentTime); }
    public void ActivateTrail() { this.fish.ActivateTrail(this.trailToggle.isOn, TimeManager.instance.currentTime); }

    public void ToggleTag(bool active) { if (fish.FishShouldExist(TimeManager.instance.currentTime)) this.tagToggle.isOn = active; }
    public void ToggleDepthLine(bool active) { if (fish.FishShouldExist(TimeManager.instance.currentTime)) this.depthToggle.isOn = active; }
    public void ToggleTrail(bool active) { if (fish.FishShouldExist(TimeManager.instance.currentTime)) this.trailToggle.isOn = active; }
}