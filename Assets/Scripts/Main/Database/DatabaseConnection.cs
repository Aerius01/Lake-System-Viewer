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
            AND (p.timestamp = (select max(timestamp) from positions p where timestamp < TO_TIMESTAMP('{1}', 'YYYY-MM-DD HH24:MI:SS')
                AND id = {0} 
                AND p.timestamp IS NOT NULL
                AND p.x IS NOT NULL
                AND p.y IS NOT NULL
                AND p.z IS NOT NULL)
            OR p.timestamp = (select min(timestamp) from positions p where timestamp > TO_TIMESTAMP('{1}', 'YYYY-MM-DD HH24:MI:SS') 
                AND id = {0}
                AND p.timestamp IS NOT NULL
                AND p.x IS NOT NULL
                AND p.y IS NOT NULL
                AND p.z IS NOT NULL))
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
            // ERROR HANDLING
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

        rdr.Close();
        Debug.Log("created data packet");
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

    public static FishPacket GetMetaData(int fishID)
    {
        string sql = string.Format(
            @"SELECT f.id,  f.species, s.name, f.tl, f.weight, f.sex, f.firstposition, f.lastposition, f.comment 
            FROM FISH f 
            left join species s 
            ON f.species = s.ID 
            where f.id = {0} and s.name != 'Beacon' and f.firstposition is not null and f.lastposition is not null", fishID);

        NpgsqlCommand cmd = new NpgsqlCommand(sql, DatabaseConnection.connection);
        NpgsqlDataReader rdr = cmd.ExecuteReader();

        int i = 0;
        FishPacket returnPacket = null;

        // No fish metadata could be recovered
        if (!rdr.HasRows) { return returnPacket; }

        while (rdr.Read())
        {
            int id = rdr.GetInt32(rdr.GetOrdinal("id"));

            string captureType = null;
            var entry = rdr.GetValue(rdr.GetOrdinal("comment"));
            if (!DBNull.Value.Equals(entry))
            {
                try
                {
                    captureType = Convert.ToString(entry); 
                    if (String.IsNullOrEmpty(captureType)) captureType = null;
                }
                catch { Debug.Log("Metadata conversion fail: capture type"); }
            }    

            int? length = null;
            entry = rdr.GetValue(rdr.GetOrdinal("tl"));
            if (!DBNull.Value.Equals(entry))
            {
                try { length = Convert.ToInt32(entry); }
                catch { Debug.Log("Metadata conversion fail: length"); }
            }     

            int? speciesCode = null;
            entry = rdr.GetValue(rdr.GetOrdinal("species"));
            if (!DBNull.Value.Equals(entry))
            {
                try { speciesCode = Convert.ToInt32(entry); }
                catch { Debug.Log("Metadata conversion fail: species code"); }
            } 

            int? weight = null;
            entry = rdr.GetValue(rdr.GetOrdinal("weight"));
            if (!DBNull.Value.Equals(entry))
            {
                try { weight = Convert.ToInt32(entry); }
                catch { Debug.Log("Metadata conversion fail: weight"); }
            } 

            string speciesName = null;
            entry = rdr.GetValue(rdr.GetOrdinal("name"));
            if (!DBNull.Value.Equals(entry))
            {
                try
                {
                    speciesName = Convert.ToString(entry); 
                    if (String.IsNullOrEmpty(speciesName)) speciesName = null;
                }
                catch { Debug.Log("Metadata conversion fail: species name"); }
            } 

            bool? male = null;
            entry = rdr.GetValue(rdr.GetOrdinal("sex"));
            if (!DBNull.Value.Equals(entry))
            {
                string tempString = Convert.ToString(entry);
                if (tempString.Contains('m')) male = true;
                else if (tempString.Contains('f')) male = false;
            }
            
            // Null-handling secured by SQL query
            DateTime earliestTimestamp = rdr.GetDateTime(rdr.GetOrdinal("firstposition"));
            DateTime latestTimestamp = rdr.GetDateTime(rdr.GetOrdinal("lastposition"));
            returnPacket = new FishPacket(id, captureType, length, speciesCode, weight, speciesName, male, earliestTimestamp, latestTimestamp);
            i++;
        };

        rdr.Close();
        return returnPacket;
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
