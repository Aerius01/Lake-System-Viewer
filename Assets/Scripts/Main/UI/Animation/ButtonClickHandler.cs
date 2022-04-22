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
        {
            if (this.moveCamera != null) { StopCoroutine(this.moveCamera); }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (zoomDisabled) { this.gameObject.transform.parent.GetComponent<SubMenuTriggers>().ToggleBool(); }
        else
        {
            tap++;
            if (tap == 1)
            {
                StartCoroutine(DoubleTapInterval());
            }

            else if (tap > 1)
            {
                // Zoom in
                this.moveCamera = StartCoroutine(FocusCamera()) as Coroutine;
                tap = 0;
            }
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

    public void DisableZoom(bool state)
    {
        zoomDisabled = state;
    }
}
