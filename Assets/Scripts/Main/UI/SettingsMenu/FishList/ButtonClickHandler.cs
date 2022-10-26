using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using TMPro;

public class ButtonClickHandler : MonoBehaviour, IPointerClickHandler
{
    private Coroutine moveCamera;
    private bool zoomDisabled;

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.W) || Input.GetKeyDown(KeyCode.A) || Input.GetKeyDown(KeyCode.S) || Input.GetKeyDown(KeyCode.D))
        { if (this.moveCamera != null) { StopCoroutine(this.moveCamera); } }
    }

    public void OnPointerClick(PointerEventData eventData) { if (!zoomDisabled) { if (eventData.clickCount == 2) this.moveCamera = StartCoroutine(FocusCamera()) as Coroutine; } }

    private IEnumerator FocusCamera()
    {
        Camera mainCamera = Camera.main;
        Vector3 startCamPos = mainCamera.transform.position;

        int fishID = int.Parse(this.gameObject.transform.Find("FishID").GetComponent<TextMeshProUGUI>().text);

        Vector3 endCamPos = FishManager.fishDict[fishID].currentPosition;

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
