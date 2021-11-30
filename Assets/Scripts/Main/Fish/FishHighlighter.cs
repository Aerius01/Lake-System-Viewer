using UnityEngine;

public class FishHighlighter : MonoBehaviour
{
    public Renderer bodyRenderer, finRenderer;
    private Color stdBodyColor, stdFinColor;

    private void Start()
    {
        stdBodyColor = bodyRenderer.material.color;
        stdFinColor = finRenderer.material.color;
    }

    public void ResetColor()
    {
        bodyRenderer.material.DisableKeyword("_EMISSION");
        finRenderer.material.DisableKeyword("_EMISSION");

        bodyRenderer.material.color = stdBodyColor;
        finRenderer.material.color = stdFinColor;

        bodyRenderer.material.SetColor("_EmissionColor", Color.black);
        finRenderer.material.SetColor("_EmissionColor", Color.black);
    }

    public void SetColor(Color color)
    {
        bodyRenderer.material.EnableKeyword("_EMISSION");
        finRenderer.material.EnableKeyword("_EMISSION");   

        bodyRenderer.material.SetColor("_EmissionColor", color);
        finRenderer.material.SetColor("_EmissionColor", color); 

        DynamicGI.SetEmissive(bodyRenderer, color);
        DynamicGI.SetEmissive(finRenderer, color);
    }
}
