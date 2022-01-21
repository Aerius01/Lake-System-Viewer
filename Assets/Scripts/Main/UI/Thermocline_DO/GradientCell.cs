using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class GradientCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject textBox;
    private TextMeshProUGUI text;
    public float? currentVal {get; private set;}
    public float currentDepth {get; private set;}
    public bool typeTemp {get; private set;}

    private void Start()
    {
        text = textBox.transform.Find("Tooltip").GetComponent<TextMeshProUGUI>();
        textBox.SetActive(false);
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentVal == null)
        {
            text.text = string.Format("-");
        }
        else
        {
            text.text = typeTemp ? string.Format("{0}m, {1}C", currentDepth, currentVal) : string.Format("{0}m, {1}mg/L", currentDepth, currentVal);
        }

        textBox.SetActive(true);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        textBox.SetActive(false);
    }

    public void SetColor(Color color)
    {
        Color currentColor = this.GetComponent<Image>().color;
        currentColor = color;
        this.GetComponent<Image>().color = currentColor;
    }

    public void SetDepth(float depth)
    {
        currentDepth = depth;
    }

    public void SetVal(float? value)
    {
        currentVal = value;
    }

    public void IsTemp(bool value)
    {
        typeTemp = value;
    }
}
