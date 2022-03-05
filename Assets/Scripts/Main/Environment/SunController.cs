using UnityEngine;
using System;

public class SunController : MonoBehaviour
{
    // https://www.youtube.com/watch?v=babgYCTyw3Y
    [SerializeField] private GameObject skyTransform;
    [SerializeField] private Gradient sunColor;

    public float latitude = 53, longitude = 13.58f;
    int timeZone = 1;

    private double currentZenithAngleDeg, currentAzimuthAngleDeg;

    public void AdjustSunPosition()
    {
        CalculateNewSunPos();
        skyTransform.transform.localRotation = Quaternion.Euler(new Vector3(90 - (float)currentZenithAngleDeg, (float)currentAzimuthAngleDeg, 0f));
    }

    private double degrees_to_radians(double degrees)
    {
        return (degrees * (Math.PI/180));
    }
    
    private double radians_to_degrees(double radians)
    {
        return (radians * (180/Math.PI));
    }

    private double ToJulianDate(DateTime date)
    {
        return date.ToOADate() + 2415018.5;
    }

    private void CalculateNewSunPos()
    {
        DateTime currentTime = TimeManager.instance.currentTime;

        double julianDay = ToJulianDate(currentTime) - timeZone / 24;
        double julianCentury = (julianDay - 2451545) / 36525;

        double geoMeanLongSunDegrees = (280.46646 + julianCentury * (36000.76983 + julianCentury * 0.0003032)) % 360;
        double geoMeanAnomSunDegrees = 357.52911 + julianCentury * (35999.05029 - 0.0001537 * julianCentury);
        double eccentEarthOrbit = 0.016708634 - julianCentury * (0.000042037 + 0.0000001267 * julianCentury);

        double meanObliqEclipticDegrees = 23 + (26 + ((21.448 - julianCentury * (46.815 + julianCentury * (0.00059 - julianCentury * 0.001813)))) / 60) / 60;
        double obliqueCorrDegrees = meanObliqEclipticDegrees + 0.00256 * Math.Cos(degrees_to_radians(125.04 - 1934.136 * julianCentury));

        double varianceY = Math.Tan(degrees_to_radians(obliqueCorrDegrees / 2)) * Math.Tan(degrees_to_radians(obliqueCorrDegrees / 2));

        double EqOfTimeMin = 4 * radians_to_degrees(varianceY * Math.Sin(2 * degrees_to_radians(geoMeanLongSunDegrees)) - 
                            2 * eccentEarthOrbit * Math.Sin(degrees_to_radians(geoMeanAnomSunDegrees)) +
                            4 * eccentEarthOrbit * varianceY * Math.Sin(degrees_to_radians(geoMeanAnomSunDegrees)) * Math.Cos(2 * degrees_to_radians(geoMeanLongSunDegrees)) -
                            0.5 * Math.Pow(varianceY, 2) * Math.Sin(4 * degrees_to_radians(geoMeanLongSunDegrees)) - 
                            1.25 * Math.Pow(eccentEarthOrbit, 2) * Math.Sin(2 * degrees_to_radians(geoMeanAnomSunDegrees)));

        double sunEqOfCtr = Math.Sin(degrees_to_radians(geoMeanAnomSunDegrees)) * (1.914602 - julianCentury * (0.004817 + 0.000014 * julianCentury)) +
                            Math.Sin(degrees_to_radians(2 * geoMeanAnomSunDegrees)) * (0.019993 - 0.000101 * julianCentury) +
                            Math.Sin(degrees_to_radians(3 * geoMeanAnomSunDegrees)) * 0.000289;

        double sunTrueLongDegrees = geoMeanLongSunDegrees + sunEqOfCtr;
        double sunAppLongDegrees = sunTrueLongDegrees - 0.00569 - 0.00478 * Math.Sin(degrees_to_radians(125.04 - 1934.136 * julianCentury));
        double trueSolarTimeMin = (((currentTime - currentTime.Date).TotalMinutes + EqOfTimeMin + 4 * longitude - 60 * timeZone) % 1440);

        double sunDeclinationDegrees = radians_to_degrees(Math.Asin(Math.Sin(degrees_to_radians(obliqueCorrDegrees)) * Math.Sin(degrees_to_radians(sunAppLongDegrees))));
        double hourAngleDegrees = (trueSolarTimeMin / 4 < 0) ? (trueSolarTimeMin / 4 + 180) : (trueSolarTimeMin / 4 - 180);

        double solarZenithAngleDegrees = radians_to_degrees(Math.Acos(Math.Sin(degrees_to_radians(latitude)) * Math.Sin(degrees_to_radians(sunDeclinationDegrees)) + 
                                        Math.Cos(degrees_to_radians(latitude)) * Math.Cos(degrees_to_radians(sunDeclinationDegrees)) * Math.Cos(degrees_to_radians(hourAngleDegrees))));
        
        double solarElevationAngleDegrees = 90 - solarZenithAngleDegrees;

        double solarAzimuthAngleDegrees = hourAngleDegrees > 0 ?
                                        (radians_to_degrees(Math.Acos(((Math.Sin(degrees_to_radians(latitude)) * Math.Cos(degrees_to_radians(solarZenithAngleDegrees))) -
                                            Math.Sin(degrees_to_radians(sunDeclinationDegrees))) / (Math.Cos(degrees_to_radians(latitude)) * Math.Sin(degrees_to_radians(solarZenithAngleDegrees))))) +
                                            180) % 360 :
                                        (540 - radians_to_degrees(Math.Acos(((Math.Sin(degrees_to_radians(latitude)) * Math.Cos(degrees_to_radians(solarZenithAngleDegrees))) - 
                                            Math.Sin(degrees_to_radians(sunDeclinationDegrees))) / (Math.Cos(degrees_to_radians(latitude)) * Math.Sin(degrees_to_radians(solarZenithAngleDegrees)))))) % 360;
        
        currentZenithAngleDeg = solarZenithAngleDegrees;
        currentAzimuthAngleDeg = solarAzimuthAngleDegrees;
    }
}
