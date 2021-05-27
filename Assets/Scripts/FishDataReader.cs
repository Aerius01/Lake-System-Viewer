using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;
using System;

public class FishDataReader : MonoBehaviour
{
    // TODO: sort data by DateTime for each fish ID
    // TODO: have the Dictionary hold the actual types and not the strings
    // TODO: rework the class to inherit from CSVReader

    public TextAsset csvFile;

    public bool hasHeaders, removeIdCol;

    public static Dictionary<string, string[,]> parsedData {get; private set;}

    private CSVReader csvReader = new CSVReader();

    // Start is called before the first frame update
    void Start()
    {
        string[,] stringGrid = csvReader.readCSVOutput2DString(csvFile.text, hasHeaders, removeIdCol);
        
        parsedData = createDataStructure(stringGrid);

        // Debug.Log(parsedData["59800"][3,0]);
        // Debug.Log(DateTime.Parse(parsedData["59800"][3,0]));

    }

    Dictionary<string, string[,]> createDataStructure(string[,] stringGrid){

        string[] stringKeys = SliceCol(stringGrid, 0).Distinct().ToArray();

        Dictionary<string, string[,]> dataSet = new Dictionary<string, string[,]>();
        foreach (string key in stringKeys)
        {
            string[,] positions = SliceMultipleColsByKey(stringGrid, 1, 4, key);
            dataSet.Add(key, positions);
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

    // string[,] SliceMultipleCols(string[,] array, int from, int to)
    // {
    //     string[,] arraySlice = new string[array.GetLength(1), to - from + 1];

    //     for (int y = 0; y < array.GetLength(1); y++)
    //     {
    //         for (int x = from; x <= to; x++ ){

    //             arraySlice[x - (to - from), y] = array[x, y];
    //         }
            
    //     }
    //     return arraySlice;
    // }

    string[,] SliceMultipleColsByKey(string[,] array, int from, int to, string key)
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
        string[,] arraySlice = new string[to - from + 1, cutSize];

        for (int y = 0; y < cutSize; y++)
        {
            for (int x = 0; x <= to - from; x++ ){
                arraySlice[x, y] = array[x + from, y + firstInstance].Trim(); 
            }
        }

        return arraySlice;
    }
}
