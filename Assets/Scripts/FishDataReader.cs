using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using System.Linq;

public class FishDataReader : MonoBehaviour
{
    public TextAsset csvFile;

    public bool hasHeaders, removeIdRow;
    
    string[,] stringGrid;

    Dictionary<string, string[,]> parsedData;

    // Start is called before the first frame update
    void Start()
    {
        stringGrid = readCSV(csvFile.text);
        parsedData = createDataStructure(stringGrid);

        Debug.Log(parsedData["74000"][0,0]);

    }

    string[,] readCSV(string csvText)
    {
        // split the data on split line character
        string[] lines = csvText.Split("\n"[0]);

        // find the max number of columns
        int totalColumns = 0;
        int nullRowCounter = 0;
        for (int i = 0; i < lines.Length; i++)
        {
            string[] row = lines[i].Split(',');
            totalColumns = Mathf.Max(totalColumns, row.Length);

            if(nullInRow(row) == true){
                nullRowCounter++;
            }
        }

        // define output grid size dependent on whether headers exist
        string[,] outputGrid;
        if (hasHeaders == true){
            outputGrid = new string[totalColumns, lines.Length - nullRowCounter - 1];
        }
        else {
            outputGrid = new string[totalColumns, lines.Length - nullRowCounter];
        }

        // if a row has an empty string or a null, skip it
        for (int k = 0, y = 0; y < lines.Length; y++)
        {
            if (y == 0 && hasHeaders == true){
                y++;
            }

            string[] row = lines[y].Split(',');
            if(nullInRow(row) == true){
                continue;
            }
            else {
                for (int x = 0; x < row.Length; x++){
                    outputGrid[x, k] = row[x];
                } 
                k++;
            }
        }

        return outputGrid;
    }

    bool nullInRow(string[] currentRow){

        for (int x = 0; x < currentRow.Length; x++){
            if (string.IsNullOrEmpty(currentRow[x]) == true){
                return true;
            }
        }

        return false;
    }

    // Dictionary<int, List<Object>>
    Dictionary<string, string[,]> createDataStructure(string[,] stringGrid){

        string[] stringKeys = SliceCol(stringGrid, 0).Distinct().ToArray();

        Dictionary<string, string[,]> dataSet = new Dictionary<string, string[,]>();
        foreach (string key in stringKeys){
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
