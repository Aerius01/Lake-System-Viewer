using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class FishDataReader : CSVReader
{
    // TODO: have this be part of the UI
    double minLong = 3404493.13224369, maxLong = 3405269.13224369, minLat = 5872333.13262316, maxLat = 5872869.13262316;
    public Dictionary<int, DataPointClass[]> parsedData {get; private set;}
    public static DateTime earliestTimeStamp {get; private set;}
    public static DateTime latestTimeStamp {get; private set;}
    public string[,] stringGrid;

    public void ReadData(GameObject referenceObject, bool Headers, bool IdCol) {
        hasHeaders = Headers;
        removeIdCol = IdCol;

        stringGrid = readCSVOutput2DString(referenceObject.GetComponent<LocalFileBrowser>().csvFile);

        // TODO: Combine this with the initial read operations for efficiency. HM have no dates, so only output2dString
        // Initialize time stamps
        earliestTimeStamp = latestTimeStamp = DateTime.Parse(stringGrid[4,0].Trim());

        for (int j = 0; j < stringGrid.GetLength(1); j++)
        {
            // Establish the bounds on the global data set timestamps
            if (DateTime.Parse(stringGrid[4,j].Trim()) > latestTimeStamp)
            {
                latestTimeStamp = DateTime.Parse(stringGrid[4,j].Trim());
            }
            if (DateTime.Parse(stringGrid[4,j].Trim()) < earliestTimeStamp)
            {
                earliestTimeStamp = DateTime.Parse(stringGrid[4,j].Trim());
            }
        }
    }

    public void parseFishData()
    {
        parsedData = createDataStructure(stringGrid);
    }

    Dictionary<int, DataPointClass[]> createDataStructure(string[,] stringGrid){

        string[] stringKeys = SliceCol(stringGrid, 0).Distinct().ToArray();

        Dictionary<int, DataPointClass[]> dataSet = new Dictionary<int, DataPointClass[]>();

        foreach (string key in stringKeys)
        {
            DataPointClass[] positions = SliceMultipleColsByKey(stringGrid, 1, 4, key);
            dataSet.Add(int.Parse(key.Trim()), positions);
        }
        
        return dataSet;
    }

    string[] SliceCol(string[,] array, int column)
    {
        string[] arraySlice = new string[array.GetLength(1)];

        for (int y = 0; y < array.GetLength(1); y++)
        {
            arraySlice[y] = array[column, y].Trim();
        }

        return arraySlice;
    }

    DataPointClass[] SliceMultipleColsByKey(string[,] array, int from, int to, string key)
    {
        int firstInstance = 0;
        while (array[0, firstInstance] != key){
            firstInstance++;
        }

        int lastInstance = firstInstance;
        while (array[0, lastInstance] == key && lastInstance != array.GetLength(1) - 1){
            lastInstance++;
        }

        if (lastInstance != array.GetLength(1) - 1){
            lastInstance -= 1; 
        }
        
        int cutSize = lastInstance - firstInstance + 1;
        List<DataPointClass> slicedList = new List<DataPointClass>();

        for (int y = 0; y < cutSize; y++)
        {
            DataPointClass record = new DataPointClass(
                convertStringLongValue(array[1, y + firstInstance]),
                convertStringLatValue(array[2, y + firstInstance]),
                - float.Parse(array[3, y + firstInstance].Trim()),
                DateTime.Parse(array[4, y + firstInstance].Trim())
            );

            slicedList.Add(record);
        }

        DataPointClass[] sorted = slicedList.OrderBy(f => f.obsTime).ToArray();

        return sorted;
    }

    public float convertStringLatValue(string stringLat){
        double doubleLat = double.Parse(stringLat.Replace("\"", "").Trim());

        if (doubleLat > maxLat || doubleLat < minLat)
        {
            throw new FormatException("The provided latitude is outside the range of the bounding box");
        }

        return (float)((heightMapYMax - heightMapYMin) * ((doubleLat - minLat) / (maxLat - minLat)) + heightMapYMin);
    }

    public float convertStringLongValue(string stringLong){
        double doubleLong = double.Parse(stringLong.Replace("\"", "").Trim());

        if (doubleLong > maxLong || doubleLong < minLong)
        {
            throw new FormatException("The provided longitude is outside the range of the bounding box");
        }

        return (float)((heightMapXMax - heightMapXMin) * ((doubleLong - minLong) / (maxLong - minLong)) + heightMapXMin);
    }
}
