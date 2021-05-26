using UnityEngine;
using System;

public class CSVReader
{
    int totalColumns, totalRows, intRemoveIdCol, intHasHeaders;

    // TODO: automatically read this information --> sequential uploading of files, ie HeightMap first
    public int heightMapXMin = 0, heightMapXMax = 776, heightMapYMin = 0, heightMapYMax = 536;

    public double minLong = 3404493.13224369, maxLong = 3405269.13224369, minLat = 5872333.13262316, maxLat = 5872869.13262316;

    private string[] baseReadOperations(string csvText, bool hasHeaders, bool removeIdCol){
        // split the data on split line character
        string[] lines = csvText.Split("\n"[0]);

        // find the max number of columns
        totalColumns = lines[0].Split(',').Length;
        totalRows = lines.Length;

        for (int y = 0; y < lines.Length; y++)
        {
            string[] row = lines[y].Split(',');

            // find how many dead entries per row
            int x = row.Length - 1;
            while (string.IsNullOrEmpty(row[x].Replace("\"", "").Replace(" ", "")) || string.IsNullOrWhiteSpace(row[x].Replace("\"", "").Replace(" ", "")))
            {
                x--;

                // a negative index means the entire row is null
                if (x < 0){
                    totalRows--;
                    break;
                }
                else
                {
                    string tempChar = row[x].Replace("\"", "").Replace(" ", "");
                
                    // if the new position holds a non-trivial value, this is the end of the row
                    if (!string.IsNullOrEmpty(tempChar) && !string.IsNullOrWhiteSpace(tempChar))
                    {
                        totalColumns = Mathf.Min(totalColumns, x+1);
                    }
                }
            }
        }

        // Convert the boolean values to integers
        if (hasHeaders){
            intHasHeaders = 1;
        }
        else
        {
            intHasHeaders = 0;
        }
        
        if (removeIdCol){
            intRemoveIdCol = 1;
        }
        else
        {
            intRemoveIdCol = 0;
        }

        return (lines);

    }

    public string[,] readCSVOutput2DString(string csvText, bool hasHeaders, bool removeIdCol)
    {
        string[] lines = baseReadOperations(csvText, hasHeaders, removeIdCol);

        // define output grid size dependent on whether headers and IDs exist
        string[,] outputGrid = new string[totalColumns - intRemoveIdCol, totalRows - intHasHeaders];

        // if a row has an empty string or a null, flag it with a much higher value of 99999
        for (int y = 0 + intHasHeaders; y < totalRows; y++)
        {
            string[] row = lines[y].Split(',');

            for (int x = 0 + intRemoveIdCol; x < totalColumns; x++)
            {
                string tempChar = row[x].Replace("\"", "").Replace(" ", "");
                if(string.IsNullOrEmpty(tempChar) || string.IsNullOrWhiteSpace(tempChar))
                {
                    outputGrid[x - intRemoveIdCol, y - intHasHeaders] = "99999";
                }
                else
                {
                    outputGrid[x - intRemoveIdCol, y - intHasHeaders] = tempChar;
                }
            }
        }

        return outputGrid;
    }

    // http://codesaying.com/unity-parse-excel-in-unity3d/
    public (Vector3[], int, int) readCSVOutputVector3(string csvText, bool hasHeaders, bool removeIdCol)
    {
        string[] lines = baseReadOperations(csvText, hasHeaders, removeIdCol);

        // define output Vector3 size dependent on whether headers and IDs exist
        Vector3[] vertices = new Vector3[(totalColumns - intRemoveIdCol) * (totalRows - intHasHeaders)];

        // TODO: modularize everything below this tag, it's identical to readCSVOutput2DString, minus exception handling
        for (int index = 0, z = 0 + intHasHeaders; z < totalRows; z++)
        {
            string[] row = lines[z].Split(',');

            for (int x = 0 + intRemoveIdCol; x < totalColumns; x++)
            {
                string tempChar = row[x].Replace("\"", "").Replace(" ", "");
                if(string.IsNullOrEmpty(tempChar) || string.IsNullOrWhiteSpace(tempChar))
                {
                    vertices[index] = new Vector3(x, 99999f, z);
                }
                else
                {
                    try
                    {
                        vertices[index] = new Vector3(x, float.Parse(tempChar), z);
                    }
                    catch (FormatException e)
                    {
                        throw new FormatException("Error converting a value in the data. Check that there are only numerical values being passed.", e);
                    }
                }

                index++;
            }
        }

        return (vertices, totalColumns - intRemoveIdCol, totalRows - intHasHeaders);
    }

    public float convertStringLatValue(string stringLat){
        double doubleLat = double.Parse(stringLat.Replace("\"", "").Replace(" ", ""));

        if (doubleLat > maxLat || doubleLat < minLat)
        {
            throw new FormatException("The provided latitude is outside the range of the bounding box");
        }

        return (float)((heightMapYMax - heightMapYMin) * ((doubleLat - minLat) / (maxLat - minLat)) + heightMapYMin);
    }

    public float convertStringLongValue(string stringLong){
        double doubleLong = double.Parse(stringLong.Replace("\"", "").Replace(" ", ""));

        if (doubleLong > maxLong || doubleLong < minLong)
        {
            throw new FormatException("The provided longitude is outside the range of the bounding box");
        }

        return (float)((heightMapXMax - heightMapXMin) * ((doubleLong - minLong) / (maxLong - minLong)) + heightMapXMin);
    }

}