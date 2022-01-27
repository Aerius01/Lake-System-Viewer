using UnityEngine;
using System.Data;
using System.Collections.Generic;

public class ColorBar : MonoBehaviour
{
    private List<GameObject> gradientCells;
    public GameObject cellPrefab;
    private Gradient tempGradient, oxyGradient;

    public float upperVal = 25, lowerVal = 0;

    private void Start()
    {
        gradientCells = new List<GameObject>();

        for (int i = 0; i < 21; i++)
        {
            GameObject tempObject = Instantiate(cellPrefab, this.gameObject.transform.position, this.gameObject.transform.rotation, this.gameObject.transform) as GameObject;
            gradientCells.Add(tempObject);
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

    public void UpdateCells(int index, string colName)
    {
        // Fetch the active data set
        DataRow[] currentData = LocalThermoclineData.thermoDict[LocalThermoclineData.uniqueTimeStamps[index]];

        for (int i = 0; i < gradientCells.Count; i++)
        {
            // Determine the individual cell color & set it
            GradientCell currentCell = gradientCells[i].GetComponent<GradientCell>();
            currentCell.SetDepth(float.Parse(currentData[i]["d"].ToString()));
            
            string stringValue = currentData[i][colName].ToString().Trim();
            if (string.IsNullOrEmpty(stringValue))
            {
                currentCell.SetColor(Color.black);
                currentCell.SetVal(null);
            }
            else
            {
                if (colName == "temp")
                {
                    currentCell.IsTemp(true);
                    currentCell.SetColor(tempGradient.Evaluate((float.Parse(currentData[i][colName].ToString()) - lowerVal) / (upperVal - lowerVal)));
                }
                else
                {
                    currentCell.IsTemp(false);
                    currentCell.SetColor(oxyGradient.Evaluate((float.Parse(currentData[i][colName].ToString()) - lowerVal) / (upperVal - lowerVal)));
                }

                currentCell.SetVal(float.Parse(currentData[i][colName].ToString()));
            }        
        }
    }
}
