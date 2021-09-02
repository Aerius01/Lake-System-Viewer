using System.Collections;
using UnityEngine;

public class FadeCanvasGroup : MonoBehaviour
{
    CanvasGroup canvasGroup;

    // Start is called before the first frame update
    public void Fade(float delay)
    {
        canvasGroup = this.GetComponent<CanvasGroup>();
        canvasGroup.alpha = 1;

        StartCoroutine(DoFade(delay));
    }

    private IEnumerator DoFade(float delay)
    {
        yield return new WaitForSeconds(delay);

        while (canvasGroup.alpha > 0)
        {
            canvasGroup.alpha -= 0.015f;
            yield return null;
        }
    }
}
