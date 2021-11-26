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
        bodyRenderer.material.color = stdBodyColor;
        finRenderer.material.color = stdFinColor;
    }

    public void SetColor(Color color)
    {
        bodyRenderer.material.color = color;
        finRenderer.material.color = color;
    }
}
