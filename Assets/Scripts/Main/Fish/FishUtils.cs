using UnityEngine;
using TMPro;

public class FishUtils : MonoBehaviour
{
    public GameObject canvas, depthLine, trail, thermoInd;
    [SerializeField] private Renderer[] renderers;
    [SerializeField] private Material deflt, blue, lBlue, green, purple, orange, red, pink, yellow;

    private TextMeshProUGUI textElement;

    public float maxExtent { get { return Mathf.Max(renderers[0].bounds.extents.x, renderers[0].bounds.extents.y, renderers[0].bounds.extents.z); } }
    public Vector3 extents { get { return renderers[0].bounds.extents; } }
    public bool canvasActive {get { return canvas.activeSelf; }} 
    public bool depthLineActive {get { return depthLine.activeSelf; }} 
    public bool trailActive {get { return trail.activeSelf; }} 
    public bool thermoIndActive {get { return thermoInd.activeSelf; }} 
    public Color fishColor { get { return renderers[0].material.color; } } 

    private void Awake()
    {
        textElement = this.canvas.transform.Find("Panel").transform.Find("Outline").transform.Find("Background").transform.Find("InfoText").
            GetComponent<TextMeshProUGUI>();
    }

    private void Start()
    {
        if (UserSettings.showFishTags) ActivateTag(true);
        if (UserSettings.showFishDepthLines) ActivateDepthLine(true);
        if (UserSettings.showFishTrails) ActivateTrail(true);
        if (UserSettings.showThermocline) ActivateThermoBob(true);
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            BoxCollider collider = this.transform.Find("ScaleDummy").GetComponent<BoxCollider>();
             
            if (collider.Raycast(ray, out hit, 999999f))
            {
                this.ToggleTag();
            }
        }

        // Check if the thermobob should be spawned
        if (this.depthLine.activeSelf && UserSettings.showThermocline) this.thermoInd.SetActive(true);
        else this.thermoInd.SetActive(false);
    }

    public void ActivateTag(bool activationStatus) { canvas.SetActive(activationStatus); }
    public void ActivateDepthLine(bool activationStatus) { depthLine.SetActive(activationStatus); }
    public void ActivateTrail(bool activationStatus) { trail.SetActive(activationStatus); }
    public void ActivateThermoBob(bool activationStatus) { thermoInd.SetActive(activationStatus); Debug.Log(activationStatus); }

    public void ToggleTag() { canvas.SetActive(!canvas.activeSelf); }
    public void ToggleDepthLine() { depthLine.SetActive(!depthLine.activeSelf); }
    public void ToggleTrail() { trail.SetActive(!trail.activeSelf); }
    public void ToggleThermoBob() { thermoInd.SetActive(!thermoInd.activeSelf); }

    public void ClearTrail() { this.trail.GetComponent<TrailRenderer>().Clear(); }

    public void UpdateDepthIndicatorLine(Vector3 LinePoint)
    {
        // TODO: find the lake depth at the position of the fish
        LineRenderer line = depthLine.GetComponent<LineRenderer>();
        GameObject waterblock = GameObject.Find("WaterBlock");
        
        LinePoint.y = - Mathf.Abs(LocalMeshData.minDepth) * UserSettings.verticalScalingFactor;
        line.SetPosition(0, LinePoint);

        LinePoint.y = waterblock.transform.position.y;
        line.SetPosition(1, LinePoint);

        if (ThermoclineDOMain.currentThermoDepth != null)
        {
            LinePoint.y = (float)-ThermoclineDOMain.currentThermoDepth * UserSettings.verticalScalingFactor;
            thermoInd.transform.position = LinePoint;
        }
    }

    public void setNewText(string val) { textElement.text = val; }

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
