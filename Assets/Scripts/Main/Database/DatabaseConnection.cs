using Npgsql;
using UnityEngine;
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

public class DatabaseConnection
{
    // private static Dictionary<string, double> GISCoords;
    private static string connString = "Host=172.16.8.56;Username=public_reader;Password=777c4bde2be5c594d93cd887599d165faaa63992d800a958914f66070549c;Database=doellnsee;CommandTimeout=0;Pooling=true;MaxPoolSize=5000;Timeout=300";
    // private static string connString = "Host=172.16.8.56;Username=public_reader;Password=777c4bde2be5c594d93cd887599d165faaa63992d800a958914f66070549c;Database=doellnsee;CommandTimeout=0;Pooling=false";
    private static List<CommandWrapper> forwardBatch, doubleSidedBatch;
    private static readonly object locker = new object();
    public static bool queuedQueries { get { return forwardBatch.Any() || doubleSidedBatch.Any(); } }

    static DatabaseConnection()  
    {      
        forwardBatch = new List<CommandWrapper>();
        doubleSidedBatch = new List<CommandWrapper>();
    }

    public static void QueuePositionBatchCommand(int id, DateTime lastListTime, bool forwardOnly=true)
    {
        lock(DatabaseConnection.locker)
        {
            // Ensure that each ID has at most only a single query lined up
            if (!forwardOnly) 
            {
                if (doubleSidedBatch.Where(i => i.id == id).FirstOrDefault() != null) doubleSidedBatch.RemoveAt(doubleSidedBatch.IndexOf(doubleSidedBatch.Where(i => i.id == id).FirstOrDefault()));
                doubleSidedBatch.Add(new CommandWrapper(id, lastListTime, forwardOnly));
            }
            else
            {
                // If a full requery is in motion (double-sided), no need to run the forward-only query
                if (doubleSidedBatch.Where(i => i.id == id).FirstOrDefault() == null)
                {
                    if (forwardBatch.Where(i => i.id == id).FirstOrDefault() != null) forwardBatch.RemoveAt(forwardBatch.IndexOf(forwardBatch.Where(i => i.id == id).FirstOrDefault()));
                    forwardBatch.Add(new CommandWrapper(id, lastListTime, forwardOnly));
                }
            } 
        }
    }

    public static async Task BatchAndRunPositionQueries()
    {
        List<Task> tasks = new List<Task>();

        // Parallelize forward queries
        if (DatabaseConnection.forwardBatch.Any())
        {
            int chunkSize = (int)Mathf.Ceil(DatabaseConnection.forwardBatch.Count / 30);
            Debug.Log(string.Format("Splitting {0} forward query/ies into chunks of size {1}", forwardBatch.Count, chunkSize < 1 ? 1 : chunkSize));
            List<List<CommandWrapper>> partialBatchLists = new List<List<CommandWrapper>>();

            lock(DatabaseConnection.locker)
            {
                partialBatchLists = Tools.ChunkList(DatabaseConnection.forwardBatch, chunkSize < 1 ? 1 : chunkSize);
                DatabaseConnection.forwardBatch = new List<CommandWrapper>();
            }

            foreach (List<CommandWrapper> partialList in partialBatchLists)
            {
                NpgsqlBatch partialBatch = new NpgsqlBatch();
                foreach (CommandWrapper command in partialList) partialBatch.BatchCommands.Add(command.sql); 

                Task runner = RunQueuedPositionQueries(partialBatch);
                tasks.Add(runner);
            }
        }
        
        // Parallelize double-sided queries
        if (DatabaseConnection.doubleSidedBatch.Any())
        {
            int chunkSize = (int)Mathf.Ceil(DatabaseConnection.doubleSidedBatch.Count / 30);
            Debug.Log(string.Format("Splitting {0} double-sided query/ies into chunks of size {1}", doubleSidedBatch.Count, chunkSize < 1 ? 1 : chunkSize));
            List<List<CommandWrapper>> partialBatchLists = new List<List<CommandWrapper>>();

            lock(DatabaseConnection.locker)
            {
                partialBatchLists = Tools.ChunkList(DatabaseConnection.doubleSidedBatch, chunkSize < 1 ? 1 : chunkSize);
                DatabaseConnection.doubleSidedBatch = new List<CommandWrapper>();
            }

            foreach (List<CommandWrapper> partialList in partialBatchLists)
            {
                NpgsqlBatch partialBatch = new NpgsqlBatch();
                foreach (CommandWrapper command in partialList) partialBatch.BatchCommands.Add(command.sql); 

                Task runner = RunQueuedPositionQueries(partialBatch, false);
                tasks.Add(runner);
            }
        }

        // Synch fall-back condition (all tasks finished immediately)
        bool syncRunThrough = true;
        foreach (Task task in tasks) { if (task.Status != TaskStatus.RanToCompletion) { syncRunThrough = false; break; } }

        if (!syncRunThrough) { await Task.WhenAll(tasks.ToArray()); }

        Debug.Log("All partial query tasks complete");
    } 

    public static async Task RunQueuedPositionQueries(NpgsqlBatch queries, bool forwardOnly=true)
    {
        List<Task> tasks = new List<Task>();

        // Debug.Log("Running batch queries");
        await using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            await connection.OpenAsync();

            NpgsqlBatch batch = new NpgsqlBatch(connection);
            foreach (NpgsqlBatchCommand command in queries.BatchCommands) batch.BatchCommands.Add(command); 

            await using (NpgsqlDataReader rdr = await batch.ExecuteReaderAsync())
            {
                for (int i=0; i < batch.BatchCommands.Count; i++)
                {
                    // Debug.Log(string.Format("Command {0}: Reading...", i));

                    // No position data could be recovered
                    if (!rdr.HasRows) { rdr.NextResult(); }
                    else
                    {
                        List<DataPacket> returnPackets = new List<DataPacket>();
                        while (await rdr.ReadAsync())
                        {
                            // Nullity handled by SQL query
                            int id = rdr.GetInt32(rdr.GetOrdinal("id"));
                            DateTime timestamp = rdr.GetDateTime(rdr.GetOrdinal("timestamp"));

                            float x = 0f;
                            var entry = rdr.GetValue(rdr.GetOrdinal("x"));
                            try { x = Convert.ToSingle(entry); }
                            catch { Debug.Log("Position conversion fail: x"); }

                            float y = 0f;
                            entry = rdr.GetValue(rdr.GetOrdinal("y"));
                            try { y = LocalMeshData.rowCount - Convert.ToSingle(entry); }
                            catch { Debug.Log("Position conversion fail: y"); }


                            float z = 0f;
                            entry = rdr.GetValue(rdr.GetOrdinal("z"));
                            try { z = - Convert.ToSingle(entry); }
                            catch { Debug.Log("Position conversion fail: z"); }

                            DataPacket thisPacket = new DataPacket(id, timestamp, x, y, z);
                            returnPackets.Add(thisPacket);
                        };

                        // Task that is completely independent of next iteration
                        Task task = FishManager.fishDict[returnPackets[0].id].UpdatePositionCache(returnPackets, forwardOnly);
                        tasks.Add(task);
                    }

                    rdr.NextResult();
                }

                await rdr.CloseAsync();
            }

            await connection.CloseAsync();
        }
        
        // Ensure all fish caches are updated
        await Task.WhenAll(tasks.ToArray());
    }

    public static List<int> GetFishKeys()
    {
        DateTime startTime = DateTime.Now;

        List<int> idList = new List<int>();
        string sql = 
        @"select distinct fish.id
        from fish
        inner join positions_local
            on fish.id = positions_local.id
        where fish.id is not null
            and positions_local.z is not null";
        // limit 30";
            // and (fish.id = 2033 or fish.id = 2037)";

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


    private NpgsqlBatchCommand NewMetadataBatchCommand(int key)
    {
        string sql = string.Format(
            @"SELECT f.id,  f.species, s.name, f.tl, f.weight, f.sex, f.comment, min(p.timestamp) as minTime, max(p.timestamp) as maxTime
            FROM FISH f 
            left join species s ON f.species = s.ID 
            left join positions_local p on f.id = p.id
            where f.id = {0}
            and s.name != 'Beacon' and p.timestamp is not null
            group by f.id, f.species, s.name, f.tl, f.weight, f.sex, f.comment", key);
        
        return new NpgsqlBatchCommand(sql);
    }

    public async Task<Dictionary<int, FishPacket>> GetFishMetadata(List<int> keyList)
    {
        Dictionary<int, FishPacket> returnDict = new Dictionary<int, FishPacket>();
        await using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            await connection.OpenAsync();

            NpgsqlBatch batch = new NpgsqlBatch(connection);
            foreach (int key in keyList) { batch.BatchCommands.Add(NewMetadataBatchCommand(key)); }

            await using (NpgsqlDataReader rdr = await batch.ExecuteReaderAsync())
            {
                for (int i=0; i < keyList.Count; i++)
                {
                    // No fish metadata could be recovered
                    if (!rdr.HasRows) { rdr.NextResult(); }
                    else
                    {
                        while (await rdr.ReadAsync())
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

                            FishPacket thisPacket = new FishPacket(id, captureType, length, speciesCode, weight, speciesName, male, earliestTimestamp, latestTimestamp);
                            returnDict[id] = thisPacket;
                        };
                    }

                    rdr.NextResult();
                }

                await rdr.CloseAsync();
            }

            await connection.CloseAsync();
        }

        return returnDict;
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
        @"SELECT timestamp,
            depth,
            temperature,
            oxygen,
            (select min(timestamp) from thermocline where timestamp >= TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS')
                AND timestamp IS NOT null
                and (temperature is not null
                    or oxygen is not null)) next_timestamp
        FROM thermocline
        where timestamp = (select max(timestamp) from thermocline where timestamp <= TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS')
            AND timestamp IS NOT null
            and (temperature is not null
                or oxygen is not null))
        order by timestamp", strTime);

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
                        catch { Debug.Log("Thermocline data conversion fail: nextTimestamp"); }
                    }   

                    // Never null by the architecture of the DB
                    float depth = float.MaxValue;
                    entry = rdr.GetValue(rdr.GetOrdinal("depth"));
                    try { depth = Convert.ToSingle(entry); }
                    catch { Debug.Log("Thermocline data conversion fail: depth"); }

                    float? temperature = null;
                    entry = rdr.GetValue(rdr.GetOrdinal("temperature"));
                    if (!DBNull.Value.Equals(entry))
                    {
                        try { temperature = Convert.ToSingle(entry); }
                        catch { Debug.Log("Thermocline data conversion fail: temperature"); }
                    }   

                    float? oxygen = null;
                    entry = rdr.GetValue(rdr.GetOrdinal("oxygen"));
                    if (!DBNull.Value.Equals(entry))
                    {
                        try { oxygen = Convert.ToSingle(entry); }
                        catch { Debug.Log("Thermocline data conversion fail: oxygen"); }
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
}

// SUPPORT CLASS
public class CommandWrapper
{
    public int id { get; set; }
    public DateTime queryTime { get; set; }
    public bool forwardOnly { get; set; }
    public NpgsqlBatchCommand sql { get; set; }

    public CommandWrapper(int id, DateTime queryTime, bool forwardOnly)
    {
        this.id = id;
        this.queryTime = queryTime;
        this.forwardOnly = forwardOnly;
        this.sql = NewPositionBatchCommand(id, queryTime, forwardOnly);
    }

    private static NpgsqlBatchCommand NewPositionBatchCommand(int key, DateTime timestamp, bool forwardOnly)
    {
        string sql = "";
        // Debug.Log(string.Format("{0}: Creating batch query", key));

        if (forwardOnly)
        {
            // Get 100 data points in the future only
            sql = string.Format(
                @"SELECT * FROM 
                    (SELECT q.id, q.timestamp, q.x, q.y, q.z
                    FROM 
                        (SELECT p.id, p.timestamp, p.x, p.y, p.z, |/((p.x - lag(p.x, 1) OVER ( ORDER BY p.timestamp )) ^ 2 + (p.y - lag(p.y, 1) OVER ( ORDER BY p.timestamp )) ^ 2 + (p.z - lag(p.z, 1) OVER ( ORDER BY p.timestamp )) ^ 2) as leading_distance
                        FROM positions_local p 
                        WHERE p.id = {0}
                            AND p.timestamp IS NOT NULL
                            AND p.x IS NOT NULL
                            AND p.y IS NOT NULL
                            AND p.z IS NOT null
                            AND p.timestamp <= TO_TIMESTAMP('{1}', 'YYYY-MM-DD HH24:MI:SS')
                        ORDER BY p.timestamp desc) q
                    WHERE (q.leading_distance > {2} or q.leading_distance is null)
                    LIMIT 100) a
                ORDER BY a.timestamp", key, timestamp.ToString("yyyy-MM-dd HH:mm:ss"), UserSettings.cutoffDist);
        }
        else
        {
            // Get 100 data points on each side of the supplied time
            sql = string.Format(
                @"(SELECT * FROM 
                    (SELECT q.id, q.timestamp, q.x, q.y, q.z
                    FROM 
                        (SELECT p.id, p.timestamp, p.x, p.y, p.z, |/((p.x - lag(p.x, 1) OVER ( ORDER BY p.timestamp )) ^ 2 + (p.y - lag(p.y, 1) OVER ( ORDER BY p.timestamp )) ^ 2 + (p.z - lag(p.z, 1) OVER ( ORDER BY p.timestamp )) ^ 2) as leading_distance
                        FROM positions_local p 
                        WHERE p.id = {0}
                            AND p.timestamp IS NOT NULL
                            AND p.x IS NOT NULL
                            AND p.y IS NOT NULL
                            AND p.z IS NOT null
                            AND p.timestamp <= TO_TIMESTAMP('{1}', 'YYYY-MM-DD HH24:MI:SS')
                        ORDER BY p.timestamp desc) q
                    WHERE (q.leading_distance > {2} or q.leading_distance is null)
                    LIMIT 100) a
                ORDER BY a.timestamp)
                UNION ALL
                SELECT * FROM
                    (SELECT q.id, q.timestamp, q.x, q.y, q.z
                    FROM 
                        (SELECT p.id, p.timestamp, p.x, p.y, p.z, |/((p.x - lag(p.x, 1) OVER ( ORDER BY p.timestamp )) ^ 2 + (p.y - lag(p.y, 1) OVER ( ORDER BY p.timestamp )) ^ 2 + (p.z - lag(p.z, 1) OVER ( ORDER BY p.timestamp )) ^ 2) as leading_distance
                        FROM positions_local p 
                        WHERE p.id = {0}
                            AND p.timestamp IS NOT NULL
                            AND p.x IS NOT NULL
                            AND p.y IS NOT NULL
                            AND p.z IS NOT null
                            AND p.timestamp > TO_TIMESTAMP('{1}', 'YYYY-MM-DD HH24:MI:SS')
                        ORDER BY p.timestamp) q
                    WHERE (q.leading_distance > {2} or q.leading_distance is null)
                    LIMIT 100) w", key, timestamp.ToString("yyyy-MM-dd HH:mm:ss"), UserSettings.cutoffDist);
        }

        return new NpgsqlBatchCommand(sql);
    }
}