using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class GradientCell : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    public GameObject textBox;
    [SerializeField] private TextMeshProUGUI text;
    public float? currentVal {get; private set;}
    public float currentDepth {get; private set;}
    public bool typeTemp {get; private set;}

    public void StartUp() { textBox.SetActive(false); }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (currentVal == null)
        {
            text.text = string.Format("-");
        }
        else
        {
            text.text = typeTemp ? string.Format("{0:0.00}m, {1:0.00}C", currentDepth, currentVal) : string.Format("{0:0.00}m, {1:0.00}mg/L", currentDepth, currentVal);
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
