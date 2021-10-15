using System.Collections.Generic;
using System;
using System.Data;
using UnityEngine;
using System.Linq;

public class PositionUploadTable: UploadTable
{
    private DateTime earliestTS, latestTS;
    static public DateTime startCutoff, endCutoff;
    static public bool applyDateFilter;
    private List<int> uniqueIDs;
    private DateTime[] dateFilter;


    public PositionUploadTable(DataTable uploadTable, GameObject statsFrame) : base(uploadTable, statsFrame)
    {

    }

    protected override void AdditionalOperations(string stringValue, int row, int column)
    {
        // compare values for earliest/latest timestamps
        try
        {
            DateTime value = DateTime.Parse(stringValue);

            if (DateTime.Compare(earliestTS, value) > 0)
            {
                earliestTS = value;
            }

            if (DateTime.Compare(latestTS, value) < 0)
            {
                latestTS = value;
            }
        }
        catch (FormatException)
        {
            throwException = true;
            nullList.Add(new int[] {row, column});
        }

        // TODO: count distinct IDs
    }

    public override void ResetParams(int i)
    {
        earliestTS = DateTime.MinValue;
        latestTS = DateTime.MaxValue;

        base.ResetParams(i);
    }

    public override void SetTable(List<int> currentClickList)
    {
        base.SetTable(currentClickList);

        // Delete rows that fall outside of the time filter
        if (applyDateFilter)
        {
            dateFilter = new DateTime[2] {startCutoff, endCutoff};

            for (int r = uploadTable.Rows.Count; r > 0; r--)
            {
                if (DateTime.Compare(DateTime.Parse(uploadTable.Rows[r]["Time"].ToString()), dateFilter[0]) < 0 || DateTime.Compare(dateFilter[1], DateTime.Parse(uploadTable.Rows[r]["Time"].ToString())) > 0)
                {
                    uploadTable.Rows[r].Delete();
                }
            }

            uploadTable.AcceptChanges();
        }
        
       

        // apply GIS conversion
        // delete rows that contain nulls -> convert 2D list to 1D row-based bool
    }

    public void AttributeColumnNames()
    {
        // Apply column names to the datable so we know what is what
        for (int c = 0; c < uploadTable.Columns.Count; c++)
        {
            if (viewPort.GetColumnName(c) != null)
            {
                uploadTable.Columns[c].ColumnName = viewPort.GetColumnName(c);
            }      
            else
            {
                uploadTable.Columns.RemoveAt(c);
            }      
        }

        uploadTable.AcceptChanges();
    }
}