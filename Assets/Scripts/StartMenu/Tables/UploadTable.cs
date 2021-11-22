using System.Collections.Generic;
using System;
using System.Data;
using UnityEngine;
using System.Linq;
using System.Threading;

public class UploadTable
{
    public int adjustedColumnCount, adjustedRowCount, nullCount, totalColumnCount, totalRowCount;
    public List<int[]> nullList;
    public bool throwException, stopThread, resetComplete, waitingOnReset = false;
    public DataTable uploadTable;
    public ViewPort viewPort;
    protected int rowsToRemove = 0, columnsToRemove = 0;
    public Thread secondaryThread;
    public CanvasGroup containerCG;
    public GameObject loadingIcon;


    public UploadTable(DataTable uploadTable, GameObject statsFrame)
    {
        this.uploadTable = uploadTable; 
        this.containerCG = statsFrame.transform.Find("StatisticsFrame").transform.Find("TextContainer").GetComponent<CanvasGroup>(); 
        this.loadingIcon = containerCG.transform.Find("LoadingIcon").gameObject;
        loadingIcon.SetActive(false);

        totalColumnCount = this.uploadTable.Columns.Count;
        totalRowCount = this.uploadTable.Rows.Count;
    }

    public virtual void ResetParams(int i)
    {
        stopThread = false;
        resetComplete = false;
        waitingOnReset = true;

        secondaryThread = new Thread(() => BaseReset(i));
        secondaryThread.Start();

        containerCG.alpha = 0.5f;
        loadingIcon.SetActive(true);
    }

    protected void BaseReset(int i)
    {
        columnsToRemove = i - (int)Math.Floor((double)i/(double)viewPort.Columns) * viewPort.Columns;
        rowsToRemove = (int)Math.Floor((double)i/(double)viewPort.Columns);

        adjustedColumnCount = totalColumnCount - columnsToRemove;
        adjustedRowCount = totalRowCount - rowsToRemove;

        nullList = new List<int[]>();
        throwException = false;

        for (int row = rowsToRemove; row < totalRowCount; row++)
        {
            if (stopThread)
            {
                break;
            }
            else
            {
                for (int column = columnsToRemove; column < totalColumnCount; column++)
                {
                    if (stopThread)
                    {
                        break;
                    }

                    string stringValue = uploadTable.Rows[row][column].ToString().Trim();
                    if (string.IsNullOrEmpty(stringValue))
                    {
                        nullList.Add(new int[] {row, column});
                    }
                    else
                    {
                        AdditionalOperations(stringValue, row, column);
                    }
                }
            }
        }

        if (!stopThread)
        {
            nullCount = nullList.Count;
            resetComplete = true;
        }
        else
        {
            waitingOnReset = false;
        }
    }

    protected virtual void AdditionalOperations(string stringValue, int row, int column)
    {

    }

    public virtual void SetTable(List<int> currentClickList)
    {
        for (int r = rowsToRemove - 1; r >= 0; r--)
        {
            uploadTable.Rows[r].Delete();
        }
        uploadTable.AcceptChanges();

        for (int c = columnsToRemove - 1; c >= 0; c--)
        {
            uploadTable.Columns.RemoveAt(c);
        }
        uploadTable.AcceptChanges();
    }
}