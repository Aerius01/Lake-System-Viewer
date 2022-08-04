using Npgsql;
using Npgsql.Schema;
using Unity;
using UnityEngine;

public class DatabaseConnection
{
    public void DoIt()
    {
        var connString = "Host=172.16.8.56;Username=public_reader;Password=777c4bde2be5c594d93cd887599d165faaa63992d800a958914f66070549c;Database=doellnsee";

        NpgsqlConnection connection = new NpgsqlConnection(connString);
        connection.Open();

        Debug.Log(TimeManager.instance.currentTime);
        Debug.Log(TimeManager.instance.currentTime.ToString("yyyy-MM-dd HH:mm:ss"));

        string strTime = TimeManager.instance.currentTime.ToString("yyyy-MM-dd HH:mm:ss");

        string sql =
            string.Format(@"select 
            p.id, p.timestamp, p.x, p.y, p.z 
            from positions p 
            where timestamp = (select max(timestamp) from positions where timestamp < TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS'))", strTime);
        
        Debug.Log(sql);

        using var cmd = new NpgsqlCommand(sql, connection);
        NpgsqlDataReader rdr = cmd.ExecuteReader();

        while (rdr.Read())
        {
            Debug.Log(rdr.GetValue(rdr.GetOrdinal("timestamp")).GetType());
        };
        

        // foreach (NpgsqlDbColumn tableColumn in rdr.GetColumnSchema())
        // {
        //     Debug.Log(tableColumn.ColumnName);
        //     Debug.Log(tableColumn.ColumnAttributeNumber);
        //     Debug.Log(tableColumn.DataType);
        //     Debug.Log(tableColumn.NpgsqlDbType);
        // }
    }
}
