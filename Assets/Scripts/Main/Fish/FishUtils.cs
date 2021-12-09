using UnityEngine;
using TMPro;

public class FishUtils : MonoBehaviour
{
    [SerializeField]
    private GameObject canvas, depthLine, trail;

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            BoxCollider collider = this.GetComponent<BoxCollider>();
             
            if (collider.Raycast(ray, out hit, 999999f))
            {
                this.ToggleTag();
            }
        }
    }

    public void ActivateTag(bool activationStatus)
    {
        this.canvas.SetActive(activationStatus);
    }

    public void ActivateDepthLine(bool activationStatus)
    {
        this.depthLine.SetActive(activationStatus);
    }
    public void ActivateTrail(bool activationStatus)
    {
        this.trail.SetActive(activationStatus);
    }

    public void ToggleTag()
    {
        this.canvas.SetActive(!canvas.activeSelf);
    }

    public void ToggleDepthLine()
    {
        this.depthLine.SetActive(!depthLine.activeSelf);
    }

    public void ToggleTrail()
    {
        this.trail.SetActive(!trail.activeSelf);
    }

    public void ClearTrail()
    {
        this.trail.GetComponent<TrailRenderer>().Clear();
    }

    public void UpdateCanvasText(string updateText)
    {
        this.canvas.transform.Find("Panel").transform.Find("Background").transform.Find("InfoText").
            GetComponent<TextMeshProUGUI>().text = updateText;
    }

    public void UpdateDepthIndicatorLine(Vector3 LinePoint)
    {
        // TODO: find the lake depth at the position of the fish
        
        LineRenderer line = depthLine.GetComponent<LineRenderer>();
        GameObject waterblock = GameObject.Find("WaterBlock");
        
        LinePoint.y = - Mathf.Abs(LocalMeshData.minDepth) * UserSettings.verticalScalingFactor;
        line.SetPosition(0, LinePoint);

        LinePoint.y = waterblock.transform.position.y;
        line.SetPosition(1, LinePoint);
    }
}
