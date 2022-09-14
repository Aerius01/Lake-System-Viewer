using Npgsql;
using UnityEngine;
using System;
using System.Linq;
using System.Collections.Generic;

public class DatabaseConnection
{
    private static Dictionary<string, double> GISCoords;
    // private static string connString = "Host=172.16.8.56;Username=public_reader;Password=777c4bde2be5c594d93cd887599d165faaa63992d800a958914f66070549c;Database=doellnsee;CommandTimeout=0;Pooling=true;MaxPoolSize=1000;ConnectionIdleLifetime=10";
    private static string connString = "Host=172.16.8.56;Username=public_reader;Password=777c4bde2be5c594d93cd887599d165faaa63992d800a958914f66070549c;Database=doellnsee;CommandTimeout=0;Pooling=false";

    static DatabaseConnection()  
    {      
        DatabaseConnection.GISCoords = new Dictionary<string, double>() {
            {"MinLong", (double) 3404493.13224369},
            {"MaxLong", (double) 3405269.13224369},
            {"MinLat", (double) 5872333.13262316},
            {"MaxLat", (double) 5872869.13262316}
        };
    }

    public static DataPacket[] GetFishPositions(Fish fish)
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

                    returnPacket[i] = new DataPacket(id, timestamp, x, y, z);
                    i++;
                };

                rdr.Close();
            }

            connection.Close(); 
        }

        return returnPacket;
    }

    public static List<int> GetFishKeys()
    {
        DateTime startTime = DateTime.Now;
        Debug.Log("Fetching fish keys");

        List<int> idList = new List<int>();
        string sql = 
        @"select distinct fish.id
        from fish
        inner join positions
            on fish.id = positions.id
        where fish.id is not null
            and positions.z is not null
            and (fish.id = 2033 or fish.id = 2037)";

        using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            connection.Open(); 
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);

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
                    idList.Add(id);
                };

                rdr.Close();
            }

            connection.Close(); 
        }

        Debug.Log(string.Format("Key fetching: {0}; number of keys: {1}", (DateTime.Now - startTime).TotalSeconds, idList.Count));
        return idList;
    }

    public static FishPacket GetFishMetadata(int fishID)
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
                };

                rdr.Close();
            }

            connection.Close();
        }

        return returnPacket;
    }

    public static WeatherPacket GetWeatherData()
    {
        // https://stackoverflow.com/questions/8145479/can-constructors-be-async
        string strTime = TimeManager.instance.currentTime.ToString("yyyy-MM-dd HH:mm:ss");
        WeatherPacket returnPacket = null;

        string sql = string.Format(
        @"SELECT timestamp, windspeed, winddirection, temperature, humidity, airpressure, precipitation, LEAD(timestamp, 1) OVER ( ORDER BY timestamp ) next_timestamp
        FROM weatherstation
        where timestamp = (select max(timestamp) from weatherstation where timestamp <= TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS')
            AND timestamp IS NOT null
            and (windspeed is not null
                or winddirection is not null
                or temperature is not null
                or humidity is not null
                or airpressure is not null
                or precipitation is not null))
        or timestamp = (select min(timestamp) from weatherstation where timestamp > TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS')
            AND timestamp IS NOT null
            and (windspeed is not null
                or winddirection is not null
                or temperature is not null
                or humidity is not null
                or airpressure is not null
                or precipitation is not null))
        AND timestamp IS NOT null 
        order by timestamp
        limit 1", strTime);

        using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            connection.Open(); 
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);

            using (NpgsqlDataReader rdr = cmd.ExecuteReader())
            {
                if (!rdr.HasRows) { Debug.Log("Weather SQL query yielded empty dataset"); }

                while (rdr.Read())
                {
                    DateTime timestamp = rdr.GetDateTime(rdr.GetOrdinal("timestamp"));

                    float? windspeed = null;
                    var entry = rdr.GetValue(rdr.GetOrdinal("windspeed"));
                    if (!DBNull.Value.Equals(entry))
                    {
                        try { windspeed = Convert.ToSingle(entry); }
                        catch { Debug.Log("Weather data conversion fail: windspeed"); }
                    }   

                    DateTime? nextTimestamp = null;
                    entry = rdr.GetValue(rdr.GetOrdinal("next_timestamp"));
                    if (!DBNull.Value.Equals(entry))
                    {
                        try { nextTimestamp = Convert.ToDateTime(entry); }
                        catch { Debug.Log("Weather data conversion fail: nextTimestamp"); }
                    }   

                    float? winddirection = null;
                    entry = rdr.GetValue(rdr.GetOrdinal("winddirection"));
                    if (!DBNull.Value.Equals(entry))
                    {
                        try { winddirection = Convert.ToSingle(entry); }
                        catch { Debug.Log("Weather data conversion fail: winddirection"); }
                    }   

                    float? temperature = null;
                    entry = rdr.GetValue(rdr.GetOrdinal("temperature"));
                    if (!DBNull.Value.Equals(entry))
                    {
                        try { temperature = Convert.ToSingle(entry); }
                        catch { Debug.Log("Weather data conversion fail: temperature"); }
                    }   

                    float? humidity = null;
                    entry = rdr.GetValue(rdr.GetOrdinal("humidity"));
                    if (!DBNull.Value.Equals(entry))
                    {
                        try { humidity = Convert.ToSingle(entry); }
                        catch { Debug.Log("Weather data conversion fail: humidity"); }
                    }  

                    float? airpressure = null;
                    entry = rdr.GetValue(rdr.GetOrdinal("airpressure"));
                    if (!DBNull.Value.Equals(entry))
                    {
                        try { airpressure = Convert.ToSingle(entry); }
                        catch { Debug.Log("Weather data conversion fail: airpressure"); }
                    }  

                    float? precipitation = null;
                    entry = rdr.GetValue(rdr.GetOrdinal("precipitation"));
                    if (!DBNull.Value.Equals(entry))
                    {
                        try { precipitation = Convert.ToSingle(entry); }
                        catch { Debug.Log("Weather data conversion fail: precipitation"); }
                    }  

                    returnPacket = new WeatherPacket(timestamp, nextTimestamp, windspeed, winddirection, temperature, humidity, airpressure, precipitation);
                };

                rdr.Close();
            }

            connection.Close(); 
        }

        return returnPacket;
    }

    public static DateTime[] GetWeatherMinMaxTimes()
    {
        DateTime earliestTimestamp = DateTime.MaxValue;
        DateTime latestTimestamp = DateTime.MinValue;

        string sql = 
        @"SELECT max(timestamp), min(timestamp)
        FROM weatherstation
        where windspeed is not null
            or winddirection is not null
            or temperature is not null
            or humidity is not null
            or airpressure is not null
            or precipitation is not null
            AND timestamp IS NOT null";

        using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            connection.Open(); 
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);

            using (NpgsqlDataReader rdr = cmd.ExecuteReader())
            {
                if (!rdr.HasRows) { Debug.Log("Weather MaxMin SQL query yielded empty dataset"); }

                while (rdr.Read())
                {
                    earliestTimestamp = DateTime.Compare(earliestTimestamp, rdr.GetDateTime(rdr.GetOrdinal("min"))) < 0 ? earliestTimestamp : rdr.GetDateTime(rdr.GetOrdinal("min"));
                    latestTimestamp = DateTime.Compare(latestTimestamp, rdr.GetDateTime(rdr.GetOrdinal("min"))) > 0 ? latestTimestamp : rdr.GetDateTime(rdr.GetOrdinal("max"));
                };

                rdr.Close();
            }

            connection.Close(); 
        }

        return new DateTime[2] { earliestTimestamp, latestTimestamp };
    }

    public static ThermoPacket GetThermoData()
    {
        string strTime = TimeManager.instance.currentTime.ToString("yyyy-MM-dd HH:mm:ss");

        // Inits
        List<ThermoReading> readings = new List<ThermoReading>();
        DateTime timestamp = DateTime.MaxValue;
        DateTime? nextTimestamp = null;

        string sql = string.Format(
        @"SELECT timestamp, windspeed, winddirection, temperature, humidity, airpressure, precipitation, LEAD(timestamp, 1) OVER ( ORDER BY timestamp ) next_timestamp
        FROM weatherstation
        where timestamp = (select max(timestamp) from weatherstation where timestamp <= TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS')
            AND timestamp IS NOT null
            and (windspeed is not null
                or winddirection is not null
                or temperature is not null
                or humidity is not null
                or airpressure is not null
                or precipitation is not null))
        or timestamp = (select min(timestamp) from weatherstation where timestamp > TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS')
            AND timestamp IS NOT null
            and (windspeed is not null
                or winddirection is not null
                or temperature is not null
                or humidity is not null
                or airpressure is not null
                or precipitation is not null))
        AND timestamp IS NOT null 
        order by timestamp
        limit 1", strTime);

        using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            connection.Open(); 
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);

            using (NpgsqlDataReader rdr = cmd.ExecuteReader())
            {
                if (!rdr.HasRows) { Debug.Log("Thermo SQL query yielded empty dataset"); }

                while (rdr.Read())
                {
                    timestamp = rdr.GetDateTime(rdr.GetOrdinal("timestamp"));

                    nextTimestamp = null;
                    var entry = rdr.GetValue(rdr.GetOrdinal("next_timestamp"));
                    if (!DBNull.Value.Equals(entry))
                    {
                        try { nextTimestamp = Convert.ToDateTime(entry); }
                        catch { Debug.Log("Weather data conversion fail: nextTimestamp"); }
                    }   

                    // Never null by the architecture of the DB
                    float depth = float.MaxValue;
                    entry = rdr.GetValue(rdr.GetOrdinal("depth"));
                    try { depth = Convert.ToSingle(entry); }
                    catch { Debug.Log("Weather data conversion fail: depth"); }

                    float? temperature = null;
                    entry = rdr.GetValue(rdr.GetOrdinal("temperature"));
                    if (!DBNull.Value.Equals(entry))
                    {
                        try { temperature = Convert.ToSingle(entry); }
                        catch { Debug.Log("Weather data conversion fail: temperature"); }
                    }   

                    float? oxygen = null;
                    entry = rdr.GetValue(rdr.GetOrdinal("oxygen"));
                    if (!DBNull.Value.Equals(entry))
                    {
                        try { oxygen = Convert.ToSingle(entry); }
                        catch { Debug.Log("Weather data conversion fail: oxygen"); }
                    }  

                    ThermoReading reading = new ThermoReading(depth, temperature, oxygen);

                    // Check uniqueness of depth entry before adding to list
                    bool unique = true;
                    foreach (ThermoReading record in readings)
                    {
                        if (record.depth == reading.depth)
                        {
                            unique = false;
                            break;
                        }
                    }

                    if (unique) readings.Add(reading);
                };

                rdr.Close();
            }

            connection.Close(); 
        }

        if (readings.Any()) return new ThermoPacket(timestamp, nextTimestamp, readings);
        else return null;
    }

    public static DateTime[] GetThermoMinMaxTimes()
    {
        DateTime earliestTimestamp = DateTime.MaxValue;
        DateTime latestTimestamp = DateTime.MinValue;

        // TODO: adapt SQL query
        string sql = 
        @"SELECT max(timestamp), min(timestamp)
        FROM weatherstation
        where windspeed is not null
            or winddirection is not null
            or temperature is not null
            or humidity is not null
            or airpressure is not null
            or precipitation is not null
            AND timestamp IS NOT null";

        using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            connection.Open(); 
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);

            using (NpgsqlDataReader rdr = cmd.ExecuteReader())
            {
                if (!rdr.HasRows) { Debug.Log("Thermo MaxMin SQL query yielded empty dataset"); }

                while (rdr.Read())
                {
                    earliestTimestamp = DateTime.Compare(earliestTimestamp, rdr.GetDateTime(rdr.GetOrdinal("min"))) < 0 ? earliestTimestamp : rdr.GetDateTime(rdr.GetOrdinal("min"));
                    latestTimestamp = DateTime.Compare(latestTimestamp, rdr.GetDateTime(rdr.GetOrdinal("min"))) > 0 ? latestTimestamp : rdr.GetDateTime(rdr.GetOrdinal("max"));
                };

                rdr.Close();
            }

            connection.Close(); 
        }

        return new DateTime[2] { earliestTimestamp, latestTimestamp };
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
}
