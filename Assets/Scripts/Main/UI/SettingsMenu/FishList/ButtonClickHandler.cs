using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class ButtonClickHandler : MonoBehaviour, IPointerClickHandler
{
    private float interval = 0.3f;
    private int tap;
    private Coroutine moveCamera;
    private bool zoomDisabled;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D))
        { if (this.moveCamera != null) { StopCoroutine(this.moveCamera); } }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!zoomDisabled)
        {
            tap++;
            if (tap == 1) { StartCoroutine(DoubleTapInterval()); }
        }
    }
    
    private IEnumerator DoubleTapInterval()
    {
        for (float i = 0; i < interval; i += 0.02f)
        {
            if (tap > 1) { this.moveCamera = StartCoroutine(FocusCamera()) as Coroutine; break; }
            yield return new WaitForSeconds(0.02f);
        }
        
        this.tap = 0;
    }

    private IEnumerator FocusCamera()
    {
        Camera mainCamera = Camera.main;
        Vector3 startCamPos = mainCamera.transform.position;

        int fishID = int.Parse(this.gameObject.transform.Find("FishID").GetComponent<TextMeshProUGUI>().text);

        Vector3 endCamPos = FishManager.GetFishPosition(fishID);

        // The vector along which the camera will travel
        Vector3 travelVector = endCamPos - startCamPos;
        FishManager.LookAtFish(fishID);

        // Loop until the vector between the camera and fish has a length of <= 30
        for (float ratio = 0; travelVector.magnitude > 30; ratio = ratio + 0.035f)
        {
            mainCamera.transform.position = Vector3.Lerp(startCamPos, endCamPos, ratio);
            travelVector = endCamPos - mainCamera.transform.position;
            yield return new WaitForSeconds(0.005f);
        }

        this.moveCamera = null;
    }

    public void DisableZoom(bool state) { zoomDisabled = state; }
}
