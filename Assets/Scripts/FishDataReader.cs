using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FishDataReader : MonoBehaviour
{
    public TextAsset csvFile;

    public bool hasHeaders, removeIdCol;
    
    string[,] stringGrid;

    Dictionary<string, string[,]> parsedData;

    private CSVReader csvReader = new CSVReader();

    // Start is called before the first frame update
    void Start()
    {
        stringGrid = csvReader.readCSVOutput2DString(csvFile.text, hasHeaders, removeIdCol);
        
        parsedData = createDataStructure(stringGrid);

        Debug.Log(parsedData["74000"][3,0]);
        // Debug.Log(csvReader.convertStringLongValue(csvReader.minLong.ToString()));
        // Debug.Log(csvReader.convertStringLatValue(csvReader.minLat.ToString()));
        // Debug.Log(csvReader.convertStringLatValue(csvReader.maxLong.ToString()));

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
            arraySlice[y] = array[column, y];
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
                arraySlice[x, y] = array[x + from, y + firstInstance]; 
            }
        }

        return arraySlice;
    }
}
