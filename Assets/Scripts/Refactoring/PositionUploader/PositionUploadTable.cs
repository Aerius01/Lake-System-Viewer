using System.Collections.Generic;
using System;
using System.Data;
using UnityEngine;
using System.Linq;

public class PositionUploadTable: UploadTable
{
    private DateTime earliestTS, latestTS;
    private List<int> uniqueIDs;

    public DateTime[] dateFilter;


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

        
        // retrieve dropdown to column mapping
        // apply column names to the datable


        // apply datetime filter to time column
        dateFilter = new DateTime[2];
        // run through timestamp column & delete as necessary

        // apply GIS conversion
        // delete rows that contain nulls -> convert 2D list to 1D row-based bool
    }
}