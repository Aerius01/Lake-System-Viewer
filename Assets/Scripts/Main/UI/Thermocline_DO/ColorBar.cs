using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class ColorBar : MonoBehaviour
{
    private List<GameObject> gradientCells;
    public GameObject cellPrefab;
    private Gradient tempGradient, oxyGradient;

    public float upperVal = 25, lowerVal = 0;

    public void StartUp()
    {
        gradientCells = new List<GameObject>();

        for (int i = 0; i < 21; i++)
        {
            GameObject tempObject = Instantiate(cellPrefab, this.gameObject.transform.position, this.gameObject.transform.rotation, this.gameObject.transform) as GameObject;
            gradientCells.Add(tempObject);
            tempObject.GetComponent<GradientCell>().StartUp();
        }

        DefineGradients();
    }

    private void DefineGradients()
    {
        // Temperature
        tempGradient = new Gradient();

        GradientColorKey[] colorKeyTemp = new GradientColorKey[2];
        colorKeyTemp[0].color = Color.clear;
        colorKeyTemp[0].time = 0.0f;
        colorKeyTemp[1].color = Color.red;
        colorKeyTemp[1].time = 1.0f;

        GradientAlphaKey[] alphaKeyTemp = new GradientAlphaKey[2];
        alphaKeyTemp[0].alpha = 0.9f;
        alphaKeyTemp[0].time = 0.0f;
        alphaKeyTemp[1].alpha = 0.9f;
        alphaKeyTemp[1].time = 1.0f;

        tempGradient.SetKeys(colorKeyTemp, alphaKeyTemp);

        // Dissolved Oxygen
        oxyGradient = new Gradient();

        GradientColorKey[] colorKeyOxy = new GradientColorKey[2];
        colorKeyOxy[0].color = Color.clear;
        colorKeyOxy[0].time = 0.0f;
        colorKeyOxy[1].color = Color.blue;
        colorKeyOxy[1].time = 1.0f;

        GradientAlphaKey[] alphaKeyOxy = new GradientAlphaKey[2];
        alphaKeyOxy[0].alpha = 0.9f;
        alphaKeyOxy[0].time = 0.0f;
        alphaKeyOxy[1].alpha = 0.9f;
        alphaKeyOxy[1].time = 1.0f;

        oxyGradient.SetKeys(colorKeyOxy, alphaKeyOxy);   
    }

    public void UpdateCells(string colName, List<ThermoReading> readings)
    {
        for (int i = 0; i < gradientCells.Count; i++)
        {
            // Create mapping as there are 21 gradient cells but any number of depth readings
            float readingDepth = ThermoclineDOMain.instance.deepestReading / 21f * i;
            ThermoReading[] thermoArray = readings.ToArray();
            ThermoReading closestReading = thermoArray.OrderBy(tr => Math.Abs(tr.depth - readingDepth)).First();

            ThermoReading lowerReading = null;
            ThermoReading upperReading = null;
            List<ThermoReading> sortedList = readings.OrderBy(o=>o.depth).ToList();
            if (closestReading.depth > readingDepth)
            {
                upperReading = closestReading;
                int index = sortedList.IndexOf(upperReading);
                lowerReading = sortedList[index-1];
            }
            else
            {
                lowerReading = closestReading;
                int index = sortedList.IndexOf(lowerReading);
                upperReading = sortedList[index+1];
            }

            // Get the interpolated value
            float? value = null;
            if (colName == "temp") value = ((upperReading.temperature - lowerReading.temperature) / (upperReading.depth - lowerReading.depth)) * (readingDepth - lowerReading.depth) + lowerReading.temperature;
            else value = ((upperReading.oxygen - lowerReading.oxygen) / (upperReading.depth - lowerReading.depth)) * (readingDepth - lowerReading.depth) + lowerReading.oxygen;

            // Determine the individual cell color & set it
            GradientCell currentCell = gradientCells[i].GetComponent<GradientCell>();
            currentCell.SetDepth(readingDepth);
            
            if (value == null)
            {
                currentCell.SetColor(Color.black);
                currentCell.SetVal(null);
            }
            else
            {
                if (colName == "temp")
                {
                    currentCell.IsTemp(true);
                    currentCell.SetColor(tempGradient.Evaluate(((float)value - lowerVal) / (upperVal - lowerVal)));
                }
                else
                {
                    currentCell.IsTemp(false);
                    currentCell.SetColor(oxyGradient.Evaluate(((float)value - lowerVal) / (upperVal - lowerVal)));
                }

                currentCell.SetVal((float)value);
            }        
        }
    }
}
