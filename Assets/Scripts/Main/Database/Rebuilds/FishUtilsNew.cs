using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class FishUtilsNew : MonoBehaviour
{
    public GameObject canvas, depthLine, trail, thermoInd;
    [SerializeField] private SkinnedMeshRenderer skinRenderer;
    private TextMeshProUGUI textElement;

    public float maxExtent { get { return Mathf.Max(skinRenderer.bounds.extents.x, skinRenderer.bounds.extents.y, skinRenderer.bounds.extents.z); } }
    public Vector3 extents { get { return skinRenderer.bounds.extents; } }
    public bool canvasActive {get { return canvas.activeSelf; }} 
    public bool depthLineActive {get { return depthLine.activeSelf; }} 
    public bool trailActive {get { return trail.activeSelf; }} 
    public bool thermoIndActive {get { return thermoInd.activeSelf; }} 

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
    }

    public void ActivateTag(bool activationStatus) { canvas.SetActive(activationStatus); }
    public void ActivateDepthLine(bool activationStatus) { depthLine.SetActive(activationStatus); }
    public void ActivateTrail(bool activationStatus) { trail.SetActive(activationStatus); }
    public void ActivateThermoBob(bool activationStatus) { thermoInd.SetActive(activationStatus); }

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

        if (ThermoclineDOMain.instance.thermoclinePlane.currentDepth != null)
        {
            thermoInd.SetActive(true);

            LinePoint.y = (float)-ThermoclineDOMain.instance.thermoclinePlane.currentDepth * UserSettings.verticalScalingFactor;
            thermoInd.transform.position = LinePoint;
        }
        else { thermoInd.SetActive(false); }
    }

    public void setNewText(string val) { textElement.text = val; }
}
