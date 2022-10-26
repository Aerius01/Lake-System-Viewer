using UnityEngine;
using System.Collections;
using TMPro;

[RequireComponent(typeof(CanvasGroup))]

public class PassiveFade : MonoBehaviour
{
    private Vector3 lastMousePosition;
    private Coroutine coroutine;
    private CanvasGroup canvasGroup;
    private bool routineRunning;
    private float timer, fadeTime = 5f;
    private TMP_InputField[] childInputFields;

    private bool activelyModifying
    {
        get
        {
            bool modifying = false;
            if (this.childInputFields.Length != 0) foreach (TMP_InputField field in this.childInputFields) { if (field.isFocused) { modifying = true; break; } }
            return modifying;
        }
    }
    
    private void Start()
    {
        lastMousePosition = new Vector3(0f, 0f, 0f);
        this.coroutine = null;
        this.canvasGroup = this.gameObject.GetComponent<CanvasGroup>();
        this.childInputFields = this.GetComponentsInChildren<TMP_InputField>();
    }

    private void Update()
    {
        if (Input.mousePosition != lastMousePosition || (Input.anyKey && !Input.GetKey("w") && !Input.GetKey("a") && !Input.GetKey("s") && !Input.GetKey("d") && !Input.GetKey("q") && !Input.GetKey("e")) || this.activelyModifying)
        {
            // Bring the UI object back to full opacity
            timer = 0f;

            lastMousePosition = Input.mousePosition;
            if (routineRunning)
            {
                StopCoroutine(coroutine);
                routineRunning = false;
                canvasGroup.alpha = 1f;
            }
            else if (canvasGroup.alpha != 1f) {canvasGroup.alpha = 1f;}
        }
        else
        {
            // Wait 3 seconds and then start fading again
            timer = timer + Time.deltaTime;
            if (timer >= 3f && !routineRunning)
            {
                if (canvasGroup.alpha == 1f)
                {
                    coroutine = StartCoroutine(FadeRoutine()) as Coroutine;
                }
            }
        }
    }    

    private IEnumerator FadeRoutine()
    {
        routineRunning = true;
        float alphaVal = 1f;
        float incrementTime = 0.1f;
        float decrement = alphaVal / (fadeTime / incrementTime);

        while (canvasGroup.alpha != 0f)
        {
            alphaVal = alphaVal - decrement;
            if (alphaVal < 0f) {alphaVal = 0f;}

            canvasGroup.alpha = alphaVal;

            yield return new WaitForSeconds(incrementTime);

        }
        routineRunning = false;
    }
}
