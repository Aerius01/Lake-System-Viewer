using UnityEngine;
using System.Data;
using System.Collections.Generic;
using System.Linq;
using System;

public class DataProcessor
{
    // private LocalMeshData meshData;
    // private LocalPositionData positionData;
    // private LocalFishData fishData;
    // private LocalThermoclineData thermoclineData;
    // private LocalWeatherData weatherData;
    // private LocalYSIData ysiData;
    private Dictionary<string, TextAsset> textAssetDict;
    private Texture2D NDVI;

    public DataProcessor(Dictionary<string, TextAsset> textAssetDict, Texture2D NDVI)
    {
        this.textAssetDict = textAssetDict;
        this.NDVI = NDVI;
    }

    public void ReadData()
    {
        StringTable reader = new StringTable();

        // Parse all data sets into static classes
        // meshData = new LocalMeshData(reader.parseTable(textAssetDict["meshData"].ToString()), NDVI);
        // positionData = new LocalPositionData(reader.parseTable(textAssetDict["positionData"].ToString()));
        // fishData = new LocalFishData(reader.parseTable(textAssetDict["fishData"].ToString()));
        // thermoclineData = new LocalThermoclineData(reader.parseTable(textAssetDict["thermoclineData"].ToString()));
        // weatherData = new LocalWeatherData(reader.parseTable(textAssetDict["weatherData"].ToString()));
        // ysiData = new LocalYSIData(reader.parseTable(textAssetDict["ysiData"].ToString()));
    }
}

// public class LocalMeshData
// {
//     public static float maxDepth, minDepth, maxDiff, lakeDepthOffset = float.MinValue, ndviMax, ndviMin;
//     public static DataTable stringTable;
//     public static int rowCount, columnCount, resolution;
//     public static Vector3 meshCenter;
//     public static Texture2D NDVI;
//     public static Dictionary<string, int> cutoffs;

//     public LocalMeshData(DataTable table, Texture2D NDVI)
//     {
//         table.Rows[0].Delete();
//         table.Columns.RemoveAt(0);
//         table.AcceptChanges();

//         stringTable = table;
//         LocalMeshData.NDVI = NDVI;

//         float prevVal = 0f;
//         for (int row = 0; row < table.Rows.Count; row++)
//         {
//             for (int column = 0; column < table.Columns.Count; column++)
//             {
//                 float value = float.Parse(table.Rows[row][column].ToString().Trim());
//                 minDepth = Math.Min(minDepth, value);
//                 maxDepth = Math.Max(maxDepth, value);

//                 Check if we're at a lake/surroundings boundary (cross-pattern check), then record gap
//                 cliff-like feature between ground and lake on the boundary
//                 if (value == 0f && prevVal != 0f) { lakeDepthOffset = Math.Max(lakeDepthOffset, prevVal); } // left
//                 else if (value != 0f && prevVal == 0f) { lakeDepthOffset = Math.Max(lakeDepthOffset, value); } // right
//                 else if (row != 0 && float.Parse(table.Rows[row - 1][column].ToString().Trim()) == 0f && value != 0f)
//                 { lakeDepthOffset = Math.Max(lakeDepthOffset, value); } // above
//                 else if (row != table.Rows.Count - 1 && float.Parse(table.Rows[row + 1][column].ToString().Trim()) != 0f && value == 0f)
//                 { lakeDepthOffset = Math.Max(lakeDepthOffset, float.Parse(table.Rows[row + 1][column].ToString().Trim())); } // below
//             }
//         }
        
//         Offset lake level based on minimum gap
//         lakeDepthOffset = Math.Abs(lakeDepthOffset);
//         minDepth += lakeDepthOffset;
//         maxDiff = Math.Abs(maxDepth - minDepth);

//         Determine lake location within larger mesh
//         rowCount = table.Rows.Count;
//         columnCount = table.Columns.Count;

//         Get the resolution
//         int maxDim = Mathf.Max(rowCount, columnCount);
//         for (int i = 0; i < 12; i++)
//         {
//             if (Mathf.Pow(2, i) >= maxDim)
//             {
//                 resolution = Mathf.RoundToInt(Mathf.Pow(2, i)) + 1;
//                 break;
//             }
//         }

//         cutoffs = new Dictionary<string, int>
//         {
//             {"minHeight", Mathf.FloorToInt((resolution - rowCount) / 2)},
//             {"maxHeight", Mathf.FloorToInt((resolution - rowCount) / 2 + rowCount)},
//             {"minWidth", Mathf.FloorToInt((resolution - columnCount) / 2)},
//             {"maxWidth", Mathf.FloorToInt((resolution - columnCount) / 2 + columnCount)},
//         };

//         meshCenter = new Vector3((cutoffs["minWidth"] + cutoffs["maxWidth"])/ 2, 0f, (cutoffs["minHeight"] + cutoffs["maxHeight"]) / 2);

//         Create NDVI table
//         ndviMax = float.MinValue;
//         ndviMin = float.MaxValue;
//         for (int i = 0; i < NDVI.height; i++)
//         {
//             for (int j = 0; j < NDVI.width; j++)
//             {
//                 Color temp = NDVI.GetPixel(i, j);
//                 ndviMax = Math.Max(temp.r, ndviMax);
//                 ndviMin = Math.Min(temp.r, ndviMin);
//             }
//         }
//     }
// }

// public class LocalPositionData
// {
//     public static int rowCount {get; private set;}
//     public static int columnCount {get; private set;}
//     public static Dictionary<int, DataPointClass[]> positionDict {get; private set;}
//     public static int[] uniquefishIDs {get; private set;}
//     public static DateTime earliestDate {get; private set;}
//     public static DateTime latestDate {get; private set;}

//     private Dictionary<string, double> GISCoords;
    
//     public LocalPositionData(DataTable table)
//     {
//         table.Rows[0].Delete();
//         table.AcceptChanges();

//         List<string> columnNames = new List<string> {"id", "time", "x", "y", "d"};
//         for (int c = 0; c < table.Columns.Count; c++)
//         {
//             table.Columns[c].ColumnName = columnNames[c];
//         }

//         rowCount = table.Rows.Count;
//         columnCount = table.Columns.Count;

//         GISCoords = new Dictionary<string, double>() {
//             {"MinLong", (double) 3404493.13224369},
//             {"MaxLong", (double) 3405269.13224369},
//             {"MinLat", (double) 5872333.13262316},
//             {"MaxLat", (double) 5872869.13262316}
//         };

//         earliestDate = DateTime.MaxValue;
//         latestDate = DateTime.MinValue;
        
//         List<string> uniqueStrIDs = new List<string>();
//         foreach (DataRow row in table.Rows)
//         {
//             row["x"] = convertStringLongValue("3" + row["x"].ToString());
//             row["y"] = convertStringLatValue(row["y"].ToString());

//             if (DateTime.Compare(earliestDate, DateTime.Parse(row["Time"].ToString())) > 0)
//             {
//                 earliestDate = DateTime.Parse(row["Time"].ToString());
//             }
//             else if (DateTime.Compare(latestDate, DateTime.Parse(row["Time"].ToString())) < 0)
//             {
//                 latestDate = DateTime.Parse(row["Time"].ToString());
//             }

//             // Assemble unique fish IDs
//             if (!uniqueStrIDs.Contains(row["id"]))
//             {
//                 uniqueStrIDs.Add(row["id"].ToString());
//             }
//         }

//         table.AcceptChanges();

//         // Assemble a dictionary keyed by, and a sorted array of, those unique IDs
//         positionDict = new Dictionary<int, DataPointClass[]>();
//         uniquefishIDs = new int[uniqueStrIDs.Count];

//         for (int i = 0; i < uniqueStrIDs.Count; i++)
//         {
//             // Find the relevant rows
//             string item = uniqueStrIDs[i];
//             string searchExp = string.Format("id = '{0}'", item);
//             DataRow[] foundRows = table.Select(searchExp);

//             // Create a DataTable from the results sorted by timestamp
//             DataTable sortingSubset = new DataTable();
//             foreach (string name in columnNames)
//             {
//                 sortingSubset.Columns.Add(name, typeof(String)); 
//             }

//             foreach (DataRow row in foundRows)
//             {
//                 sortingSubset.ImportRow(row); 
//             }

//             sortingSubset.DefaultView.Sort = "time";
//             sortingSubset = sortingSubset.DefaultView.ToTable();

//             // Parse into the DataPointClass array
//             DataPointClass[] points = new DataPointClass[sortingSubset.Rows.Count];
//             for (int j = 0; j < sortingSubset.Rows.Count; j++)
//             {
//                 try
//                 {
//                     DataPointClass record = new DataPointClass(
//                         float.Parse(sortingSubset.Rows[j]["x"].ToString()),
//                         LocalMeshData.rowCount - float.Parse(sortingSubset.Rows[j]["y"].ToString()),
//                         - float.Parse(sortingSubset.Rows[j]["D"].ToString()),
//                         DateTime.Parse(sortingSubset.Rows[j]["Time"].ToString())
//                     );

//                     points[j] = record;
//                 }
//                 catch
//                 {
//                     Debug.Log("Unsuccessful parsing of position data");
//                 }
//             }

//             int id = int.Parse(item);
//             uniquefishIDs[i] = id;
//             positionDict.Add(id, points);
//         }

//         Array.Sort(uniquefishIDs);
//     }

//     private float convertStringLatValue(string stringLat)
//     {
//         double doubleLat = double.Parse(stringLat.Replace("\"", "").Trim());

//         if (doubleLat > GISCoords["MaxLat"] || doubleLat < GISCoords["MinLat"])
//         {
//             throw new FormatException("The provided latitude is outside the range of the bounding box");
//         }

//         return (float)((LocalMeshData.rowCount) * ((doubleLat - GISCoords["MinLat"]) / (GISCoords["MaxLat"] - GISCoords["MinLat"])));
//     }

//     private float convertStringLongValue(string stringLong)
//     {
//         double doubleLong = double.Parse(stringLong.Replace("\"", "").Trim());

//         if (doubleLong > GISCoords["MaxLong"] || doubleLong < GISCoords["MinLong"])
//         {
//             throw new FormatException("The provided longitude is outside the range of the bounding box");
//         }

//         return (float)((LocalMeshData.columnCount) * ((doubleLong - GISCoords["MinLong"]) / (GISCoords["MaxLong"] - GISCoords["MinLong"])));
//     }
// }

// public class LocalFishData
// {
//     public static int rowCount {get; private set;}
//     public static int columnCount {get; private set;}
//     public static Dictionary<int, DataRow> fishDict {get; private set;}
//     public static int[] uniquefishIDs {get; private set;}

//     public LocalFishData(DataTable table)
//     {
//         table.Rows[0].Delete();
//         table.AcceptChanges();

//         rowCount = table.Rows.Count;
//         columnCount = table.Columns.Count;

//         List<string> columnNames = new List<string> {"id", "surgeryTime", "speciesCode", "speciesName", "tl", "sl", "weight", "male", "firstPos", "lastPos"};
//         for (int c = 0; c < table.Columns.Count; c++)
//         {
//             table.Columns[c].ColumnName = columnNames[c];
//         }

//         foreach (DataRow row in table.Rows)
//         {
//             // Binarize fish sex
//             if (row["male"].ToString() == "male")
//             {
//                 row["male"] = true;
//             }
//             else if (row["male"].ToString() == "female")
//             {
//                 row["male"] = false;
//             }
//             else
//             {
//                 row["male"] = null;
//             }
//         }

//         table.AcceptChanges();

//         // Assemble unique fish IDs
//         List<string> uniqueStrIDs = new List<string>();
//         foreach (DataRow row in table.Rows)
//         {
//             if (!uniqueStrIDs.Contains(row["id"]))
//             {
//                 uniqueStrIDs.Add(row["id"].ToString());
//             }
//         }

//         // Assemble a dictionary keyed by, and a sorted array of, those unique IDs
//         fishDict = new Dictionary<int, DataRow>();
//         uniquefishIDs = new int[uniqueStrIDs.Count];

//         for (int i = 0; i < uniqueStrIDs.Count; i++)
//         {
//             string item = uniqueStrIDs[i];
//             string searchExp = string.Format("id = '{0}'", item);
//             DataRow[] foundRows = table.Select(searchExp);

//             try
//             {
//                 int id = int.Parse(item);
//                 uniquefishIDs[i] = id;
//                 // Choose the first entry, which should be the only one
//                 fishDict.Add(id, foundRows[0]);
//             }
//             catch
//             {
//                 Debug.Log("Unsuccessful parsing of fish data");
//             }
//         }

//         Array.Sort(uniquefishIDs);
//     }
// }

// public class LocalThermoclineData
// {
//     public static int rowCount {get; private set;}
//     public static int columnCount {get; private set;}
//     public static Dictionary<DateTime, DataRow[]> thermoDict {get; private set;}
//     public static DateTime[] uniqueTimeStamps {get; private set;}

//     public LocalThermoclineData(DataTable table)
//     {
//         table.Rows[0].Delete();
//         table.AcceptChanges();

//         rowCount = table.Rows.Count;
//         columnCount = table.Columns.Count;

//         List<string> columnNames = new List<string> {"d", "temp", "oxygen", "time"};
//         for (int c = 0; c < table.Columns.Count; c++)
//         {
//             table.Columns[c].ColumnName = columnNames[c];
//         }

//         table.AcceptChanges();
        
//         // Assemble unique timestamps
//         DataTable stringTable = table.Clone();
//         stringTable.Columns["time"].DataType = typeof(DateTime);
//         List<DateTime> uniqueTSList = new List<DateTime>();
//         foreach (DataRow row in table.Rows)
//         {
//             try
//             {
//                 stringTable.ImportRow(row);

//                 // Assemble unique timestamps
//                 if (!uniqueTSList.Contains(DateTime.Parse(row["time"].ToString())))
//                 {
//                     uniqueTSList.Add(DateTime.Parse(row["time"].ToString()));
//                 }
//             }
//             catch { continue; }
//         }

//         stringTable.AcceptChanges();
//         uniqueTSList.Sort();

//         // Restrict active list to limits of position data
//         DateTime[] uniqueTSArray = uniqueTSList.ToArray();

//         int lowerIndex = Array.BinarySearch(uniqueTSArray, LocalPositionData.earliestDate);
//         lowerIndex = lowerIndex < 0 ? Math.Abs(lowerIndex) - 2 : lowerIndex;

//         int upperIndex = Array.BinarySearch(uniqueTSArray, LocalPositionData.latestDate);
//         upperIndex = upperIndex < 0 ? Math.Abs(upperIndex) - 1 : upperIndex;

//         List<DateTime> uniqueTSListReduced = new List<DateTime>();
//         for (int i = lowerIndex; i <= upperIndex; i++) uniqueTSListReduced.Add(uniqueTSArray[i]);

//         uniqueTimeStamps = uniqueTSListReduced.ToArray();

//         // Assemble a dictionary keyed by, and a sorted array of, those unique timestamps
//         thermoDict = new Dictionary<DateTime, DataRow[]>();
        
//         for (int i = 0; i < uniqueTimeStamps.Length; i++)
//         {
//             DateTime timeStamp = uniqueTimeStamps[i];

//             string searchExp = string.Format("time = #{0}#", timeStamp);
//             DataRow[] foundRows = stringTable.Select(searchExp);
//             thermoDict.Add(timeStamp, foundRows);
//         }
//     }
// }

// public class LocalWeatherData
// {
//     public static DataTable stringTable;
//     public static int rowCount, columnCount;
//     public static DateTime[] uniqueTimeStamps {get; private set;}

//     public LocalWeatherData(DataTable table)
//     {
//         table.Rows[0].Delete();
//         table.AcceptChanges();

//         rowCount = table.Rows.Count;
//         columnCount = table.Columns.Count;

//         List<string> columnNames = new List<string> {"time", "windSpeed", "windDir", "temp", "humidity", "airPress", "precip"};
//         for (int c = 0; c < table.Columns.Count; c++)
//         {
//             table.Columns[c].ColumnName = columnNames[c];
//         }

//         table.AcceptChanges();

//         stringTable = table.Clone();
//         stringTable.Columns["time"].DataType = typeof(DateTime);
//         List<DateTime> uniqueTSList = new List<DateTime>();
//         foreach (DataRow row in table.Rows)
//         {
//             try
//             {
//                 stringTable.ImportRow(row);

//                 // Assemble unique timestamps
//                 if (!uniqueTSList.Contains(DateTime.Parse(row["time"].ToString())))
//                 {
//                     uniqueTSList.Add(DateTime.Parse(row["time"].ToString()));
//                 }
//             }
//             catch { continue; }
//         }

//         stringTable.AcceptChanges();
//         uniqueTSList.Sort();

//         // Restrict active list to limits of position data
//         DateTime[] uniqueTSArray = uniqueTSList.ToArray();

//         int lowerIndex = Array.BinarySearch(uniqueTSArray, LocalPositionData.earliestDate);
//         lowerIndex = lowerIndex < 0 ? Math.Abs(lowerIndex) - 2 : lowerIndex;

//         int upperIndex = Array.BinarySearch(uniqueTSArray, LocalPositionData.latestDate);
//         upperIndex = upperIndex < 0 ? Math.Abs(upperIndex) - 1 : upperIndex;

//         List<DateTime> arrayList = new List<DateTime>();
//         for (int i = lowerIndex; i <= upperIndex; i++) arrayList.Add(uniqueTSArray[i]);

//         // Assemble array from reduced selection
//         uniqueTimeStamps = arrayList.ToArray();
//     }
// }

// public class LocalYSIData
// {
//     public static DataTable stringTable;
//     public static int rowCount, columnCount;

//     public LocalYSIData(DataTable table)
//     {
//         table.Rows[0].Delete();
//         table.AcceptChanges();

//         stringTable = table;
//         rowCount = table.Rows.Count;
//         columnCount = table.Columns.Count;

//         List<string> columnNames = new List<string> {"time", "temp", "odo", "odoSat"};
//         for (int c = 0; c < table.Columns.Count; c++)
//         {
//             table.Columns[c].ColumnName = columnNames[c];
//         }

//         table.AcceptChanges();
//     }
// }