using System.Collections.Generic;
using System;
using System.Data;
using UnityEngine;
using System.Linq;

public class PositionUploadTable: UploadTable
{
    private DateTime earliestTS, latestTS;
    static public DateTime startCutoff, endCutoff;
    static public bool applyDateFilter, applyGISConversion;
    private List<int> uniqueIDs;
    public DateTime[] dateFilter;


    public PositionUploadTable(DataTable uploadTable, GameObject statsFrame) : base(uploadTable, statsFrame)
    {

    }

    public override void SetTable(List<int> currentClickList)
    {
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

        // Apply date and null filters
        for (int r = uploadTable.Rows.Count - 1; r > 0; r--)
        {
            if (nullRows.Contains(r)) // null or NaN values
            {
                uploadTable.Rows[r].Delete();
            }
            else if (applyDateFilter) // filter time values
            {
                if (DateTime.Compare(DateTime.Parse(uploadTable.Rows[r]["Time"].ToString()), dateFilter[0]) < 0 || DateTime.Compare(dateFilter[1], DateTime.Parse(uploadTable.Rows[r]["Time"].ToString())) > 0)
                {
                    uploadTable.Rows[r].Delete();
                }
            }
        }

        uploadTable.AcceptChanges();

        // GIS conversion needs the mesh uploader units, must take place later
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

        return issueList;
    }
}