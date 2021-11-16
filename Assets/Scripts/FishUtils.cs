using UnityEngine;
using TMPro;

public class FishUtils : MonoBehaviour
{
    [SerializeField]
    private GameObject canvas, depthLine, trail;

    public void ToggleTag()
    {
        canvas.SetActive(!canvas.activeSelf);
    }

    public void ToggleDepthLine()
    {
        depthLine.SetActive(!depthLine.activeSelf);
    }

    public void ToggleTrail()
    {
        trail.SetActive(!trail.activeSelf);
    }

    public void ClearTrail()
    {
        trail.GetComponent<TrailRenderer>().Clear();
    }

    public void UpdateCanvasText(string updateText)
    {
        canvas.transform.Find("Panel").transform.Find("Background").transform.Find("InfoText").
            GetComponent<TextMeshProUGUI>().text = updateText;
    }

    public void UpdateDepthIndicatorLine(Vector3 LinePoint)
    {
        // TODO: find the lake depth at the position of the fish
        
        LineRenderer line = depthLine.GetComponent<LineRenderer>();
        GameObject waterblock = GameObject.Find("WaterBlock");
        
        LinePoint.y = - Mathf.Abs(MeshData.instance.maxDepth) * UserSettings.verticalScalingFactor;
        line.SetPosition(0, LinePoint);

        LinePoint.y = waterblock.transform.position.y;
        line.SetPosition(1, LinePoint);
    }
}
