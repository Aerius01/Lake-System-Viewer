using UnityEngine;
using System;

public class MoonController : MonoBehaviour
{
    // Inputs
    private float latitude = 53, longitude = 13.58f;
    int timeZone = 1;

    // Method variables
    private double[,] periodicLatArray, periodicLongArray;
    private GameObject moonObject, moonLight;
    private Material moonMaterial;

    // Outputs
    private double altitude, azimuth;
    private float moonPhase;

    // Properties
    public float phase
    {
        get {return moonPhase;}
    }

    public float moonAlpha
    {
        get {return moonMaterial.GetFloat("Vector1_46109bf20a484c2caf68bdeec6ce74a7");}
    }

    private void Start()
    {
        // Base moon position is in the north (positive-X) position
        moonObject = this.transform.Find("Moon").gameObject;
        moonLight = this.transform.Find("MoonLight").gameObject;
        moonMaterial = moonObject.GetComponent<MeshRenderer>().material;
        
        // Re-center moon's skybox
        this.transform.position = LocalMeshData.meshCenter;

        // Base moon rotation is UTC-0
        Vector3 moonRotation = moonObject.transform.localEulerAngles;
        moonRotation.y = moonRotation.y + 90f;
        moonObject.transform.localEulerAngles = moonRotation;

        periodicLatArray = new double[60, 7]
        {
            {0, 0, 0, 1, 5128122, 0, 0},
            {0, 0, 1, 1, 280602, 0, 0},
            {0, 0, 1, -1, 277693, 0, 0},
            {2, 0, 0, -1, 173237, 0, 0},
            {2, 0, -1, 1, 55413, 0, 0},
            {2, 0, -1, -1, 46271, 0, 0},
            {2, 0, 0, 1, 32573, 0, 0},
            {0, 0, 2, 1, 17198, 0, 0},
            {2, 0, 1, -1, 9266, 0, 0},
            {0, 0, 2, -1, 8822, 0, 0},
            {2, -1, 0, -1, 8216, 0, 0},
            {2, 0, -2, -1, 4324, 0, 0},
            {2, 0, 1, 1, 4200, 0, 0},
            {2, 1, 0, -1, -3359, 0, 0},
            {2, -1, -1, 1, 2463, 0, 0},
            {2, -1, 0, 1, 2211, 0, 0},
            {2, -1, -1, -1, 2065, 0, 0},
            {0, 1, -1, -1, -1870, 0, 0},
            {4, 0, -1, -1, 1828, 0, 0},
            {0, 1, 0, 1, -1794, 0, 0},
            {0, 0, 0, 3, -1749, 0, 0},
            {0, 1, -1, 1, -1565, 0, 0},
            {1, 0, 0, 1, -1491, 0, 0},
            {0, 1, 1, 1, -1475, 0, 0},
            {0, 1, 1, -1, -1410, 0, 0},
            {0, 1, 0, -1, -1344, 0, 0},
            {1, 0, 0, -1, -1335, 0, 0},
            {0, 0, 3, 1, 1107, 0, 0},
            {4, 0, 0, -1, 1021, 0, 0},
            {4, 0, -1, 1, 833, 0, 0},
            {0, 0, 1, -3, 777, 0, 0},
            {4, 0, -2, 1, 671, 0, 0},
            {2, 0, 0, -3, 607, 0, 0},
            {2, 0, 2, -1, 596, 0, 0},
            {2, -1, 1, -1, 491, 0, 0},
            {2, 0, -2, 1, -451, 0, 0},
            {0, 0, 3, -1, 439, 0, 0},
            {2, 0, 2, 1, 422, 0, 0},
            {2, 0, -3, -1, 421, 0, 0},
            {2, 1, -1, 1, -366, 0, 0},
            {2, 1, 0, 1, -351, 0, 0},
            {4, 0, 0, 1, 331, 0, 0},
            {2, -1, 1, 1, 315, 0, 0},
            {2, -2, 0, -1, 302, 0, 0},
            {0, 0, 1, 3, -283, 0, 0},
            {2, 1, 1, -1, -229, 0, 0},
            {1, 1, 0, -1, 223, 0, 0},
            {1, 1, 0, 1, 223, 0, 0},
            {0, 1, -2, -1, -220, 0, 0},
            {2, 1, -1, -1, -220, 0, 0},
            {1, 0, 1, 1, -185, 0, 0},
            {2, -1, -2, -1, 181, 0, 0},
            {0, 1, 2, 1, -177, 0, 0},
            {4, 0, -2, -1, 176, 0, 0},
            {4, -1, -1, -1, 166, 0, 0},
            {1, 0, 1, -1, -164, 0, 0},
            {4, 0, 1, -1, 132, 0, 0},
            {1, 0, -1, -1, -119, 0, 0},
            {4, -1, 0, -1, 115, 0, 0},
            {2, -2, 0, 1, 107, 0, 0}
        };

        periodicLongArray = new double[60, 7]
        {
            {0, 0, 1, 0, 6288774, 0, 0},
            {2, 0, -1, 0, 1274027, 0, 0},
            {2, 0, 0, 0, 658314, 0, 0},
            {0, 0, 2, 0, 213618, 0, 0},
            {0, 1, 0, 0, -185116, 0, 0},
            {0, 0, 0, 2, -114332, 0, 0},
            {2, 0, -2, 0, 58793, 0, 0},
            {2, -1, -1, 0, 57066, 0, 0},
            {2, 0, 1, 0, 53322, 0, 0},
            {2, -1, 0, 0, 45758, 0, 0},
            {0, 1, -1, 0, -40923, 0, 0},
            {1, 0, 0, 0, -34720, 0, 0},
            {0, 1, 1, 0, -30383, 0, 0},
            {2, 0, 0, -2, 15327, 0, 0},
            {0, 0, 1, 2, -12528, 0, 0},
            {0, 0, 1, -2, 10980, 0, 0},
            {4, 0, -1, 0, 10675, 0, 0},
            {0, 0, 3, 0, 10034, 0, 0},
            {4, 0, -2, 0, 8548, 0, 0},
            {2, 1, -1, 0, -7888, 0, 0},
            {2, 1, 0, 0, -6766, 0, 0},
            {1, 0, -1, 0, -5163, 0, 0},
            {1, 1, 0, 0, 4987, 0, 0},
            {2, -1, 1, 0, 4036, 0, 0},
            {2, 0, 2, 0, 3994, 0, 0},
            {4, 0, 0, 0, 3861, 0, 0},
            {2, 0, -3, 0, 3665, 0, 0},
            {0, 1, -2, 0, -2689, 0, 0},
            {2, 0, -1, 2, -2602, 0, 0},
            {2, -1, -2, 0, 2390, 0, 0},
            {1, 0, 1, 0, -2348, 0, 0},
            {2, -2, 0, 0, 2236, 0, 0},
            {0, 1, 2, 0, -2120, 0, 0},
            {0, 2, 0, 0, -2069, 0, 0},
            {2, -2, -1, 0, 2048, 0, 0},
            {2, 0, 1, -2, -1773, 0, 0},
            {2, 0, 0, 2, -1595, 0, 0},
            {4, -1, -1, 0, 1215, 0, 0},
            {0, 0, 2, 2, -1110, 0, 0},
            {3, 0, -1, 0, -892, 0, 0},
            {2, 1, 1, 0, -810, 0, 0},
            {4, -1, -2, 0, 759, 0, 0},
            {0, 2, -1, 0, -713, 0, 0},
            {2, 2, -1, 0, -700, 0, 0},
            {2, 1, -2, 0, 691, 0, 0},
            {2, -1, 0, -2, 596, 0, 0},
            {4, 0, 1, 0, 549, 0, 0},
            {0, 0, 4, 0, 537, 0, 0},
            {4, -1, 0, 0, 520, 0, 0},
            {1, 0, -2, 0, -487, 0, 0},
            {2, 1, 0, -2, -399, 0, 0},
            {0, 0, 2, -2, -381, 0, 0},
            {1, 1, 1, 0, 351, 0, 0},
            {3, 0, -2, 0, -340, 0, 0},
            {4, 0, -3, 0, 330, 0, 0},
            {2, -1, 2, 0, 327, 0, 0},
            {0, 2, 1, 0, -323, 0, 0},
            {1, 1, -1, 0, 299, 0, 0},
            {2, 0, 3, 0, 294, 0, 0},
            {2, 0, -1, -2, 0, 0, 0}
        };
    }

    public void AdjustMoonPosition()
    {
        CalculateNewMoonPos();
        MoonPhase();
        moonMaterial.SetFloat("Vector1_0bdb1e1f24484093bf09898602915822", moonPhase);
        this.transform.localRotation = Quaternion.Euler(new Vector3(-(float)altitude, -((float)azimuth + 90f), 0f));

        // Have the moon look at the camera
        moonObject.transform.LookAt(Camera.main.transform.position, this.transform.up);
        Vector3 rotationEuler = moonObject.transform.localEulerAngles;
        rotationEuler.y = rotationEuler.y - 90f;
        moonObject.transform.localEulerAngles = rotationEuler;

        // Have the moonlight directed to the center of the mesh & manage intensity
        moonLight.transform.LookAt(LocalMeshData.meshCenter, this.transform.up);
        float intensity = 1 / (0.14f * Mathf.Sqrt(2 * Mathf.PI)) * Mathf.Pow((float)Math.E, - Mathf.Pow(moonPhase - 0.5f, 2) / (2 * Mathf.Pow(0.14f, 2)));
        float maxVal = 1 / (0.14f * Mathf.Sqrt(2 * Mathf.PI));
        moonLight.GetComponent<Light>().intensity = intensity / maxVal * 0.1f;

        // Have moon fade when near & below horizon
        if (altitude <= 15) moonMaterial.SetFloat("Vector1_46109bf20a484c2caf68bdeec6ce74a7", (float)altitude / 15);
        else if (moonMaterial.GetFloat("Vector1_46109bf20a484c2caf68bdeec6ce74a7") != 1f) moonMaterial.SetFloat("Vector1_46109bf20a484c2caf68bdeec6ce74a7", 1f);

        // Have the moon fade if it is between the mesh and the camera
    }

    private double deg_to_rad(double degrees)
    {
        return (degrees * (Math.PI/180));
    }
    
    private double rad_to_deg(double radians)
    {
        return (radians * (180/Math.PI));
    }

    private double ToJulianDate(DateTime date)
    {
        return date.ToOADate() + 2415018.5;
    }

    private double mod(double x, double m) { return (x%m + m)%m; }

    private void CalculateNewMoonPos()
    {
        // http://www.geoastro.de/elevazmoon/basics/index.htm
        // The Excel file from above link is also in the project additional resources folder

        // https://astronomy.stackexchange.com/questions/21002/how-to-find-greenwich-mean-sideral-time
        // https://lweb.cfa.harvard.edu/~jzhao/times.html
        // https://airmass.org/notes
        // https://www.heavens-above.com/moon.aspx?lat=53&lng=13.58&loc=Unspecified&alt=0&tz=CET

        DateTime currentTime = TimeManager.instance.currentTime;
        // DateTime currentTime = DateTime.Parse("2019-01-01 08:00:00");
        
        double julianDay = currentTime.ToOADate() + 2415018.5 - timeZone / 24;
        double T = (julianDay - 2451545) / 36525;

        double eclipticOfDate = (84381.448 - 46.815 * T - 0.00059 * Math.Pow(T, 2) + 0.001813 * Math.Pow(T, 3)) / 3600;
        double ascLunarNode = mod(125.04452 - 1934.136261 * T, 360);
        double meanLongSun = mod(280.4665 + 36000.7698 * T, 360);
        double meanLongMoon = mod(218.3165 + 481267.8813 * T, 360);

        double L_a = mod(218.3164591 + 481267.88134236 * T - 0.0013268 * Math.Pow(T, 2)
            + Math.Pow(T, 3) / 538841 - Math.Pow(T, 4) / 65194000, 360);
        double D = mod(297.8502042+445267.1115168 * T - 0.00163 * Math.Pow(T, 2)
            + Math.Pow(T, 3) / 545868 - Math.Pow(T, 4) / 113065000, 360);
        double M = mod(357.5291092 + 35999.0502909 * T - 0.0001536 * Math.Pow(T, 2)
            + Math.Pow(T, 3) / 24490000, 360);
        double M_a = mod(134.9634114 + 477198.8676313 * T + 0.008997 * Math.Pow(T, 2)
            + Math.Pow(T, 3) / 69699 - Math.Pow(T, 4) / 14712000, 360);
        double F = mod(93.2720993 + 483202.0175273 * T - 0.0034029 * Math.Pow(T, 2) 
            + Math.Pow(T, 3) / 3526000 - Math.Pow(T, 4) / 863310000, 360);
        double A1 = mod(119.75 + 131.849 * T, 360);
        double A2 = mod(53.09 + 479264.29 * T, 360);
        double A3 = mod(313.45 + 481266.484 * T, 360);
        double e = 1 - 0.002516 * T - 0.0000074 * Math.Pow(T, 2);

        // Lambda calcs
        double sumOfPeriodicLongTerms = SumPeriodicLongTerms(e, deg_to_rad(D), deg_to_rad(M), deg_to_rad(M_a), deg_to_rad(F));
        double deltaPhi = - 17.2 * Math.Sin(deg_to_rad(ascLunarNode)) - 1.32 * Math.Sin(2 * deg_to_rad(meanLongSun))
            - 0.23 * Math.Sin(2 * deg_to_rad(meanLongMoon)) + 0.21 * Math.Sin(2 * deg_to_rad(ascLunarNode));
        double meanLambda = L_a + (L_a - F + A1 + A2 + sumOfPeriodicLongTerms) / 1000000;
        double trueLambdaDeg = meanLambda + deltaPhi;

        // Beta calcs
        double sumOfPeriodicLatTerms = SumPeriodicLatTerms(e, deg_to_rad(D), deg_to_rad(M), deg_to_rad(M_a), deg_to_rad(F));
        double deltaE = (9.2 * Math.Cos(ascLunarNode) + 0.57 * Math.Cos((2 * meanLongSun)) 
            + 0.1 * Math.Cos((2 * meanLongMoon)) - 0.09 * Math.Cos((2 * ascLunarNode))) / 3600;
        double meanBeta = (A3 + 2 * A1 + 3 * L_a + sumOfPeriodicLatTerms) / 1000000;
        double trueBetaDeg = deltaE + meanBeta;

        // Ecliptic calc
        double trueEclipticOfDateDeg = eclipticOfDate + deltaE / 3600;

        // Right ascension calcs
        double a = Math.Sin(deg_to_rad(trueLambdaDeg)) * Math.Cos(deg_to_rad(trueEclipticOfDateDeg))
            - (Math.Sin(deg_to_rad(trueBetaDeg)) / Math.Sin(deg_to_rad(trueBetaDeg))) 
            * Math.Sin(deg_to_rad(trueEclipticOfDateDeg));
        double b = Math.Cos(deg_to_rad(trueLambdaDeg));
        double tan = a / b;

        double alpha = 0f;
        if (b < 0f) { alpha = Math.PI + Math.Atan(tan); }
        else if (a < 0f) { alpha = 2 * Math.PI + Math.Atan(tan); }
        else { alpha = Math.Atan(tan); }

        double rightAscension = rad_to_deg(alpha) / 15;
        double declination = rad_to_deg(Math.Asin(Math.Sin(deg_to_rad(trueBetaDeg)) * Math.Cos(deg_to_rad(trueEclipticOfDateDeg))
            + Math.Cos(deg_to_rad(trueBetaDeg)) * Math.Sin(deg_to_rad(trueEclipticOfDateDeg)) * Math.Sin(deg_to_rad(trueLambdaDeg))));

        // Local mean sidereal time calcs
        double modT = (currentTime.Date.ToOADate() + 2415018.5 - 2451545) / 36525;
        double GMST1 = 24110.54841 + 8640184.812866 * modT + 0.093104 * Math.Pow(modT, 2) - 0.0000062 * Math.Pow(modT, 3);
        double corrFactor = (1.00273790935 + 5.9 * Math.Pow(10, -11) * modT) * (currentTime.TimeOfDay.TotalSeconds);
        double GMST = (mod(GMST1 + corrFactor, 86400)) / 3600 * 15;

        // Local hour angle
        Vector2 GMSTVector = new Vector2((float)Math.Cos(deg_to_rad(GMST)), (float)Math.Sin(deg_to_rad(GMST)));
        Vector2 longVector = new Vector2((float)Math.Cos(deg_to_rad(longitude)), (float)Math.Sin(deg_to_rad(longitude)));
        double localHourAngle = Vector2.Angle(GMSTVector, longVector);

        // Correction for LHA selection based on hemisphere
        if (GMST < longitude + 180 && GMST > longitude) localHourAngle = 360 - localHourAngle;

        // Final values
        altitude = rad_to_deg(Math.Asin(Math.Sin(deg_to_rad(latitude)) * Math.Sin(deg_to_rad(declination))
            + Math.Cos(deg_to_rad(latitude)) * Math.Cos(deg_to_rad(declination)) * Math.Cos(deg_to_rad(localHourAngle))));
        
        azimuth = localHourAngle > 180 ?
            rad_to_deg(Math.Acos((Math.Sin(deg_to_rad(declination)) - Math.Sin(deg_to_rad(altitude))
            * Math.Sin(deg_to_rad(latitude))) / (Math.Cos(deg_to_rad(altitude)) * Math.Cos(deg_to_rad(latitude))))) :
            360 - (rad_to_deg(Math.Acos((Math.Sin(deg_to_rad(declination)) - Math.Sin(deg_to_rad(altitude))
            * Math.Sin(deg_to_rad(latitude))) / (Math.Cos(deg_to_rad(altitude)) * Math.Cos(deg_to_rad(latitude))))))
        ;
    }

    private double SumPeriodicLatTerms(double e, double D, double M, double M_a, double F)
    {
        double currentSum = 0f;
        for (int i = 0; i < periodicLatArray.GetLength(0); i++)
        {
            if (Math.Abs(periodicLatArray[i, 1]) == 1)
            {
                periodicLatArray[i, 5] = periodicLatArray[i, 4] * e;
            }
            else if (Math.Abs(periodicLatArray[i, 1]) == 2)
            {
                periodicLatArray[i, 5] = periodicLatArray[i, 4] * Math.Pow(e, 2);
            }
            else
            {
                periodicLatArray[i, 5] = periodicLatArray[i, 4];
            };

            periodicLatArray[i, 6] = periodicLatArray[i, 5] * Math.Sin((periodicLatArray[i, 0] * D
                + periodicLatArray[i, 1] * M + periodicLatArray[i, 2] * M_a + periodicLatArray[i, 3] * F));
            
            currentSum += periodicLatArray[i, 6];
        }

        return currentSum;
    }

    private double SumPeriodicLongTerms(double e, double D, double M, double M_a, double F)
    {
        double currentSum = 0f;
        for (int i = 0; i < periodicLongArray.GetLength(0); i++)
        {
            if (Math.Abs(periodicLongArray[i, 1]) == 1)
            {
                periodicLongArray[i, 5] = periodicLongArray[i, 4] * e;
            }
            else if (Math.Abs(periodicLongArray[i, 1]) == 2)
            {
                periodicLongArray[i, 5] = periodicLongArray[i, 4] * Math.Pow(e, 2);
            }
            else
            {
                periodicLongArray[i, 5] = periodicLongArray[i, 4];
            };

            periodicLongArray[i, 6] = periodicLongArray[i, 5] * Math.Sin((periodicLongArray[i, 0] * D
                + periodicLongArray[i, 1] * M + periodicLongArray[i, 2] * M_a + periodicLongArray[i, 3] * F));
            
            currentSum += periodicLongArray[i, 6];
        }

        return currentSum;
    }

    private void MoonPhase()
    {
        // https://minkukel.com/en/various/calculating-moon-phase/
        // returns a value between 0 & 1, where 0 & 1 are the new moon, and 0.5 is the full moon
        double phasePeriod = 29.53058770576;

        double newMoonOrigin = (DateTime.Parse("2000-01-06 18:14:00").ToOADate()) + 2415018.5;
        double diffJulianDate = TimeManager.instance.currentTime.ToOADate() + 2415018.5 - newMoonOrigin;

        moonPhase = (float)(mod(diffJulianDate, phasePeriod) / phasePeriod);
    }
}