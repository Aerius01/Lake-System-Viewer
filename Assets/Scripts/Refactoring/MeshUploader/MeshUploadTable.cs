using System.Collections.Generic;
using System;
using System.Data;
using UnityEngine;
using System.Linq;

public class MeshUploadTable : UploadTable
{
    public float maxDepth, minDepth;

    public MeshUploadTable(DataTable uploadTable) : base(uploadTable)
    {

    }

    protected override void AdditionalOperations(string stringValue, int row, int column)
    {
        try
        {
            float value = float.Parse(stringValue);
            minDepth = Math.Min(minDepth, value);
            maxDepth = Math.Max(maxDepth, value);
        }
        catch (FormatException)
        {
            throwException = true;
            nullList.Add(new int[] {row, column});
        }
    }

    public override void ResetParams(int i)
    {
        minDepth = int.MaxValue;
        maxDepth = int.MinValue;

        base.ResetParams(i);
    }

    // public void SetTable(List<int> currentClickList, int toggleID = -1, float replacementVal = 0f)
    // {
    //     // Trim non-data rows/columns
    //     int rowsToRemove = 0;
    //     int columnsToRemove = 0;
    //     foreach (int idNumber in currentClickList)
    //     {
    //         rowsToRemove = Mathf.Max(idNumber - (int)Math.Floor((double)idNumber/(double)viewPort.Columns) * viewPort.Columns, rowsToRemove);
    //         columnsToRemove = Mathf.Max(idNumber - (int)Math.Floor((double)idNumber/(double)viewPort.Rows) * viewPort.Rows, columnsToRemove);
    //     }

    //     for (int r = rowsToRemove; r > 0; r--)
    //     {
    //         uploadTable.Rows[r].Delete();
    //     }
    //     uploadTable.AcceptChanges();

    //     for (int c = columnsToRemove; c > 0; c--)
    //     {
    //         uploadTable.Columns.RemoveAt(c);
    //     }
    //     uploadTable.AcceptChanges();

    //     if (toggleID > 0)
    //     {
    //         if (toggleID == 1)
    //         {
    //             // Interpolate
    //             foreach (int[] entry in nullList)
    //             {
    //                 // check boundary conditions
    //                 List<float> boundingValues = new List<float>();
                    
    //                 if (entry[0] - 1 > rowsToRemove)
    //                 {
    //                     // if the entry is within the table limits, check if it is also null
    //                     if (!nullList.Any(p => p.SequenceEqual(new int[] {entry[0] - 1, entry[1]})))
    //                     {
    //                         boundingValues.Add(float.Parse(uploadTable.Rows[entry[0] - 1][entry[1]].ToString()));
    //                     }
    //                 }

    //                 if (entry[0] + 1 < uploadTable.Rows.Count)
    //                 {
    //                     if (!nullList.Any(p => p.SequenceEqual(new int[] {entry[0] + 1, entry[1]})))
    //                     {
    //                         boundingValues.Add(float.Parse(uploadTable.Rows[entry[0] + 1][entry[1]].ToString()));
    //                     }
    //                 }

    //                 if (entry[1] - 1 > columnsToRemove)
    //                 {
    //                     if (!nullList.Any(p => p.SequenceEqual(new int[] {entry[0], entry[1] - 1})))
    //                     {
    //                         boundingValues.Add(float.Parse(uploadTable.Rows[entry[0]][entry[1] - 1].ToString()));
    //                     }
    //                 }

    //                 if (entry[1] + 1 < uploadTable.Columns.Count)
    //                 {
    //                     if (!nullList.Any(p => p.SequenceEqual(new int[] {entry[0], entry[1] + 1})))
    //                     {  
    //                         boundingValues.Add(float.Parse(uploadTable.Rows[entry[0]][entry[1] + 1].ToString()));
    //                     }
    //                 }
                    
    //                 if (!boundingValues.Any())
    //                 {
    //                     // All surrounding values are null
    //                     Debug.Log(string.Format("Value at [row][column] [{0}][{1}] is surrounded by nulls, interpolation not possible", entry[0], entry[1]));
    //                     Debug.Log("Replacing this value with a value of 99999");

    //                     uploadTable.Rows[entry[0]][entry[1]] = 99999;
    //                 }
    //                 else
    //                 {
    //                     uploadTable.Rows[entry[0]][entry[1]] = boundingValues.Sum() / boundingValues.Count;
    //                 }
    //             }
    //         }
    //         else
    //         {
    //             // Replace
    //             foreach (int[] entry in nullList)
    //             {
    //                 uploadTable.Rows[entry[0]][entry[1]] = replacementVal;
    //             }
    //         }

    //         uploadTable.AcceptChanges();
    //     }
    // }
}