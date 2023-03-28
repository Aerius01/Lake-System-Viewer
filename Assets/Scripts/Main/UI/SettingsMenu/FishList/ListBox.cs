using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using TMPro;
using Unity;

public class ListBox : MonoBehaviour
{
    public bool open {get; protected set;} = false;
    protected bool? opening = null;

    // Coroutine
    protected RectTransform parentRect;
    [SerializeField] protected GameObject arrowImage;

    protected RectTransform rect;
    [SerializeField] protected Toggle activeToggle, tagToggle, depthToggle, trailToggle;
    protected TextMeshProUGUI headerText;


    protected virtual IEnumerator AnimateChange(float headerSize, float contentSize, GameObject contentWindow)
    {
        // if currently closed, activate the canvases before opening
        if (!this.open) contentWindow.SetActive(true); 

        // if currently open, the position differential is negative
        float diff = this.open ? headerSize - contentSize : contentSize - headerSize;
        float rotDiff = this.open ? 90f : -90f;

        // rotation of arrowhead occurs in the first 1/3 of the animation, always
        float targetTime = 0.1f;
        float totalIncrements = 60f * targetTime; // 60fps
        float rotIncrements = Mathf.RoundToInt(totalIncrements / 3);
        float period = targetTime / totalIncrements;
        
        float increment = diff / totalIncrements;
        float rotIncr = rotDiff / rotIncrements;

        for (float i = 0; i < totalIncrements; i ++)
        {
            if (i < rotIncrements) arrowImage.transform.localEulerAngles += new Vector3(0f, 0f, rotIncr);
            this.rect.sizeDelta += new Vector2(0, increment);
            LayoutRebuilder.ForceRebuildLayoutImmediate(parentRect);
            yield return new WaitForSeconds(period);
        }

        if (this.opening == true) this.open = true;
        else this.open = false;

        // if currently closed now, then deactivate the canvases
        if (!this.open) contentWindow.SetActive(false); 

        this.opening = null;
    }

    protected void ToggleInteractable(bool status) { this.tagToggle.interactable = this.depthToggle.interactable = this.trailToggle.interactable = status; }

}