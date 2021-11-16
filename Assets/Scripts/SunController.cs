using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System;

public class SunController : MonoBehaviour
{
    // https://www.youtube.com/watch?v=babgYCTyw3Y
    [SerializeField] private GameObject skyTransform;
    [SerializeField] private Gradient sunColor;

    public float axisTilt;

    public void AdjustSunPosition()
    {
        // TODO: have axisTilt stabilize the intensity. Too severe a tilt keeps dusk lighting for the entire day
        DateTime currentTime = TimeManager.instance.currentTime;
        float sunAngle = (float)currentTime.TimeOfDay.TotalSeconds / 86400;

        // The sun rises at 6h, and sets at 18h
        skyTransform.transform.localRotation = Quaternion.Euler(new Vector3(axisTilt, 0f, 360f * sunAngle + 90));

        // Control sun intensity
        Light sun = this.GetComponent<Light>();
        float intensity = Vector3.Dot(sun.transform.forward, Vector3.down);
        intensity = Mathf.Clamp01(intensity);

        sun.intensity = intensity;

        sun.color = sunColor.Evaluate(intensity);

    }


}
