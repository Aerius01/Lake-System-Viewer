using UnityEngine;
using System.Collections;
using UnityEngine.UI;

public class SubMenuTriggers : ButtonTriggerAnimation
{
    // Start is called before the first frame update
    public override void ToggleBool()
    {
        StartCoroutine(ToggleBoolCR());
        

        // Add or subtract 250 from the viewport height
        // currentSize.y = !this.animator.GetBool(boolName) ? currentSize.y + 250 : currentSize.y - 250;
        // contentObject.GetComponent<RectTransform>().sizeDelta = currentSize;

        //LayoutRebuilder.ForceRebuildLayoutImmediate(contentObject.GetComponent<RectTransform>());
    }

    private IEnumerator ToggleBoolCR()
    {
        GameObject contentObject = this.gameObject.transform.parent.gameObject;
        Vector2 currentSize = contentObject.GetComponent<RectTransform>().sizeDelta;

        if (this.animator.GetBool(boolName))
        {
            currentSize.y = currentSize.y - 250;
            contentObject.GetComponent<RectTransform>().sizeDelta = currentSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentObject.GetComponent<RectTransform>());

            base.ToggleBool();            
        }
        else
        {
            base.ToggleBool();
            yield return new WaitForSeconds(0.5f);

            currentSize.y = currentSize.y + 250;
            contentObject.GetComponent<RectTransform>().sizeDelta = currentSize;
            LayoutRebuilder.ForceRebuildLayoutImmediate(contentObject.GetComponent<RectTransform>());
        }
    }
}
