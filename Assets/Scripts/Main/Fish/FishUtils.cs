using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class FishUtils : MonoBehaviour 
{
    public GameObject canvas, depthLine, trail, thermoInd;
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Material deflt, blue, lBlue, green, purple, orange, red, pink, yellow, occluded;
    private BoxCollider boxCollider;
    private GameObject scaleDummy;
    private Fish fish;

    private TextMeshProUGUI textElement;
    public float baseExtent { get; private set; }

    public bool canvasActive {get { return canvas.activeSelf; }} 
    public bool depthLineActive {get { return depthLine.activeSelf; }} 
    public bool trailActive {get { return trail.activeSelf; }} 
    public bool thermoIndActive {get { return thermoInd.activeSelf; }} 
    private bool firstTagActivation = true; 
    public Color fishColor { get { return renderers[0].material.color; } } 

    private GameObject waterBlock;
    private LineRenderer line;

    public void Setup(int fishLayer, Fish fish)
    {
        this.fish = fish;
        this.scaleDummy = this.transform.Find("ScaleDummy").gameObject;
        this.boxCollider = this.scaleDummy.GetComponent<BoxCollider>();
        this.baseExtent = Mathf.Max(renderers[0].localBounds.extents.x, renderers[0].localBounds.extents.y, renderers[0].localBounds.extents.z);

        this.canvas.GetComponent<Canvas>().worldCamera = Camera.main.transform.Find("UICamera").GetComponent<Camera>();
        this.ChangeLayersRecursively(this.scaleDummy.transform, fishLayer);
    }

    private void ChangeLayersRecursively(Transform transform, int layer)
    {
        foreach (Transform child in transform)
        {
            child.gameObject.layer = layer;
            ChangeLayersRecursively(child, layer);
        }
    }

    public void UpdateFishScale(float newVal, float length)
    {
        float scalingFactor = length / 1000f / this.baseExtent * newVal;
        this.scaleDummy.transform.localScale = new Vector3(scalingFactor, scalingFactor, scalingFactor);

        // Extents represent only half the size, move the fish tag
        this.boxCollider.size = renderers[0].localBounds.extents * 2;
        RectTransform canvasRect = this.canvas.GetComponent<RectTransform>();
        canvasRect.localPosition = new Vector3(canvasRect.localPosition.x, renderers[0].bounds.size.y + 2f, canvasRect.localPosition.z);
    }
    public void SeeFishInList()
    {
        // Open the menu if it's hidden
        ButtonTriggerAnimation animator = GameObject.Find("MenuPanel").GetComponent<ButtonTriggerAnimation>();
        if (animator.menuClosed) animator.ToggleBool();
        
        // Open relevant tab
        TabController.instance.FishTab();

        // Navigate to relevant fish box
        StartCoroutine(FishList.instance.FocusBox(this.fish));
    }

    private void Awake()
    {
        textElement = this.canvas.transform.Find("Panel").transform.Find("Outline").transform.Find("Background").transform.Find("InfoText").
            GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        this.ActivateDepthLine(UserSettings.showFishDepthLines);
        this.ActivateTrail(UserSettings.showFishTrails);
        this.ActivateThermoBob(UserSettings.showThermocline);
        this.ActivateTag(UserSettings.showFishTags);
        this.occluded.color = green.color;
        this.line = depthLine.GetComponent<LineRenderer>();
        this.waterBlock = GameObject.Find("WaterBlock");
    }

    // Called via Event Trigger component on the ScaleDummy child object of the fish
    public void ColliderHit() { if (!CanvasRaycast.hoveringOverUI) this.ToggleTag(); }

    public void ActivateTag(bool activationStatus)
    { 
        canvas.SetActive(activationStatus); 
        if (activationStatus && this.firstTagActivation)
        {
            StartCoroutine(this.RebuildLayout()); 
            this.firstTagActivation = false;
        }
    }
    public void ActivateDepthLine(bool activationStatus) { depthLine.SetActive(activationStatus); }
    public void ActivateTrail(bool activationStatus) { trail.SetActive(activationStatus); }
    public void ActivateThermoBob(bool activationStatus) { thermoInd.SetActive(activationStatus); }

    public void ToggleTag() { canvas.SetActive(!canvas.activeSelf); LayoutRebuilder.ForceRebuildLayoutImmediate(this.canvas.GetComponent<RectTransform>()); }
    public void ToggleDepthLine() { depthLine.SetActive(!depthLine.activeSelf); }
    public void ToggleTrail() { trail.SetActive(!trail.activeSelf); }
    public void ToggleThermoBob() { thermoInd.SetActive(!thermoInd.activeSelf); }

    public void ClearTrail() { this.trail.GetComponent<TrailRenderer>().Clear(); }

    public void UpdateDepthIndicatorLine(Vector3 LinePoint)
    {
        LinePoint.y = - Mathf.Abs(LocalMeshData.minDepth) * UserSettings.verticalScalingFactor;
        this.line.SetPosition(0, LinePoint);

        LinePoint.y = this.waterBlock.transform.position.y;
        this.line.SetPosition(1, LinePoint);
        
        if (TableProofings.tables[TableProofings.checkTables[7]].imported)
        {
            if (ThermoclineDOMain.instance.currentThermoDepth != null)
            {
                LinePoint.y = ((float)-ThermoclineDOMain.instance.currentThermoDepth + UserSettings.waterLevel) * UserSettings.verticalScalingFactor;
                thermoInd.transform.position = LinePoint;
            }
        }

        // Check if the thermobob should be spawned
        if (this.depthLine.activeSelf && UserSettings.showThermocline) this.thermoInd.SetActive(true);
        else this.thermoInd.SetActive(false);
    }

    public void setNewText(string val) { textElement.text = val; }

    private IEnumerator RebuildLayout()
    {
        for (int i = 0; i < 6; i++) 
        { 
            LayoutRebuilder.ForceRebuildLayoutImmediate(this.canvas.GetComponent<RectTransform>());
            yield return new WaitForSeconds(0.5f); 
        }
    }

    // Color-handling
    public void ResetColor() 
    { 
        for (int i = 0; i < renderers.Length; i++) { renderers[i].material = deflt; }

        // Fish trail color
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(green.color, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(0f, 1.0f) }
        );
        trail.GetComponent<TrailRenderer>().colorGradient = gradient;

        // Fish depth line color
        gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(green.color, 0.0f), new GradientColorKey(green.color, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(1f, 1.0f) }
        );
        depthLine.GetComponent<LineRenderer>().colorGradient = gradient;
    }

    public void SetFishColor(string color)
    {
        bool defaultColor = false;

        // Actual fish color
        if (color == "blue") { for (int i = 0; i < renderers.Length; i++) { renderers[i].material = blue; };  }
        else if (color == "lBlue") { for (int i = 0; i < renderers.Length; i++) { renderers[i].material = lBlue; } }
        else if (color == "green") { for (int i = 0; i < renderers.Length; i++) { renderers[i].material = green; } }
        else if (color == "purple") { for (int i = 0; i < renderers.Length; i++) { renderers[i].material = purple; } }
        else if (color == "orange") { for (int i = 0; i < renderers.Length; i++) { renderers[i].material = orange; } }
        else if (color == "pink") { for (int i = 0; i < renderers.Length; i++) { renderers[i].material = pink; } }
        else if (color == "red") { for (int i = 0; i < renderers.Length; i++) { renderers[i].material = red; } }
        else if (color == "yellow") { for (int i = 0; i < renderers.Length; i++) { renderers[i].material = yellow; } }
        else { ResetColor(); defaultColor = true; };

        Color trailColor = defaultColor ? green.color : this.fishColor;

        // Fish trail color
        Gradient gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(trailColor, 0.0f), new GradientColorKey(Color.white, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(0f, 1.0f) }
        );
        trail.GetComponent<TrailRenderer>().colorGradient = gradient;

        // Fish depth line color
        gradient = new Gradient();
        gradient.SetKeys(
            new GradientColorKey[] { new GradientColorKey(trailColor, 0.0f), new GradientColorKey(trailColor, 1.0f) },
            new GradientAlphaKey[] { new GradientAlphaKey(1f, 0.0f), new GradientAlphaKey(1f, 1.0f) }
        );
        depthLine.GetComponent<LineRenderer>().colorGradient = gradient;
    }
}
