using UnityEngine;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System;

public class ThermoclDepth
{
    private List<ProfileEntry> profileEntries;
    private List<float> uniqueDepths;
    private float maxTemp, minTemp;

    public ThermoclDepth()
    {
        DataRow[] currentData = LocalThermoclineData.thermoDict[LocalThermoclineData.uniqueTimeStamps[ThermoclineDOMain.instance.CurrentIndex()]];

        uniqueDepths = new List<float>();
        profileEntries = new List<ProfileEntry>();
        maxTemp = float.MinValue;
        minTemp = float.MaxValue;

        for (int i = 0; i < currentData.Length; i++)
        {
            float depth = float.Parse(currentData[i]["d"].ToString());
            if (!string.IsNullOrEmpty(currentData[i]["temp"].ToString()))
            {
                float tempVal = float.Parse(currentData[i]["temp"].ToString());

                profileEntries.Add(new ProfileEntry(depth, tempVal, WaterDensity(tempVal)));

                maxTemp = Math.Max(tempVal, maxTemp);
                minTemp = Math.Min(tempVal, minTemp);

                if (!uniqueDepths.Contains(depth))
                {
                    uniqueDepths.Add(depth);
                }
            }
        }
    }

    private double WaterDensity(float temp)
    {
        // The CIPM Formula, assuming atmospheric pressure
        // https://metgen.pagesperso-orange.fr/metrologieen19.htm

        // Corrective factor for dissolved air
        double d1 = -4.612 * Math.Pow(10, -3); // kg/m3
        double d2 = 0.106 * Math.Pow(10, -3); // kg/m3 · °C^-1
        double Cw = d1 + d2 * temp; // kg/m3

        // Density Calculation
        double a1 = -3.983035; // °C	 	
        double a2 = 301.797; // °C	 	
        double a3 = 522528.9; // °C	
        double a4 = 69.34881; // °C	 	
        double a5 = 999.974950; // kg/m3

        double density = a5 * (1 - (Math.Pow((temp + a1), 2) * (temp + a2)) / (a3 * (temp + a4))); // kg/m3

        return (Cw + density);
    }

    public static List<int> GetInflectionPoints(double[] source)
    {
        var polarity = true;
        List<int> inflectionPoints = new List<int>();
        for (var i = 1; i < source.Length; i++)
        {
            if (polarity ? source[i] < source[i-1] : source[i] > source[i-1])
            {
                polarity = !polarity;
                inflectionPoints.Add(i - 1);
            }
        }

        return inflectionPoints;
    }

    public (float? depth, int? index) ThermoDepth()
    {
        // Suggested by Georgiy Kirillin
        // https://github.com/GLEON/rLakeAnalyzer/blob/ef6de8c3e86d24f1d4190dbb1370afeb4e3181cb/R/thermo.depth.R
        // Removed null handling as this is handled in dictionary creation
        // Find peaks function made by hand instead of GIT version
  
        // Ensure adequate temperature differential
        if(maxTemp - minTemp < 1)
        {
            Debug.Log(string.Format("max: {0}, min: {1}", maxTemp, minTemp));
            Debug.Log("Temperature range less than cut-off");
            return(null, null);
        }
  
        // We can't determine anything with less than 3 measurements
        if(profileEntries.Count < 3)
        {
            Debug.Log("Less than three tmeperature measurements");
            return(null, null);
        }
  
        // Ensure depth profile is unique
        if(profileEntries.Count != uniqueDepths.Count)
        {
            Debug.Log("Non-unique depths");
            return(null, null);
        }
         
        // Calculate the first derivative of density
        List<double> drho_dz = new List<double>();
        for(int i = 0; i < profileEntries.Count - 1; i++)
        {
            ProfileEntry entry1 = profileEntries[i+1];
            ProfileEntry entry2 = profileEntries[i];

            drho_dz.Add((entry1.density - entry2.density) / (entry1.depth - entry2.depth));
        }
  
        // look for two distinct maximum slopes, lower one assumed to be seasonal
        int thermoInd = drho_dz.IndexOf(drho_dz.Max());
        double mDrhoZ = drho_dz[thermoInd];
        double thermoD = (profileEntries[thermoInd].depth + profileEntries[thermoInd+1].depth) / 2;
  
        if(thermoInd > 1 && thermoInd < (profileEntries.Count - 2))
        {  
            // if within range
            double Sdn = -(profileEntries[thermoInd+1].depth - profileEntries[thermoInd].depth)/
            (drho_dz[thermoInd+1] - drho_dz[thermoInd]);
            
            double Sup = (profileEntries[thermoInd].depth - profileEntries[thermoInd-1].depth)/
                (drho_dz[thermoInd] - drho_dz[thermoInd-1]);
                
            float upD  = profileEntries[thermoInd].depth;
            float dnD  = profileEntries[thermoInd+1].depth;
            
            if( !Double.IsInfinity(Sup) & !Double.IsInfinity(Sdn) )
            {
                thermoD = dnD*(Sdn/(Sdn+Sup))+upD*(Sup/(Sdn+Sup));
            }
        }

        float dRhoPerc = 0.15f; // in percentage max for unique thermocline step
        float Smin = 0.1f;
        double dRhoCut = Math.Max(dRhoPerc * mDrhoZ, Smin);

        List<int> inflectionPoints = GetInflectionPoints(drho_dz.ToArray());
        List<int> locs = new List<int>();
        List<double> pks = new List<double>();

        foreach (int point in inflectionPoints)
        {
            // Ensure it's not the last point (point + 1 doesn't exist)
            if (point != profileEntries.Count - 1)
            {
                // Ensure it's a peak inflection point and not a valley
                if (profileEntries[point].density < profileEntries[point + 1].density && profileEntries[point].density > dRhoCut)
                {
                    locs.Add(point);
                    pks.Add(drho_dz[point]);
                }
            }
        }

        int SthermoInd = int.MaxValue;
        double SthermoD = int.MaxValue;
        if(locs.Count == 0)
        {
            SthermoD = thermoD;
            SthermoInd = thermoInd;
        }
        else
        {
            mDrhoZ = pks[pks.Count - 1];
            SthermoInd = locs[pks.Count - 1];

            if(SthermoInd > (thermoInd + 1))
            {
                SthermoD = (profileEntries[SthermoInd].depth + profileEntries[SthermoInd+1].depth) / 2;
                
                if(SthermoInd > 1 && SthermoInd < (profileEntries.Count - 2))
                {
                    double Sdn = -(profileEntries[thermoInd+1].depth - profileEntries[thermoInd].depth)/
                    (drho_dz[thermoInd+1] - drho_dz[thermoInd]);
                    
                    double Sup = (profileEntries[thermoInd].depth - profileEntries[thermoInd-1].depth)/
                        (drho_dz[thermoInd] - drho_dz[thermoInd-1]);
                        
                    float upD  = profileEntries[thermoInd].depth;
                    float dnD  = profileEntries[thermoInd+1].depth;
                    
                    if( !Double.IsInfinity(Sup) & !Double.IsInfinity(Sdn) )
                    {
                        SthermoD = dnD*(Sdn/(Sdn+Sup))+upD*(Sup/(Sdn+Sup));
                    }
                }
            }
            else
            {
                SthermoD = thermoD;
                SthermoInd = thermoInd;
            }
        }
  
        if(SthermoD < thermoD)
        {
            SthermoD = thermoD;
            SthermoInd = thermoInd;
        }

        return ((float?)thermoD, thermoInd);
        
        // #Ok, which output was requested. Index or value
        // # seasonal or non-seasonal
        // if(index)
        // {
        //     if(seasonal)
        //     {
        //         return(SthermoInd)
        //     }
        //     else
        //     {
        //         return(thermoInd)
        //     }
        // }
        // else
        // {
        //     if(seasonal)
        //     {
        //     return(SthermoD)
        //     }
        //     else
        //     {
        //         return(thermoD)
        //     }
        // }
    }
}

public class ProfileEntry
{
    public float depth, temp;
    public double density;

    public ProfileEntry(float depth, float temp, double density)
    {
        this.depth = depth;
        this.temp = temp;
        this.density = density;
    }
}