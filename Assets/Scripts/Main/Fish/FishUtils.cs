using UnityEngine;
using TMPro;
using System.Collections.Generic;

public class FishUtils : MonoBehaviour
{
    public GameObject canvas, depthLine, trail, thermoInd;

    private Dictionary<string, GameObject> classifier;
    public string newCanvasText {
        set
        {
            this.canvas.transform.Find("Panel").transform.Find("Background").transform.Find("InfoText").
            GetComponent<TextMeshProUGUI>().text = value;
        }
    }

    private void Start()
    {
        classifier = new Dictionary<string, GameObject>()
        {
            {"tag", canvas},
            {"line", depthLine},
            {"trail", trail},
            {"thermo", thermoInd}
        };
    }

    private void Update()
    {
        if (Input.GetMouseButtonDown(0))
        {
            RaycastHit hit;
            Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);

            BoxCollider collider = this.GetComponent<BoxCollider>();
             
            if (collider.Raycast(ray, out hit, 999999f))
            {
                this.ToggleUtil("tag");
            }
        }
    }

    public void ActivateUtil(string util, bool activationStatus)
    {
        GameObject obj = classifier[util];
        obj.SetActive(activationStatus);
    }

    public void ToggleUtil(string util)
    {
        GameObject obj = classifier[util];
        obj.SetActive(!obj.activeSelf);
    }

    public void ClearTrail()
    {
        this.trail.GetComponent<TrailRenderer>().Clear();
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

        if (ThermoclineDOMain.instance.thermoclinePlane.currentDepth != null)
        {
            thermoInd.SetActive(true);

            LinePoint.y = (float)-ThermoclineDOMain.instance.thermoclinePlane.currentDepth * UserSettings.verticalScalingFactor;
            thermoInd.transform.position = LinePoint;
        }
        else
        {
            thermoInd.SetActive(false);
        }
    }
}
