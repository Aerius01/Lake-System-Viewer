using Npgsql;
using UnityEngine;
using System;
using System.Data;
using System.Net;   
using System.Threading.Tasks;
using System.Threading;
using System.Linq;
using System.Collections.Generic;
using System.Globalization;

public class DatabaseConnection
{
    private static int counter = 0;
    private static string connString;
    public static string host { get; private set; }
    public static List<int> requestedIDs {get; private set;}
    private static List<CommandWrapper> forwardBatch, doubleSidedBatch;
    private static readonly object locker = new object();
    private static readonly object listLocker = new object();
    public static bool queuedQueries { get { return forwardBatch.Any() || doubleSidedBatch.Any(); } }
    public static bool querying { get; private set; }
    private static bool? smallSample = true;
    // true: 2033 & 2037
    // false: 30 fish
    // null: all fish

    static DatabaseConnection()
    {
        forwardBatch = new List<CommandWrapper>();
        doubleSidedBatch = new List<CommandWrapper>();
        DatabaseConnection.querying = false;
        DatabaseConnection.requestedIDs = new List<int>();
    }

    public static void SetConnectionString(string connString, string host) { DatabaseConnection.connString = connString; DatabaseConnection.host = host;}

    public static void QueuePositionBatchCommand(int id, DateTime queryRootTimestamp, bool forwardOnly=true)
    {
        // Ensure that each ID has at most only a single query lined up
        lock(DatabaseConnection.locker)
        {
            if (!forwardOnly)
            {
                // Remove the previously lined up query and add it with the updated time
                if (doubleSidedBatch.Where(i => i.id == id).FirstOrDefault() != null) doubleSidedBatch.RemoveAt(doubleSidedBatch.IndexOf(doubleSidedBatch.Where(i => i.id == id).FirstOrDefault()));
                doubleSidedBatch.Add(new CommandWrapper(id, queryRootTimestamp, forwardOnly));

                // If the same fish has a query in the forward only queue, remove it
                if (forwardBatch.Where(i => i.id == id).FirstOrDefault() != null) forwardBatch.RemoveAt(forwardBatch.IndexOf(forwardBatch.Where(i => i.id == id).FirstOrDefault()));
            }
            else
            {
                // Remove the previously lined up query and add it with the updated time
                if (forwardBatch.Where(i => i.id == id).FirstOrDefault() != null) forwardBatch.RemoveAt(forwardBatch.IndexOf(forwardBatch.Where(i => i.id == id).FirstOrDefault()));
                forwardBatch.Add(new CommandWrapper(id, queryRootTimestamp, forwardOnly));

                // If the same fish has a query in the double-sided queue, remove it
                if (doubleSidedBatch.Where(i => i.id == id).FirstOrDefault() != null) doubleSidedBatch.RemoveAt(doubleSidedBatch.IndexOf(doubleSidedBatch.Where(i => i.id == id).FirstOrDefault()));
            }
        }
    }

    public static bool QueryIsQueued(int id)
    {
        if (DatabaseConnection.requestedIDs.Contains(id)) return true;
        else return false;
    }

    public static async Task BatchAndRunPositionQueries()
    {
        // Establish base parameters for the querying instance
        DatabaseConnection.querying = true;
        lock(DatabaseConnection.locker)
        {
            foreach (CommandWrapper commandWrapper in DatabaseConnection.forwardBatch) DatabaseConnection.requestedIDs.Add(commandWrapper.id);
            foreach (CommandWrapper commandWrapper in DatabaseConnection.doubleSidedBatch) DatabaseConnection.requestedIDs.Add(commandWrapper.id);
            DatabaseConnection.requestedIDs.Sort();
        }
        
        int thisIteration = DatabaseConnection.counter;
        DatabaseConnection.counter++;
        Debug.Log(string.Format("Pull: {0}", thisIteration));

        // Async-related inits
        BufferTimer bufferTimer = Buffer.InitializeTimer();
        CancellationTokenSource cts = new CancellationTokenSource();
        cts.CancelAfter(30000); // cancel if query is not complete after 30s
        List<Task> queryingTasks = new List<Task>();

        // Parallelize forward queries
        if (DatabaseConnection.forwardBatch.Any())
        {
            int chunkSize = (int)Mathf.Ceil(DatabaseConnection.forwardBatch.Count / 30);
            List<List<CommandWrapper>> partialBatchLists = new List<List<CommandWrapper>>();

            // Chunk all the queries into batches, and then reset the query collection container
            lock(DatabaseConnection.locker)
            {
                partialBatchLists = Tools.ChunkList(DatabaseConnection.forwardBatch, chunkSize < 1 ? 1 : chunkSize);
                DatabaseConnection.forwardBatch = new List<CommandWrapper>();
            }

            // Wrap each batch query in a task and catalogue that task
            foreach (List<CommandWrapper> partialList in partialBatchLists)
            {
                NpgsqlBatch partialBatch = new NpgsqlBatch();
                foreach (CommandWrapper command in partialList) { partialBatch.BatchCommands.Add(command.sql); }

                Task runner = Task.Run(() => RunQueuedPositionQueries(partialBatch, cts.Token), cts.Token);
                queryingTasks.Add(runner);
            }
        }

        // Parallelize double-sided queries
        if (DatabaseConnection.doubleSidedBatch.Any())
        {
            int chunkSize = (int)Mathf.Ceil(DatabaseConnection.doubleSidedBatch.Count / 30);
            List<List<CommandWrapper>> partialBatchLists = new List<List<CommandWrapper>>();

            // Chunk all the queries into batches, and then reset the query collection container
            lock(DatabaseConnection.locker)
            {
                partialBatchLists = Tools.ChunkList(DatabaseConnection.doubleSidedBatch, chunkSize < 1 ? 1 : chunkSize);
                DatabaseConnection.doubleSidedBatch = new List<CommandWrapper>();
            }

            // Wrap each batch query in a task and catalogue that task
            foreach (List<CommandWrapper> partialList in partialBatchLists)
            {
                List<int> localIDs = new List<int>();
                NpgsqlBatch partialBatch = new NpgsqlBatch();
                foreach (CommandWrapper command in partialList) partialBatch.BatchCommands.Add(command.sql);

                Task runner = Task.Run(() => RunQueuedPositionQueries(partialBatch, cts.Token, false), cts.Token);
                queryingTasks.Add(runner);
            }
        }

        // Synch fall-back condition (in case all tasks finish immediately)
        bool syncRunThrough = true;
        foreach (Task task in queryingTasks) { if (task.Status != TaskStatus.RanToCompletion) { syncRunThrough = false; break; } }

        if (!syncRunThrough)
        {
            // The NPGSQL ExecuteReaderAsync() method runs indefinitely if the connection is lost while querying, and doesn't exit despite a cancelled token. Need to cancel it manually.
            Task completionTask = Task.WhenAll(queryingTasks.ToArray()); // Task that completes once all chunked pulls have respectively completed, faulted or cancelled
            Task manualCancellation = Task.Delay(30000); // Simple task that waits 30s before completing
            Task endingTask = Task.WhenAny(completionTask, manualCancellation); // Task that completes when either the pull finishes naturally, or the timer is reached

            await endingTask;
            if (endingTask == manualCancellation) { Debug.Log(string.Format("Ending pull {0}: Manual cancel", thisIteration)); } // Manual cancellation finished first. Pull cancel has been triggered but is stuck and may run for a long time.
            else 
            { 
                if (cts.Token.IsCancellationRequested) { Debug.Log(string.Format("Ending pull {0}: Natural Cancellation", thisIteration)); } // The pull finished naturally, but at least one of the chunks failed
                else Debug.Log(string.Format("Ending pull {0}: Natural", thisIteration)); // clean pull
            }
        }

        bufferTimer.StopTiming();
        DatabaseConnection.querying = false;
        lock(DatabaseConnection.locker) DatabaseConnection.requestedIDs = new List<int>();

        return;
    }

    public static async Task RunQueuedPositionQueries(NpgsqlBatch queries, CancellationToken token, bool forwardOnly=true)
    {
        // Actually execute the batch query
        List<Task> tasks = new List<Task>();

        await using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            if (await DatabaseConnection.Connect(connection, token))
            {    
                try
                {   
                    NpgsqlBatch batch = new NpgsqlBatch(connection);
                    foreach (NpgsqlBatchCommand command in queries.BatchCommands) batch.BatchCommands.Add(command);

                    // Only proceed if we can successfully ping the address
                    Pinger pinger = new Pinger();
                    if (pinger.PingHost())
                    {   
                        await using (NpgsqlDataReader rdr = await batch.ExecuteReaderAsync(token))
                        {
                            for (int i=0; i < batch.BatchCommands.Count; i++)
                            {
                                if (token.IsCancellationRequested) throw new Exception();

                                // No position data could be recovered
                                if (!rdr.HasRows) { await rdr.NextResultAsync(token); }
                                else
                                {
                                    bool firstRunThrough = true;
                                    int fishID = 0;
                                    List<DataPacket> returnPackets = new List<DataPacket>();
                                    while (await rdr.ReadAsync(token))
                                    {
                                        if (token.IsCancellationRequested) throw new Exception();

                                        // Nullity handled by SQL query
                                        int id = rdr.GetInt32(rdr.GetOrdinal("id"));
                                        DateTime timestamp = rdr.GetDateTime(rdr.GetOrdinal("timestamp"));

                                        if (firstRunThrough)
                                        {
                                            fishID = id;
                                            firstRunThrough = false;
                                        }

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
                                        try 
                                        { 
                                            z = Convert.ToSingle(entry); 
                                            if (z > 0) z = -z;
                                        }
                                        catch { Debug.Log("Position conversion fail: z"); }

                                        DataPacket thisPacket = new DataPacket(id, timestamp, x, y, z);
                                        returnPackets.Add(thisPacket);
                                    };

                                    // Task that is completely independent of next iteration
                                    if (!token.IsCancellationRequested)
                                    {
                                        Task task = Task.Run(() => FishManager.fishDict[returnPackets[0].id].UpdatePositionCache(returnPackets, forwardOnly, token), token);
                                        tasks.Add(task);

                                        lock(DatabaseConnection.locker) { DatabaseConnection.requestedIDs.Remove(fishID); }
                                    }
                                }

                                rdr.NextResult();
                            }

                            await rdr.CloseAsync();
                        }

                        await connection.CloseAsync();
                    }
                    else 
                    {
                        await connection.CloseAsync();
                        throw new Exception();
                    }
                }
                catch (Exception) when (token.IsCancellationRequested) { ; }
                catch (Exception) { ; }
            }
        }

        // Ensure all fish caches are updated, or have cancelled gracefully
        await Task.WhenAll(tasks.ToArray());
    }

    public static List<int> GetFishKeys()
    {
        DateTime startTime = DateTime.Now;

        List<int> idList = null;
        string sql = "select distinct fish.id from fish where fish.id is not null";

        if (DatabaseConnection.smallSample == true)
        {
            string addon = " and (fish.id = 2033 or fish.id = 2037)";
            sql = sql + addon;
        }
        else if (DatabaseConnection.smallSample == false)
        {
            string addon = " limit 30";
            sql = sql + addon;
        }

        using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            connection.Open();
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            if (connection.State != ConnectionState.Open) throw new Exception();

            Pinger pinger = new Pinger();
            if (pinger.PingHost())
            {
                using (NpgsqlDataReader rdr = cmd.ExecuteReader())
                {
                    idList = new List<int>();
                    if (!rdr.HasRows) throw new Exception(); 

                    while (rdr.Read())
                    {
                        int id = rdr.GetInt32(rdr.GetOrdinal("id"));
                        idList.Add(id);
                    };

                    rdr.Close();
                }

                connection.Close();
            }
            else 
            {
                connection.Close();
                throw new Exception();
            }
        }

        Debug.Log(string.Format("Key fetching: {0}; number of keys: {1}", (DateTime.Now - startTime).TotalSeconds, idList.Count));
        return idList;
    }


    private NpgsqlBatchCommand NewMetadataBatchCommand(int key, List<string> presentFishColumns)
    {
        string sql = string.Format(
            @"SELECT f.id, {0}, min(p.timestamp) as minTime, max(p.timestamp) as maxTime
            FROM FISH f
            left join positions_local p on f.id = p.id
            where f.id = {1} and p.timestamp is not null
            group by f.id, {0}", string.Join(", ", presentFishColumns), key
        );

        if (TableImports.tables[TableImports.checkTables[5]].imported && presentFishColumns.Contains("f.species"))
        {
            sql = string.Format(
                @"SELECT f.id, {0}, s.name, min(p.timestamp) as minTime, max(p.timestamp) as maxTime
                FROM FISH f
                left join species s ON f.species = s.ID
                left join positions_local p on f.id = p.id
                where f.id = {1}
                and s.name != 'Beacon' and p.timestamp is not null
                group by f.id, {0}, s.name", string.Join(", ", presentFishColumns), key
            );
        }

        return new NpgsqlBatchCommand(sql);
    }

    public async Task<Dictionary<int, FishPacket>> GetFishMetadata(List<int> keyList)
    {
        Dictionary<int, FishPacket> returnDict = new Dictionary<int, FishPacket>();
        await using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            if (await DatabaseConnection.Connect(connection))
            {
                // We need to know what to query based on the present columns in the fish import
                List<string> optionalFishColumns = new List<string>() { "species", "length", "tl", "weight", "sex", "comment" };
                List<string> presentFishColumns = new List<string>();
                foreach (string column in optionalFishColumns) { if (TableImports.tables[TableImports.checkTables[0]].presentColumns.Contains(column)) { presentFishColumns.Add("f." + column); } }

                NpgsqlBatch batch = new NpgsqlBatch(connection);
                foreach (int key in keyList) { batch.BatchCommands.Add(NewMetadataBatchCommand(key, presentFishColumns)); }

                Pinger pinger = new Pinger();
                if (pinger.PingHost())
                {
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
                                    if (presentFishColumns.Contains("f.comment"))
                                    {
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
                                    }

                                    int? length = null;
                                    if (presentFishColumns.Contains("f.tl"))
                                    {
                                        var entry = rdr.GetValue(rdr.GetOrdinal("tl"));
                                        if (!DBNull.Value.Equals(entry))
                                        {
                                            try { length = Convert.ToInt32(entry); }
                                            catch { Debug.Log("Metadata conversion fail: length"); }
                                        }
                                    }
                                    else if (presentFishColumns.Contains("f.length"))
                                    {
                                        var entry = rdr.GetValue(rdr.GetOrdinal("length"));
                                        if (!DBNull.Value.Equals(entry))
                                        {
                                            try { length = Convert.ToInt32(entry); }
                                            catch { Debug.Log("Metadata conversion fail: length"); }
                                        }
                                    }

                                    int? speciesCode = null;
                                    if (presentFishColumns.Contains("f.species"))
                                    {
                                        var entry = rdr.GetValue(rdr.GetOrdinal("species"));
                                        if (!DBNull.Value.Equals(entry))
                                        {
                                            try { speciesCode = Convert.ToInt32(entry); }
                                            catch { Debug.Log("Metadata conversion fail: species code"); }
                                        }
                                    }
                                    

                                    int? weight = null;
                                    if (presentFishColumns.Contains("f.weight"))
                                    {
                                        var entry = rdr.GetValue(rdr.GetOrdinal("weight"));
                                        if (!DBNull.Value.Equals(entry))
                                        {
                                            try { weight = Convert.ToInt32(entry); }
                                            catch { Debug.Log("Metadata conversion fail: weight"); }
                                        }
                                    }

                                    string speciesName = null;
                                    if (TableImports.tables[TableImports.checkTables[5]].imported && presentFishColumns.Contains("f.species"))
                                    { 
                                        var entry = rdr.GetValue(rdr.GetOrdinal("name"));
                                        if (!DBNull.Value.Equals(entry))
                                        {
                                            try
                                            {
                                                speciesName = Convert.ToString(entry);
                                                if (String.IsNullOrEmpty(speciesName)) speciesName = null;
                                            }
                                            catch { Debug.Log("Metadata conversion fail: species name"); }
                                        }
                                    }

                                    bool? male = null;
                                    if (presentFishColumns.Contains("f.sex"))
                                    {
                                        var entry = rdr.GetValue(rdr.GetOrdinal("sex"));
                                        if (!DBNull.Value.Equals(entry))
                                        {
                                            string tempString = Convert.ToString(entry);
                                            if (tempString.Contains('f')) male = false;
                                            else if (tempString.Contains('m')) male = true;
                                        }
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
                else
                {
                    await connection.CloseAsync();
                    throw new Exception();
                }
            }
        }

        return returnDict;
    }

    public static async Task<WeatherPacket> GetWeatherData()
    {
        // https://stackoverflow.com/questions/8145479/can-constructors-be-async
        string strTime = TimeManager.instance.currentTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);
        WeatherPacket returnPacket = null;

        List<string> optionalWeatherColumns = new List<string>() { "windspeed", "winddirection", "temperature", "humidity", "airpressure", "precipitation" };
        List<string> presentWeatherColumns = new List<string>();
        foreach (string column in optionalWeatherColumns) { if (TableImports.tables[TableImports.checkTables[6]].presentColumns.Contains(column)) { presentWeatherColumns.Add(column); } }

        string sql = string.Format(
            @"SELECT timestamp, {0}, LEAD(timestamp, 1) OVER ( ORDER BY timestamp ) next_timestamp
            FROM weatherstation
            where timestamp = (select max(timestamp) from weatherstation where timestamp <= TO_TIMESTAMP('{2}', 'YYYY-MM-DD HH24:MI:SS')
                AND timestamp IS NOT null
                and ({1} is not null))
            or timestamp = (select min(timestamp) from weatherstation where timestamp > TO_TIMESTAMP('{2}', 'YYYY-MM-DD HH24:MI:SS')
                AND timestamp IS NOT null
                and ({1} is not null))
            AND timestamp IS NOT null
            order by timestamp
            limit 1", string.Join(", ", presentWeatherColumns), string.Join(" is not null or ", presentWeatherColumns), strTime);

        await using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            if (await DatabaseConnection.Connect(connection))
            {
                NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                Pinger pinger = new Pinger();
                if (pinger.PingHost())
                {
                    await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
                    {
                        if (!rdr.HasRows) { Debug.Log("Weather SQL query yielded empty dataset"); throw new Exception(); }

                        while (await rdr.ReadAsync())
                        {
                            DateTime timestamp = rdr.GetDateTime(rdr.GetOrdinal("timestamp"));

                            DateTime? nextTimestamp = null;
                            var entry = rdr.GetValue(rdr.GetOrdinal("next_timestamp"));
                            if (!DBNull.Value.Equals(entry))
                            {
                                try { nextTimestamp = Convert.ToDateTime(entry); }
                                catch { Debug.Log("Weather data conversion fail: nextTimestamp"); }
                            }

                            float? temperature = null;
                            entry = rdr.GetValue(rdr.GetOrdinal("temperature"));
                            if (!DBNull.Value.Equals(entry))
                            {
                                try { temperature = Convert.ToSingle(entry); }
                                catch { Debug.Log("Weather data conversion fail: temperature"); }
                            }

                            float? windspeed = null;
                            if (presentWeatherColumns.Contains("windspeed"))
                            {
                                entry = rdr.GetValue(rdr.GetOrdinal("windspeed"));
                                if (!DBNull.Value.Equals(entry))
                                {
                                    try { windspeed = Convert.ToSingle(entry); }
                                    catch { Debug.Log("Weather data conversion fail: windspeed"); }
                                }
                            }

                            float? winddirection = null;
                            if (presentWeatherColumns.Contains("winddirection"))
                            {
                                entry = rdr.GetValue(rdr.GetOrdinal("winddirection"));
                                if (!DBNull.Value.Equals(entry))
                                {
                                    try { winddirection = Convert.ToSingle(entry); }
                                    catch { Debug.Log("Weather data conversion fail: winddirection"); }
                                }
                            }

                            float? humidity = null;
                            if (presentWeatherColumns.Contains("humidity"))
                            {
                                entry = rdr.GetValue(rdr.GetOrdinal("humidity"));
                                if (!DBNull.Value.Equals(entry))
                                {
                                    try { humidity = Convert.ToSingle(entry); }
                                    catch { Debug.Log("Weather data conversion fail: humidity"); }
                                }
                            }

                            float? airpressure = null;
                            if (presentWeatherColumns.Contains("airpressure"))
                            {
                                entry = rdr.GetValue(rdr.GetOrdinal("airpressure"));
                                if (!DBNull.Value.Equals(entry))
                                {
                                    try { airpressure = Convert.ToSingle(entry); }
                                    catch { Debug.Log("Weather data conversion fail: airpressure"); }
                                }
                            }

                            float? precipitation = null;
                            if (presentWeatherColumns.Contains("precipitation"))
                            {
                                entry = rdr.GetValue(rdr.GetOrdinal("precipitation"));
                                if (!DBNull.Value.Equals(entry))
                                {
                                    try { precipitation = Convert.ToSingle(entry); }
                                    catch { Debug.Log("Weather data conversion fail: precipitation"); }
                                }
                            }

                            returnPacket = new WeatherPacket(timestamp, nextTimestamp, windspeed, winddirection, temperature, humidity, airpressure, precipitation);
                        };

                        await rdr.CloseAsync();
                    }

                    await connection.CloseAsync();
                }
                else 
                {
                    await connection.CloseAsync();
                    throw new Exception();
                }
            }
        }

        return returnPacket;
    }

    public static async Task<DateTime[]> GetWeatherMinMaxTimes()
    {
        DateTime earliestTimestamp = DateTime.MaxValue;
        DateTime latestTimestamp = DateTime.MinValue;

        List<string> optionalWeatherColumns = new List<string>() { "windspeed", "winddirection", "temperature", "humidity", "airpressure", "precipitation" };
        List<string> presentWeatherColumns = new List<string>();
        foreach (string column in optionalWeatherColumns) { if (TableImports.tables[TableImports.checkTables[6]].presentColumns.Contains(column)) { presentWeatherColumns.Add(column); } }

        string sql = string.Format(
            @"SELECT max(timestamp), min(timestamp)
            FROM weatherstation
            where {0} is not null AND timestamp IS NOT null", string.Join(" is not null or ", presentWeatherColumns));

        await using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            if (await DatabaseConnection.Connect(connection))
            {
                NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                Pinger pinger = new Pinger();
                if (pinger.PingHost())
                {
                    await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
                    {
                        if (!rdr.HasRows) { Debug.Log("Weather MaxMin SQL query yielded empty dataset"); throw new Exception(); }

                        while (await rdr.ReadAsync())
                        {
                            earliestTimestamp = DateTime.Compare(earliestTimestamp, rdr.GetDateTime(rdr.GetOrdinal("min"))) < 0 ? earliestTimestamp : rdr.GetDateTime(rdr.GetOrdinal("min"));
                            latestTimestamp = DateTime.Compare(latestTimestamp, rdr.GetDateTime(rdr.GetOrdinal("min"))) > 0 ? latestTimestamp : rdr.GetDateTime(rdr.GetOrdinal("max"));
                        };

                        await rdr.CloseAsync();
                    }

                    await connection.CloseAsync();
                }
                else 
                {
                    await connection.CloseAsync();
                    throw new Exception();
                }
            }
        }

        return new DateTime[2] { earliestTimestamp, latestTimestamp };
    }

    public async static Task<ThermoPacket> GetThermoData()
    {
        string strTime = TimeManager.instance.currentTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        // Inits
        List<ThermoReading> readings = new List<ThermoReading>();
        DateTime timestamp = DateTime.MaxValue;
        DateTime? nextTimestamp = null;

        string sql = string.Format(
        @"SELECT timestamp,
            depth,
            temperature,
            (select min(timestamp) from thermocline where timestamp >= TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS')
                AND timestamp IS NOT null
                and temperature is not null) next_timestamp
        FROM thermocline
        where timestamp = (select max(timestamp) from thermocline where timestamp <= TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS')
            AND timestamp IS NOT null
            and temperature is not null)
        order by timestamp", strTime);

        if (TableImports.tables[TableImports.checkTables[7]].presentColumns.Contains("oxygen"))
        {
            sql = string.Format(
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
        }

        await using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            if (await DatabaseConnection.Connect(connection))
            {
                NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                Pinger pinger = new Pinger();
                if (pinger.PingHost())
                {
                    await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
                    {
                        if (!rdr.HasRows) { Debug.Log("Thermo SQL query yielded empty dataset"); throw new Exception(); }

                        while (await rdr.ReadAsync())
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
                            try 
                            { 
                                depth = Convert.ToSingle(entry);
                                if (depth < 0) depth = - depth;
                            }
                            catch { Debug.Log("Thermocline data conversion fail: depth"); }

                            float? temperature = null;
                            entry = rdr.GetValue(rdr.GetOrdinal("temperature"));
                            if (!DBNull.Value.Equals(entry))
                            {
                                try { temperature = Convert.ToSingle(entry); }
                                catch { Debug.Log("Thermocline data conversion fail: temperature"); }
                            }

                            float? oxygen = null;
                            if (TableImports.tables[TableImports.checkTables[7]].presentColumns.Contains("oxygen"))
                            {
                                entry = rdr.GetValue(rdr.GetOrdinal("oxygen"));
                                if (!DBNull.Value.Equals(entry))
                                {
                                    try { oxygen = Convert.ToSingle(entry); }
                                    catch { Debug.Log("Thermocline data conversion fail: oxygen"); }
                                }
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

                        await rdr.CloseAsync();
                    }

                    await connection.CloseAsync();
                }
                else
                {
                    await connection.CloseAsync();
                    throw new Exception();
                }
            }
        }

        if (readings.Any()) return new ThermoPacket(timestamp, nextTimestamp, readings);
        else return null;
    }

    public static Tuple<DateTime, DateTime, float> GetThermoMinMaxes()
    {
        DateTime earliestTimestamp = DateTime.MaxValue;
        DateTime latestTimestamp = DateTime.MinValue;
        float maxDepth = float.MinValue;

        string sql =
        @"SELECT max(timestamp), min(timestamp), max(depth) as max_depth
        FROM thermocline
        where temperature is not null
            AND timestamp IS NOT null";

        if (TableImports.tables[TableImports.checkTables[7]].presentColumns.Contains("oxygen"))
        {
            sql =
            @"SELECT max(timestamp), min(timestamp), max(depth) as max_depth
            FROM thermocline
            where (temperature is not null
                    or oxygen is not null)
                AND timestamp IS NOT null";
        }

        using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            if (connection.State != ConnectionState.Open)
            {
                int runningCount = 0;
                while (runningCount < 10)
                {
                    try { connection.Open(); }
                    catch (NpgsqlException) { ; }

                    if (connection.State == ConnectionState.Open) break;
                    runningCount++;
                }
            }

            if (connection.State != ConnectionState.Open) throw new Exception();

            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            Pinger pinger = new Pinger();
            if (pinger.PingHost())
            {
                using (NpgsqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (!rdr.HasRows) { Debug.Log("Thermo MaxMin SQL query yielded empty dataset"); throw new Exception(); }

                    while (rdr.Read())
                    {
                        earliestTimestamp = DateTime.Compare(earliestTimestamp, rdr.GetDateTime(rdr.GetOrdinal("min"))) < 0 ? earliestTimestamp : rdr.GetDateTime(rdr.GetOrdinal("min"));
                        latestTimestamp = DateTime.Compare(latestTimestamp, rdr.GetDateTime(rdr.GetOrdinal("min"))) > 0 ? latestTimestamp : rdr.GetDateTime(rdr.GetOrdinal("max"));
                        maxDepth = Convert.ToSingle(rdr.GetValue(rdr.GetOrdinal("max_depth")));
                    };

                    rdr.Close();
                }

                connection.Close();
            }
            else
            {
                connection.Close();
                throw new Exception();
            }
        }

        return new Tuple<DateTime, DateTime, float>(earliestTimestamp, latestTimestamp, maxDepth);
    }

    public static async Task<DataTable> GetMeshMap()
    {
        DataTable meshTable = null;
        string sql = "SELECT * FROM meshmap";
        
        await using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            if (await DatabaseConnection.Connect(connection))
            {
                NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                Pinger pinger = new Pinger();
                if (pinger.PingHost())
                {
                    await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
                    {
                        // No fish metadata could be recovered
                        if (!rdr.HasRows)
                        {
                            Debug.Log("Meshmap SQL query yielded empty dataset");
                            throw new Exception();
                        }
                        else
                        {
                            meshTable = new DataTable();
                            int columnCount = rdr.FieldCount;
                            int rowCount = 0;

                            // Add the appropriate number of columns
                            for (int i=0; i<columnCount; i++) { meshTable.Columns.Add(i.ToString(), typeof(float)); }

                            while (await rdr.ReadAsync())
                            {
                                DataRow row = meshTable.NewRow();
                                for (int i=0; i<columnCount; i++)
                                {
                                    // Read and set the row value
                                    float value = 0f;
                                    var entry = rdr.GetValue(i);
                                    if (!DBNull.Value.Equals(entry))
                                    {
                                        try { value = Convert.ToSingle(entry); }
                                        catch { Debug.Log(string.Format("Meshmap column data conversion fail at row: {0}, column: {1}", rowCount, i)); }
                                    }
                                    else value = float.MaxValue; // Make it visible

                                    row[i] = value;
                                }

                                // Add the entire row to the table
                                meshTable.Rows.Add(row);
                                rowCount++;
                            };

                            meshTable.AcceptChanges();
                        }
                        await rdr.CloseAsync();
                    }
                    await connection.CloseAsync();
                }
                else
                {
                    await connection.CloseAsync();
                    throw new Exception();
                }
            }
        }

        return meshTable;
    }

    public static DateTime EarliestDate(string tableName)
    {
        DateTime timestamp = DateTime.MaxValue;
        string sql = string.Format("select min(timestamp) from {0}", tableName);

        using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            if (connection.State != ConnectionState.Open)
            {
                int runningCount = 0;
                while (runningCount < 10)
                {
                    try { connection.Open(); }
                    catch (NpgsqlException) { ; }

                    if (connection.State == ConnectionState.Open) break;
                    runningCount++;
                }
            }

            if (connection.State != ConnectionState.Open) throw new Exception();

            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            Pinger pinger = new Pinger();
            if (pinger.PingHost())
            {
                using (NpgsqlDataReader rdr = cmd.ExecuteReader())
                {
                    if (!rdr.HasRows) { Debug.Log(string.Format("Problem getting earliest date from table {0}", tableName)); throw new Exception(); }
                    while (rdr.Read()) { timestamp = rdr.GetDateTime(rdr.GetOrdinal("min")); }

                    rdr.Close();
                }

                connection.Close();
            }
            else
            {
                connection.Close();
                throw new Exception();
            }
        }

        return timestamp;
    }

    public async static Task<PolygonPacket> GetMacromapPolygons()
    {
        string strTime = TimeManager.instance.currentTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        // Inits
        DateTime timestamp = DateTime.MaxValue;
        DateTime? nextTimestamp = null;
        PolygonPacket returnPacket = null;

        string sql = string.Format(
        @"SELECT mp.timestamp,
            mp.poly_id,
            mp.lower,
            mp.upper,
            mp.x,
            mp.y,
            (select min(timestamp) from macromap_polygons_local where timestamp > TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS')
                and x is not null
                and y is not null
                and poly_id is not null
                and lower is not null
                and upper is not null) next_timestamp
        FROM macromap_polygons_local mp
        where mp.timestamp = (select max(timestamp) from macromap_polygons_local where mp.timestamp <= TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS')
            and x is not null
            and y is not null
            and poly_id is not null
            and lower is not null
            and upper is not null)
        order by mp.timestamp", strTime);

        await using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            if (await DatabaseConnection.Connect(connection))
            {
                NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                Pinger pinger = new Pinger();
                if (pinger.PingHost())
                {
                    await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
                    {
                        if (!rdr.HasRows)
                        {
                            Debug.Log("polygon SQL query yielded empty dataset");

                            // Query for earliest TS and make dummy return packet with accurate next_timestamp
                            // TODO: query failure returns DateTime.MaxValue, error handle if earliestTS is this value
                            DateTime earliestTS = DatabaseConnection.EarliestDate("macromap_polygons_local");
                            return new PolygonPacket(null, earliestTS);
                        }

                        while (await rdr.ReadAsync())
                        {
                            timestamp = rdr.GetDateTime(rdr.GetOrdinal("timestamp"));

                            nextTimestamp = null;
                            var entry = rdr.GetValue(rdr.GetOrdinal("next_timestamp"));
                            if (!DBNull.Value.Equals(entry))
                            {
                                try { nextTimestamp = Convert.ToDateTime(entry); }
                                catch { Debug.Log("Polygonal macromap data conversion fail: nextTimestamp"); }
                            }

                            // Never null by the architecture of the query
                            int polyID = 0;
                            entry = rdr.GetValue(rdr.GetOrdinal("poly_id"));
                            try { polyID = Convert.ToInt32(entry); }
                            catch { Debug.Log("Polygonal macromap data conversion fail: poly_ID"); }

                            int lower = 0;
                            entry = rdr.GetValue(rdr.GetOrdinal("lower"));
                            try { lower = Convert.ToInt32(entry); }
                            catch { Debug.Log("Polygonal macromap data conversion fail: lower"); }

                            int upper = 0;
                            entry = rdr.GetValue(rdr.GetOrdinal("upper"));
                            try { upper = Convert.ToInt32(entry); }
                            catch { Debug.Log("Polygonal macromap data conversion fail: upper"); }

                            float x = 0;
                            entry = rdr.GetValue(rdr.GetOrdinal("x"));
                            try { x = Convert.ToSingle(entry); }
                            catch { Debug.Log("Polygonal macromap data conversion fail: x"); }

                            float y = 0;
                            entry = rdr.GetValue(rdr.GetOrdinal("y"));
                            try { y = Convert.ToSingle(entry); }
                            catch { Debug.Log("Polygonal macromap data conversion fail: y"); }

                            // Send off to MacromapPolygon to process
                            if (returnPacket == null) { returnPacket = new PolygonPacket(timestamp, nextTimestamp); }
                            returnPacket.CataloguePacket(polyID, lower, upper, x, y);
                        };

                        await rdr.CloseAsync();
                    }

                    await connection.CloseAsync();
                }
                else 
                {
                    await connection.CloseAsync();
                    throw new Exception();
                }
            }
        }

        return returnPacket;
    }

    public async static Task<MacroHeightPacket> GetMacromapHeights()
    {
        string strTime = TimeManager.instance.currentTime.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture);

        // Inits
        DateTime timestamp = DateTime.MaxValue;
        DateTime? nextTimestamp = null;
        MacroHeightPacket returnPacket = null;

        string sql = string.Format(
            @"select
                *,
                (select min(timestamp) from macromap_heights_local s where s.timestamp > TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS')
                    and s.height_m is not null 
                    and s.x is not null 
                    and s.y is not null) next_timestamp
                from macromap_heights_local mhl
                where mhl.timestamp = (select max(timestamp) from macromap_heights_local where timestamp <= TO_TIMESTAMP('{0}', 'YYYY-MM-DD HH24:MI:SS'))
                    and mhl.height_m is not null 
                    and mhl.x is not null 
                    and mhl.y is not null", 
            strTime
        );

        await using (NpgsqlConnection connection = new NpgsqlConnection(connString))
        {
            if (await DatabaseConnection.Connect(connection))
            {
                NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                Pinger pinger = new Pinger();
                if (pinger.PingHost())
                {
                    await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
                    {
                        if (!rdr.HasRows) { Debug.Log("macromap height SQL query yielded empty dataset"); }
                        else
                        {
                            while (await rdr.ReadAsync())
                            {
                                timestamp = rdr.GetDateTime(rdr.GetOrdinal("timestamp"));

                                nextTimestamp = null;
                                var entry = rdr.GetValue(rdr.GetOrdinal("next_timestamp"));
                                if (!DBNull.Value.Equals(entry))
                                {
                                    try { nextTimestamp = Convert.ToDateTime(entry); }
                                    catch { Debug.Log("Height macromap data conversion fail: nextTimestamp"); }
                                }

                                // Never null by the architecture of the query
                                float x = 0;
                                entry = rdr.GetValue(rdr.GetOrdinal("x"));
                                try { x = Convert.ToSingle(entry); }
                                catch { Debug.Log("Height macromap data conversion fail: x"); }

                                float y = 0;
                                entry = rdr.GetValue(rdr.GetOrdinal("y"));
                                try { y = Convert.ToSingle(entry); }
                                catch { Debug.Log("Height macromap data conversion fail: y"); }

                                float height = 0;
                                entry = rdr.GetValue(rdr.GetOrdinal("height_m"));
                                try { height = Convert.ToSingle(entry); }
                                catch { Debug.Log("Height macromap data conversion fail: height"); }

                                if (returnPacket == null) { returnPacket = new MacroHeightPacket(timestamp, nextTimestamp); }
                                returnPacket.AddPoint(x, y, height);
                            };
                        }
                        await rdr.CloseAsync();
                    }

                    await connection.CloseAsync();
                }
                else
                {
                    await connection.CloseAsync();
                    throw new Exception();
                }
            }
        }

        if (returnPacket != null) returnPacket.CoalesceDictionary();
        return returnPacket;
    }

    public static async Task<bool> Connect(NpgsqlConnection connection, CancellationToken token = default)
    {
        // First start by pinging the address since with a pooled connection, sometimes the ConnectionState is "open" even though the address is unavailable
        Pinger pinger = new Pinger();
        if (pinger.PingHost())
        {
            // If the ping is successful and the connection is already open, we're done
            if (connection.State == ConnectionState.Open) return true;
            else
            {
                // Connections sometimes fail due to pooling connections not being available. The connection attempt therefore needs to iterate.
                int runningCount = 0;
                while (runningCount < 10)
                {
                    if (token != CancellationToken.None) { if (token.IsCancellationRequested) return false; }

                    try { await connection.OpenAsync(); }
                    catch (NpgsqlException) { await Task.Delay(3000); }

                    if (connection.State == ConnectionState.Open) return true;
                    runningCount++;
                }

                return false; // The connection could not be opened
            }
        }
        else return false;
    }
}

// SUPPORT CLASSES
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
            // Get PositionCache.batchSize (=300) data points in the future only
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
                    LIMIT {3}) a
                ORDER BY a.timestamp", key, timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), UserSettings.cutoffDist, PositionCache.batchSize);
        }
        else
        {
            // Get PositionCache.batchSize (=300) data points on each side of the supplied time
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
                    LIMIT {3}) a
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
                    LIMIT {3}) w", key, timestamp.ToString("yyyy-MM-dd HH:mm:ss", CultureInfo.InvariantCulture), UserSettings.cutoffDist, PositionCache.batchSize);
        }

        return new NpgsqlBatchCommand(sql);
    }
}
