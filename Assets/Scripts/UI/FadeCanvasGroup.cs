using System.Collections;
using UnityEngine;

public class FadeCanvasGroup : MonoBehaviour
{
    CanvasGroup canvasGroup;
    Coroutine co;
    bool coroutineRunning = false;


    // Start is called before the first frame update
    public void Fade(float delay)
    {
        if (coroutineRunning)
        {
            StopCoroutine(co); // stop the coroutine
        }
        
        canvasGroup = this.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1;

        co = StartCoroutine(DoFade(delay));
    }

    private IEnumerator DoFade(float delay)
    {
        coroutineRunning = true;
        yield return new WaitForSeconds(delay);

        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= 0.015f;
            yield return null;
        }

        coroutineRunning = false;
    }
}
