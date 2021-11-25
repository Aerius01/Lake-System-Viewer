using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class ButtonClickHandler : MonoBehaviour, IPointerClickHandler
{
    float interval = 0.3f;
    int tap;

    public void OnPointerClick(PointerEventData eventData)
    {
        tap++;
        if (tap == 1)
        {
            StartCoroutine(DoubleTapInterval());
        }

        else if (tap > 1)
        {
            // Zoom in
            StartCoroutine(FocusCamera());
            tap = 0;
        }
    }
    
    private IEnumerator DoubleTapInterval()
    {
        yield return new WaitForSeconds(interval);
        
        if (this.tap == 1)
        {
            this.gameObject.transform.parent.GetComponent<SubMenuTriggers>().ToggleBool();
        }
        
        this.tap = 0;
    }

    private IEnumerator FocusCamera()
    {
        Camera mainCamera = Camera.main;
        Vector3 startCamPos = mainCamera.transform.position;

        GameObject fishObject = FishGeneratorNew.GetFishObject(int.Parse(this.gameObject.transform.Find("FishID").GetComponent<TextMeshProUGUI>().text));
        Vector3 endCamPos = fishObject.transform.position;

        // The vector along which the camera will travel
        Vector3 travelVector = endCamPos - startCamPos;
        mainCamera.transform.LookAt(fishObject.transform);

        // Loop until the vector between the camera and fish has a length of <= 30
        for (float ratio = 0; travelVector.magnitude > 30; ratio = ratio + 0.01f)
        {
            mainCamera.transform.position = Vector3.Lerp(startCamPos, endCamPos, ratio);
            travelVector = endCamPos - mainCamera.transform.position;
            yield return new WaitForSeconds(0.005f);
        }
    }
}
