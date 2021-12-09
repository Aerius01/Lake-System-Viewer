using UnityEngine;
using System.Data;
using System.Collections.Generic;
using System;

public class DataProcessor
{
    private LocalMeshData meshData;
    private LocalPositionData positionData;
    private LocalFishData fishData;
    private LocalThermoclineData thermoclineData;
    private LocalWeatherData weatherData;
    private LocalYSIData ysiData;
    private Dictionary<string, TextAsset> textAssetDict;

    public DataProcessor(Dictionary<string, TextAsset> textAssetDict)
    {
        this.textAssetDict = textAssetDict;
    }

    public void ReadData()
    {
        StringTable reader = new StringTable();

        // Parse all data sets into static classes
        meshData = new LocalMeshData(reader.parseTable(textAssetDict["meshData"].ToString()));
        positionData = new LocalPositionData(reader.parseTable(textAssetDict["positionData"].ToString()));
        fishData = new LocalFishData(reader.parseTable(textAssetDict["fishData"].ToString()));
        thermoclineData = new LocalThermoclineData(reader.parseTable(textAssetDict["thermoclineData"].ToString()));
        weatherData = new LocalWeatherData(reader.parseTable(textAssetDict["weatherData"].ToString()));
        ysiData = new LocalYSIData(reader.parseTable(textAssetDict["ysiData"].ToString()));
    }
}

public class LocalMeshData
{
    public static float waterLevel = -0.3f, maxDepth, minDepth;
    public static DataTable stringTable;
    public static int rowCount, columnCount;

    public LocalMeshData(DataTable table)
    {
        table.Rows[0].Delete();
        table.Columns.RemoveAt(0);
        table.AcceptChanges();

        stringTable = table;

        for (int row = 0; row < table.Rows.Count; row++)
        {
            for (int column = 0; column < table.Columns.Count; column++)
            {
                string stringValue = table.Rows[row][column].ToString().Trim();

                float value = float.Parse(stringValue);
                minDepth = Math.Min(minDepth, value);
                maxDepth = Math.Max(maxDepth, value);
            }
        }

        rowCount = table.Rows.Count;
        columnCount = table.Columns.Count;
    }
}

public class LocalPositionData
{
    private Dictionary<string, float> GISCoords;
    public static DateTime earliestDate, latestDate;
    public static DataTable stringTable;
    public static int rowCount, columnCount;

    public LocalPositionData(DataTable table)
    {
        table.Rows[0].Delete();
        table.AcceptChanges();

        List<string> columnNames = new List<string> {"id", "time", "x", "y", "d"};
        for (int c = 0; c < table.Columns.Count; c++)
        {
            table.Columns[c].ColumnName = columnNames[c];
        }

        stringTable = table;
        rowCount = table.Rows.Count;
        columnCount = table.Columns.Count;

        GISCoords = new Dictionary<string, float>() {
            {"MinLong", (float) 3404493.13224369},
            {"MaxLong", (float) 3405269.13224369},
            {"MinLat", (float) 5872333.13262316},
            {"MaxLat", (float) 5872869.13262316}
        };

        earliestDate = DateTime.MaxValue;
        latestDate = DateTime.MinValue;
        
        foreach (DataRow row in table.Rows)
        {
            row["x"] = convertStringLongValue("3" + row["x"].ToString());
            row["y"] = convertStringLatValue(row["y"].ToString());

            if (DateTime.Compare(earliestDate, DateTime.Parse(row["Time"].ToString())) > 0)
            {
                earliestDate = DateTime.Parse(row["Time"].ToString());
            }
            else if (DateTime.Compare(latestDate, DateTime.Parse(row["Time"].ToString())) < 0)
            {
                latestDate = DateTime.Parse(row["Time"].ToString());
            }
        }

        table.AcceptChanges();
    }

    public float convertStringLatValue(string stringLat)
    {
        double doubleLat = double.Parse(stringLat.Replace("\"", "").Trim());

        if (doubleLat > GISCoords["MaxLat"] || doubleLat < GISCoords["MinLat"])
        {
            throw new FormatException("The provided latitude is outside the range of the bounding box");
        }

        return (float)((LocalMeshData.rowCount) * ((doubleLat - GISCoords["MinLat"]) / (GISCoords["MaxLat"] - GISCoords["MinLat"])));
    }

    public float convertStringLongValue(string stringLong)
    {
        double doubleLong = double.Parse(stringLong.Replace("\"", "").Trim());

        if (doubleLong > GISCoords["MaxLong"] || doubleLong < GISCoords["MinLong"])
        {
            throw new FormatException("The provided longitude is outside the range of the bounding box");
        }

        return (float)((LocalMeshData.columnCount) * ((doubleLong - GISCoords["MinLong"]) / (GISCoords["MaxLong"] - GISCoords["MinLong"])));
    }
}

public class LocalFishData
{
    public static DataTable stringTable;
    public static int rowCount, columnCount;

    public LocalFishData(DataTable table)
    {
        table.Rows[0].Delete();
        table.AcceptChanges();

        stringTable = table;
        rowCount = table.Rows.Count;
        columnCount = table.Columns.Count;

        List<string> columnNames = new List<string> {"id", "surgeryTime", "speciesCode", "speciesName", "tl", "sl", "weight", "male", "firstPos", "lastPos"};
        for (int c = 0; c < table.Columns.Count; c++)
        {
            table.Columns[c].ColumnName = columnNames[c];
        }

        foreach (DataRow row in table.Rows)
        {
            // Binarize fish sex
            if (row["male"].ToString() == "male")
            {
                row["male"] = true;
            }
            else if (row["male"].ToString() == "female")
            {
                row["male"] = false;
            }
            else
            {
                row["male"] = null;
            }
        }

        table.AcceptChanges();

        foreach (var item in table.Rows[5].ItemArray)
        {
            Debug.Log(item.ToString());
        }
    }
}

public class LocalThermoclineData
{
    public static DataTable stringTable;
    public static int rowCount, columnCount;

    public LocalThermoclineData(DataTable table)
    {
        table.Rows[0].Delete();
        table.AcceptChanges();

        stringTable = table;
        rowCount = table.Rows.Count;
        columnCount = table.Columns.Count;

        List<string> columnNames = new List<string> {"d", "temp", "oxygen", "time"};
        for (int c = 0; c < table.Columns.Count; c++)
        {
            table.Columns[c].ColumnName = columnNames[c];
        }

        table.AcceptChanges();
    }
}

public class LocalWeatherData
{
    public static DataTable stringTable;
    public static int rowCount, columnCount;

    public LocalWeatherData(DataTable table)
    {
        table.Rows[0].Delete();
        table.AcceptChanges();

        stringTable = table;
        rowCount = table.Rows.Count;
        columnCount = table.Columns.Count;

        List<string> columnNames = new List<string> {"time", "windSpeed", "windDir", "temp", "humidity", "airPress", "precip"};
        for (int c = 0; c < table.Columns.Count; c++)
        {
            table.Columns[c].ColumnName = columnNames[c];
        }

        table.AcceptChanges();
    }
}

public class LocalYSIData
{
    public static DataTable stringTable;
    public static int rowCount, columnCount;

    public LocalYSIData(DataTable table)
    {
        table.Rows[0].Delete();
        table.AcceptChanges();

        stringTable = table;
        rowCount = table.Rows.Count;
        columnCount = table.Columns.Count;

        List<string> columnNames = new List<string> {"time", "temp", "odo", "odoSat"};
        for (int c = 0; c < table.Columns.Count; c++)
        {
            table.Columns[c].ColumnName = columnNames[c];
        }

        table.AcceptChanges();
    }
}