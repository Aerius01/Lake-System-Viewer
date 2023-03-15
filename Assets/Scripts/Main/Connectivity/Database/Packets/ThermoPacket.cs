using System;
using System.Collections.Generic;

public class ThermoPacket
{
    public DateTime timestamp {get; private set;}
    public DateTime? nextTimestamp {get; private set;}
    public List<ThermoReading> readings {get; private set;}
    public float? maxTemp { get; private set; }
    public float? minTemp { get; private set; }

    public ThermoPacket(DateTime timestamp, DateTime? nextTimestamp, List<ThermoReading> readings)
    {
        this.timestamp = timestamp;
        this.nextTimestamp = nextTimestamp;
        this.readings = readings;
        this.maxTemp = null;
        this.minTemp = null;

        foreach (ThermoReading reading in readings)
        {
            if (reading.temperature != null)
            {
                if (this.maxTemp == null) { this.maxTemp = reading.temperature; }
                else this.maxTemp = reading.temperature > this.maxTemp ? reading.temperature : this.maxTemp;

                if (this.minTemp == null) { this.minTemp = reading.temperature; }
                else this.minTemp = reading.temperature < this.minTemp ? reading.temperature : this.minTemp;
            }
        }
    }
}

public class ThermoReading
{
    public float depth {get; private set;}
    public float? temperature {get; private set;}
    public float? oxygen {get; private set;}
    public double? density {get; private set;}

    public ThermoReading(float depth, float? temperature, float? oxygen)
    {
        this.depth = depth;
        this.temperature = temperature;
        this.oxygen = oxygen;
        this.density = WaterDensity(temperature);
    }

    private double? WaterDensity(float? temp)
    {
        if (temp == null) return null;
        else
        {
            // The CIPM Formula, assuming atmospheric pressure
            // https://metgen.pagesperso-orange.fr/metrologieen19.htm

            // Corrective factor for dissolved air
            double d1 = -4.612 * Math.Pow(10, -3); // kg/m3
            double d2 = 0.106 * Math.Pow(10, -3); // kg/m3 · °C^-1
            double Cw = d1 + d2 * (float)temp; // kg/m3

            // Density Calculation
            double a1 = -3.983035; // °C	 	
            double a2 = 301.797; // °C	 	
            double a3 = 522528.9; // °C	
            double a4 = 69.34881; // °C	 	
            double a5 = 999.974950; // kg/m3

            double density = a5 * (1 - (Math.Pow(((float)temp + a1), 2) * ((float)temp + a2)) / (a3 * ((float)temp + a4))); // kg/m3

            return (Cw + density);
        }
    }
}