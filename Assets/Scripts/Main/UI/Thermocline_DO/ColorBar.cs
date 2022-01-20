using UnityEngine;
using System.Data;
using System.Collections.Generic;

public class ColorBar : MonoBehaviour
{
    private List<GameObject> gradientCells;
    public GameObject cellPrefab;
    private Gradient gradient;

    public float upperVal = 25, lowerVal = 0;

    private void Start()
    {
        gradientCells = new List<GameObject>();

        for (int i = 0; i < 21; i++)
        {
            GameObject tempObject = Instantiate(cellPrefab, this.gameObject.transform.position, this.gameObject.transform.rotation, this.gameObject.transform) as GameObject;
            gradientCells.Add(tempObject);
        }

        gradient = new Gradient();

        GradientColorKey[] colorKey = new GradientColorKey[2];
        colorKey[0].color = Color.blue;
        colorKey[0].time = 0.0f;
        colorKey[1].color = Color.red;
        colorKey[1].time = 1.0f;

        GradientAlphaKey[] alphaKey = new GradientAlphaKey[2];
        alphaKey[0].alpha = 1.0f;
        alphaKey[0].time = 0.0f;
        alphaKey[1].alpha = 1.0f;
        alphaKey[1].time = 1.0f;

        gradient.SetKeys(colorKey, alphaKey);
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
                currentCell.SetColor(gradient.Evaluate((float.Parse(currentData[i][colName].ToString()) - lowerVal) / (upperVal - lowerVal)));
                currentCell.SetVal(float.Parse(currentData[i][colName].ToString()));
            }        
        }
    }
}
