using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class DataPointClass
{
    public float x { get; set; }
    public float y { get; set; }
    public float z { get; set; }
    public DateTime obsTime { get; set; }
}

public class FishDataReader : CSVReader
{
    double minLong = 3404493.13224369, maxLong = 3405269.13224369, minLat = 5872333.13262316, maxLat = 5872869.13262316;
    
    public static Dictionary<int, DataPointClass[]> parsedData {get; private set;}

    public string[,] stringGrid;

    public static DateTime earliestTimeStamp {get; private set;}
    public static DateTime latestTimeStamp {get; private set;}

    public void ReadFishData() {
        stringGrid = readCSVOutput2DString(csvFile.text);
        earliestTimeStamp = latestTimeStamp = DateTime.Parse(stringGrid[4,0].Trim());
    }


    public void ConvertStructure()
    {
        parsedData = createDataStructure(stringGrid);

        // foreach (int key in parsedData.Keys)
        // {
        //     Debug.Log(key);
        // }

        // for (int i = 0; i < 4; i++){
        //     Debug.Log(String.Format("{0}, {1}, {2}, {3}", parsedData[59800][i].x, parsedData[59800][i].y, parsedData[59800][i].z, parsedData[59800][i].obsTime));
        // }
        // //Debug.Log(DateTime.Parse(parsedData["59800"][3,0]));

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
            DataPointClass record = new DataPointClass();

            record.x = convertStringLongValue(array[1, y + firstInstance]);
            record.y = convertStringLatValue(array[2, y + firstInstance]);
            record.z = - float.Parse(array[3, y + firstInstance].Trim());

            DateTime parsedDate = DateTime.Parse(array[4, y + firstInstance].Trim());
            record.obsTime = parsedDate;
            slicedList.Add(record);

            // Establish the bounds on the global data set timestamps
            if (parsedDate > latestTimeStamp)
            {
                latestTimeStamp = parsedDate;
            }
            if (parsedDate < earliestTimeStamp)
            {
                earliestTimeStamp = parsedDate;
            }
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
