using System.Collections.Generic;
using System.Threading;
using System;
using System.Data;
using UnityEngine;
using System.Linq;

public class PositionUploadTable: UploadTable
{
    private DateTime earliestTS, latestTS;
    private MeshData meshData;
    static public DateTime startCutoff, endCutoff;
    static volatile public bool checkOpComplete;
    static public bool applyDateFilter, applyGISConversion, setTableComplete;
    private List<int> uniqueIDs;
    public DateTime[] dateFilter;


    public PositionUploadTable(DataTable uploadTable, GameObject statsFrame) : base(uploadTable, statsFrame)
    {

    }

    public override void SetTable(List<int> currentClickList)
    {
        setTableComplete = false;
        base.SetTable(currentClickList);

        // Remove unnamed columns
        AttributeColumnNames(uploadTable, removeUnnamed: true);

        // Create mappings
        List<int> nullRows = new List<int>();
        foreach (int[] entry in nullList)
        {
            if (!nullRows.Contains(entry[0]))
            {
                nullRows.Add(entry[0]);
            }
        }

        if (applyDateFilter)
        {
            dateFilter = new DateTime[2] {startCutoff, endCutoff};
        }

        if (applyGISConversion)
        {
            meshData = GameObject.Find("MeshReader").GetComponent<MeshData>();
        }

        // Apply date and null filters
        Thread filtersThread = new Thread(() => {
            for (int r = uploadTable.Rows.Count - 1; r > 0; r--)
            {
                if (nullRows.Contains(r)) // null or NaN values
                {
                    uploadTable.Rows[r].Delete();
                }
                else
                {
                    bool rowDeleted = false;

                    if (applyDateFilter) // filter time values
                    {
                        if (DateTime.Compare(DateTime.Parse(uploadTable.Rows[r]["Time"].ToString()), dateFilter[0]) < 0 || DateTime.Compare(dateFilter[1], DateTime.Parse(uploadTable.Rows[r]["Time"].ToString())) > 0)
                        {
                            uploadTable.Rows[r].Delete();
                            rowDeleted = true;
                        }
                    }

                    if (!rowDeleted && applyGISConversion)
                    {
                        uploadTable.Rows[r]["x"] = (convertStringLongValue(uploadTable.Rows[r]["x"].ToString())).ToString();
                        uploadTable.Rows[r]["y"] = (convertStringLatValue(uploadTable.Rows[r]["y"].ToString())).ToString();
                    }
                }
            }

            uploadTable.AcceptChanges();
            setTableComplete = true;
        });

        filtersThread.Start();
    }

    public DataTable AttributeColumnNames(DataTable table, bool removeUnnamed = false)
    {
        // Apply column names to the datable so we know what is what
        for (int c = 0; c < table.Columns.Count; c++)
        {
            if (viewPort.GetColumnName(c) != null)
            {
                // Debug.Log("Attempting col ID: " + c);
                table.Columns[c].ColumnName = viewPort.GetColumnName(c);
            }      
            else
            {
                // Debug.Log("Failed name attribution: " + c);
                if (removeUnnamed)
                {
                    table.Columns.RemoveAt(c);
                }
            }      
        }

        table.AcceptChanges();
        return table;
    }

    public List<int> CheckColumnFormatting(DataTable namedColumnsTable, Dictionary<string, Type> types)
    {
        checkOpComplete = false;
        
        List<int> issueList = new List<int>();
        foreach (DataRow row in namedColumnsTable.Rows)
        {
            if (namedColumnsTable.Rows.IndexOf(row) >= rowsToRemove)
            {
                foreach (string columnName in types.Keys)
                {
                    try
                    {
                        Convert.ChangeType(row[columnName].ToString(), (types[columnName]));
                    }
                    catch(InvalidCastException)
                    {
                        // Add row to problem list and move on
                        issueList.Add(namedColumnsTable.Rows.IndexOf(row));
                        break;
                    }
                }
            }
        }

        checkOpComplete = true;
        Debug.Log("Done check");
        return issueList;
    }

    public float convertStringLatValue(string stringLat)
    {
        double doubleLat = double.Parse(stringLat.Replace("\"", "").Trim());

        if (doubleLat > PositionUploader.GISBox["MaxLat"] || doubleLat < PositionUploader.GISBox["MinLat"])
        {
            throw new FormatException("The provided latitude is outside the range of the bounding box");
        }

        return (float)((meshData.rowCount) * ((doubleLat - PositionUploader.GISBox["MinLat"]) / (PositionUploader.GISBox["MaxLat"] - PositionUploader.GISBox["MinLat"])));
    }

    public float convertStringLongValue(string stringLong)
    {
        double doubleLong = double.Parse(stringLong.Replace("\"", "").Trim());

        if (doubleLong > PositionUploader.GISBox["MaxLong"] || doubleLong < PositionUploader.GISBox["MinLong"])
        {
            throw new FormatException("The provided longitude is outside the range of the bounding box");
        }

        return (float)((meshData.columnCount) * ((doubleLong - PositionUploader.GISBox["MinLong"]) / (PositionUploader.GISBox["MaxLong"] - PositionUploader.GISBox["MinLong"])));
    }
}