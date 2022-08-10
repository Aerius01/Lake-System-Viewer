using System;
using System.Collections.Generic;

public class SQLStringBuilder
{
    public SQLStringBuilder(List<(string, string)> selectAndTable, List<string> where)
    {
        List<string> items = new List<string>(); //field
        List<(string, string)> tables = new List<(string, string)>(); //(alias, table name)

        foreach ((string, string) item in selectAndTable)
        {
            string select = item.Item1;
            string table = item.Item2;

            string alias = table.Substring(0,1);
            // if (alias in tables)
            // {
            //     while (!(alias in tables))
            //     {

            //     }
            // }
        }
    }
}