using Npgsql;
using Npgsql.Schema;
using Unity;
using UnityEngine;
using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;

public class DatabaseConnection
{
    public static bool connected
    { get { if (DatabaseConnection.connection.State == ConnectionState.Open)  {return true; } else { return false; }} }

    private static NpgsqlConnection connection;
    private static Dictionary<string, double> GISCoords;

    static DatabaseConnection()  
    {      
        // https://stackoverflow.com/questions/8145479/can-constructors-be-async
        string connString = "Host=172.16.8.56;Username=public_reader;Password=777c4bde2be5c594d93cd887599d165faaa63992d800a958914f66070549c;Database=doellnsee;CommandTimeout=0";
        
        NpgsqlConnection connection = new NpgsqlConnection(connString);
        connection.Open(); 

        DatabaseConnection.connection = connection;
        DatabaseConnection.GISCoords = new Dictionary<string, double>() {
            {"MinLong", (double) 3404493.13224369},
            {"MaxLong", (double) 3405269.13224369},
            {"MinLat", (double) 5872333.13262316},
            {"MaxLat", (double) 5872869.13262316}
        };
    }

    public async static Task<DataPacket[]> GetFishData(Fish fish)
    {
        int fishID = fish.id;
        string strTime = TimeManager.instance.currentTime.ToString("yyyy-MM-dd HH:mm:ss");
        // 2015-10-16 03:10:46

        string sql = string.Format(
            @"SELECT p.id, p.timestamp, p.x, p.y, p.z
            FROM positions p 
            where
            p.id = {0}
            AND (p.timestamp = (select max(timestamp) from positions where timestamp < TO_TIMESTAMP('{1}', 'YYYY-MM-DD HH24:MI:SS') and id = {0})
            OR p.timestamp = (select min(timestamp) from positions where timestamp > TO_TIMESTAMP('{1}', 'YYYY-MM-DD HH24:MI:SS') and id = {0}))
            AND p.id IS NOT NULL
            AND p.timestamp IS NOT NULL
            AND p.x IS NOT NULL
            AND p.y IS NOT NULL
            AND p.z IS NOT NULL
            ORDER BY p.timestamp", fishID, strTime);
        
        NpgsqlCommand cmd = new NpgsqlCommand(sql, DatabaseConnection.connection);

        int i = 0;
        DataPacket[] returnPacket = new DataPacket[2];
        await using NpgsqlDataReader rdr = cmd.ExecuteReader();

        if (!rdr.HasRows)
        {
            Debug.Log("there's no data");
        }

        while (rdr.Read())
        {
            int id = rdr.GetInt32(rdr.GetOrdinal("id"));
            DateTime timestamp = rdr.GetDateTime(rdr.GetOrdinal("timestamp"));
            float x = DatabaseConnection.ConvertLong(rdr.GetValue(rdr.GetOrdinal("x")));
            float y = DatabaseConnection.ConvertLat(rdr.GetValue(rdr.GetOrdinal("y")));
            float z = Convert.ToSingle(rdr.GetValue(rdr.GetOrdinal("z")));

            returnPacket[i] = new DataPacket(id, timestamp, x, y, z);
            i++;
        };

        Debug.Log("created packet");
        return returnPacket;
    }

    private static float ConvertLat(object latObject)
    {
        double lat = Convert.ToDouble(latObject);
        if (lat > DatabaseConnection.GISCoords["MaxLat"] || lat < DatabaseConnection.GISCoords["MinLat"])
        { throw new FormatException("The provided latitude is outside the range of the bounding box"); }

        return (float)((LocalMeshData.rowCount) * ((lat - DatabaseConnection.GISCoords["MinLat"]) / (DatabaseConnection.GISCoords["MaxLat"] - DatabaseConnection.GISCoords["MinLat"])));
    }

    private static float ConvertLong(object longObject)
    {
        string stringLong = "3" + Convert.ToString(longObject);
        double doubleLong = double.Parse(stringLong.Replace("\"", "").Trim());

        if (doubleLong > DatabaseConnection.GISCoords["MaxLong"] || doubleLong < DatabaseConnection.GISCoords["MinLong"])
        { throw new FormatException("The provided longitude is outside the range of the bounding box"); }

        return (float)((LocalMeshData.columnCount) * ((doubleLong - DatabaseConnection.GISCoords["MinLong"]) / (DatabaseConnection.GISCoords["MaxLong"] - DatabaseConnection.GISCoords["MinLong"])));
    }
        

        

    // public void smmn()
    // {
    //     Debug.Log(TimeManager.instance.currentTime);
    //     Debug.Log(TimeManager.instance.currentTime.ToString("yyyy-MM-dd HH:mm:ss"));

    //     string strTime = TimeManager.instance.currentTime.ToString("yyyy-MM-dd HH:mm:ss");

    //     string sql =
    //         string.Format(@"select 
    //         p.id, p.timestamp, p.x, p.y, p.z, lead(p.timestamp) over (order by p.timestamp) as next 
    //         from positions p 
    //         where timestamp = (select max(timestamp) from positions where timestamp < TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS'))", strTime);
        
    //     Debug.Log(sql);

    //     using var cmd = new NpgsqlCommand(sql, connection);
    //     NpgsqlDataReader rdr = cmd.ExecuteReader();

    //     while (rdr.Read()) { Debug.Log(rdr.GetValue(rdr.GetOrdinal("timestamp")).GetType()); };
        

    //     // foreach (NpgsqlDbColumn tableColumn in rdr.GetColumnSchema())
    //     // {
    //     //     Debug.Log(tableColumn.ColumnName);
    //     //     Debug.Log(tableColumn.ColumnAttributeNumber);
    //     //     Debug.Log(tableColumn.DataType);
    //     //     Debug.Log(tableColumn.NpgsqlDbType);
    //     // }
    // }
}
