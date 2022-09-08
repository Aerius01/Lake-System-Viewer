using Npgsql;
using UnityEngine;
using System;
using System.Collections.Generic;

public class DatabaseConnection
{
    private static Dictionary<string, double> GISCoords;
    private static string connString = "Host=172.16.8.56;Username=public_reader;Password=777c4bde2be5c594d93cd887599d165faaa63992d800a958914f66070549c;Database=doellnsee;CommandTimeout=0";

    static DatabaseConnection()  
    {      
        DatabaseConnection.GISCoords = new Dictionary<string, double>() {
            {"MinLong", (double) 3404493.13224369},
            {"MaxLong", (double) 3405269.13224369},
            {"MinLat", (double) 5872333.13262316},
            {"MaxLat", (double) 5872869.13262316}
        };
    }

    public static DataPacket[] GetFishData(Fish fish)
    {
        // https://stackoverflow.com/questions/8145479/can-constructors-be-async
        DataPacket[] returnPacket = new DataPacket[2];
        int fishID = fish.id;
        string strTime = TimeManager.instance.currentTime.ToString("yyyy-MM-dd HH:mm:ss");
        // 2015-10-16 03:10:46

        string sql = string.Format(
            @"SELECT p.id, p.timestamp, p.x, p.y, p.z
            FROM positions p 
            where
            p.id = {0}
            AND (p.timestamp = (select max(timestamp) from positions p where timestamp <= TO_TIMESTAMP('{1}', 'YYYY-MM-DD HH24:MI:SS')
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

        using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            connection.Open(); 
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);

            int i = 0;
            using (NpgsqlDataReader rdr = cmd.ExecuteReader())
            {
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
                    float y = LocalMeshData.rowCount - DatabaseConnection.ConvertLat(rdr.GetValue(rdr.GetOrdinal("y")));
                    float z = - Convert.ToSingle(rdr.GetValue(rdr.GetOrdinal("z")));

                    // if (id == 2033)
                    // {
                    //     if (i == 0) {Debug.Log(string.Format("startPos: ({0}, {1}, {2}); bound: {3}", x, y, z, timestamp));}
                    //     else {Debug.Log(string.Format("endPos: ({0}, {1}, {2}); bound: {3}", x, y, z, timestamp));}
                    // }

                    returnPacket[i] = new DataPacket(id, timestamp, x, y, z);
                    i++;
                };

                rdr.Close();
            }

            connection.Close(); 
        }

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
        FishPacket returnPacket = null;
        string sql = string.Format(
            @"SELECT f.id,  f.species, s.name, f.tl, f.weight, f.sex, f.comment, min(p.timestamp) as minTime, max(p.timestamp) as maxTime
            FROM FISH f 
            left join species s ON f.species = s.ID 
            left join positions p on f.id = p.id
            where f.id = {0}
            and s.name != 'Beacon' and p.timestamp is not null
            group by f.id,  f.species, s.name, f.tl, f.weight, f.sex, f.comment", fishID);
        
        using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            connection.Open();
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);

            using (NpgsqlDataReader rdr = cmd.ExecuteReader())
            {
                // No fish metadata could be recovered
                if (!rdr.HasRows) { rdr.Close(); connection.Close(); return returnPacket; }

                int i = 0;
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
                    
                    // Nullity handled by SQL query
                    DateTime earliestTimestamp = rdr.GetDateTime(rdr.GetOrdinal("minTime"));
                    DateTime latestTimestamp = rdr.GetDateTime(rdr.GetOrdinal("maxTime"));

                    returnPacket = new FishPacket(id, captureType, length, speciesCode, weight, speciesName, male, earliestTimestamp, latestTimestamp);
                    i++;
                };

                rdr.Close();
            }

            connection.Close();
        }

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
