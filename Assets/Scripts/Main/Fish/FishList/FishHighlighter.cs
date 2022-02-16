using UnityEngine;

public class FishHighlighter : MonoBehaviour
{
    public Renderer[] renderers;
    private Color[] stdColors;

    private void Start()
    {
        Color[] stdColors = new Color[renderers.Length];
        for (int i = 0; i < renderers.Length; i++)
        {
            stdColors[i] = renderers[i].material.color;
        }        
    }

    public void ResetColor()
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.DisableKeyword("_EMISSION");
            renderers[i].material.color = stdColors[i];
            renderers[i].material.SetColor("_EmissionColor", Color.black);
        }
    }

    public void SetColor(Color color)
    {
        for (int i = 0; i < renderers.Length; i++)
        {
            renderers[i].material.EnableKeyword("_EMISSION");
            renderers[i].material.SetColor("_EmissionColor", color);
            DynamicGI.SetEmissive(renderers[i], color);
        }
    }
}
