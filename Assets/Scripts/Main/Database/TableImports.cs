using UnityEngine;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Data;
using System;
using Npgsql;

public class TableImports
{
    private NpgsqlConnection connection;
    private List<string> tableNames;
    private Dictionary<string, Table> tables;
    private LoaderBar loadingBar;
    private List<string> checkTables;

    // die notwendig sind
    public bool fishAccepted { get; private set; }
    public bool positionsAccepted { get; private set; }
    public bool heightMapAccepted { get; private set; }

    // die optionale sind
    public bool speciesAccepted { get; private set; }
    public bool weatherAccepted { get; private set; }
    public bool thermoclineAccepted { get; private set; }
    public bool macroPolysAccepted { get; private set; }
    public bool macroHeightsAccepted { get; private set; }

    private static readonly object operationLocker = new object();

    public TableImports(NpgsqlConnection connection, LoaderBar loadingBar) { this.connection = connection; this.loadingBar = loadingBar; this.tables = new Dictionary<string, Table>(); }

    public async Task<Tuple<bool, Dictionary<string,Table>>> VerifyTables()
    {
        int runningCount = 0;
        if (connection.State != ConnectionState.Open)
        {
            // This connection tends to have problems. Attempt to re-open the connection if it fails
            while (runningCount < 30)
            {
                runningCount++;
                try { await connection.OpenAsync(); break; }
                catch (NpgsqlException e) { Debug.Log(e.Message); }
            }
        }

        if (runningCount == 30) { return new Tuple<bool, Dictionary<string,Table>>(false, this.tables); } // failed to open
        else
        {
            DataTable schema = await connection.GetSchemaAsync("Tables");

            this.tableNames = new List<string>();
            foreach(DataRow row in schema.Rows) { this.tableNames.Add(row["table_name"].ToString()); }

            // Run through checklist of tables
            return new Tuple<bool, Dictionary<string,Table>>(await this.Verificator(), this.tables);
        }
    }

    private async Task<bool> Verificator()
    {
        // Verify the structure of each table in sequence. These cannot be parallelized since they each rely on
        // querying the same database, and queries cannot be run in parallel (will throw an error).

        // Start by checking how many of the check tables are present. The ordering of these names is important.
        this.checkTables = new List<string>() { "fish", "meshmap", "positions_local", "macromap_polygons_local", "macromap_heights_local", "species", "weatherstation", "thermocline" };
        int tableCounter = 0;
        foreach (string tableName in checkTables) if (this.tableNames.Contains(tableName)) tableCounter += 1;

        this.loadingBar.WakeUp(tableCounter);

        this.loadingBar.SetText(this.checkTables[0]);
        this.fishAccepted = await Task.Run(() => this.CheckFishTable(this.checkTables[0]));

        this.loadingBar.SetText(this.checkTables[1]);
        Tuple<bool, int, int, float, float> heightMapPacket = await Task.Run(() => this.CheckHeightMap(this.checkTables[1]));
        this.heightMapAccepted = heightMapPacket.Item1;

        // These tables are dependent on the successful querying of the heightmap table
        this.positionsAccepted = false;
        this.macroPolysAccepted = false;
        this.macroHeightsAccepted = false;
        if (this.heightMapAccepted)
        {
            this.loadingBar.SetText(this.checkTables[2]);
            this.positionsAccepted = await Task.Run(() => this.CheckPositionsTable(this.checkTables[2], heightMapPacket.Item2, heightMapPacket.Item3, heightMapPacket.Item4, heightMapPacket.Item5));

            this.loadingBar.SetText(this.checkTables[3]);
            this.macroPolysAccepted = await Task.Run(() => this.CheckMacroPolyTable(this.checkTables[3], heightMapPacket.Item2, heightMapPacket.Item3));

            this.loadingBar.SetText(this.checkTables[4]);
            this.macroHeightsAccepted = await Task.Run(() => this.CheckMacroHeightTable(this.checkTables[4], heightMapPacket.Item2, heightMapPacket.Item3));
        }

        this.loadingBar.SetText(this.checkTables[5]);
        this.speciesAccepted = await Task.Run(() => this.CheckSpeciesTable(this.checkTables[5]));

        this.loadingBar.SetText(this.checkTables[6]);
        this.weatherAccepted = await Task.Run(() => this.CheckWeatherTable(this.checkTables[6]));

        this.loadingBar.SetText(this.checkTables[7]);
        this.thermoclineAccepted = await Task.Run(() => this.CheckThermoTable(this.checkTables[7]));

        this.loadingBar.ShutDown();
        
        bool requiredTablesPresent = (this.fishAccepted && this.heightMapAccepted && this.positionsAccepted) ? true : false;
        return requiredTablesPresent;
    }

    private async Task<bool> CheckFishTable(string tableName)
    {
        // CHECKS
        // PRIMARY: The table is present, the table is populated, the id column exists, and the id column is unique
        // WARNING: There should be no null values

        bool conditionsMet = true; 

        // Fish table
        Table fishTable = new Table(tableName);
        lock(operationLocker) this.tables[tableName] = fishTable;

        // PRIMARY: The table must be present
        if (this.tableNames.Contains(tableName))
        {
            // PRIMARY: The IDs must be unique
            string sql = string.Format("select count(id) base_count, count(distinct id) unique_count from {0} where id is not null", tableName);

            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            try
            {   
                await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
                {
                    // PRIMARY: The table must have data
                    if (!rdr.HasRows)
                    {
                        fishTable.SetLight(2);
                        fishTable.SetMessage("No records were recovered while testing for unique IDs. Is this table populated?");
                        conditionsMet = false;
                    }
                    else
                    {
                        while (await rdr.ReadAsync())
                        {
                            int baseCount = rdr.GetInt32(rdr.GetOrdinal("base_count"));
                            int uniqueCount = rdr.GetInt32(rdr.GetOrdinal("unique_count"));

                            if (baseCount != uniqueCount)
                            {
                                fishTable.SetLight(2);
                                fishTable.SetMessage("The ID column has duplicate values. Please ensure that the ID column is unique, and then try again.");
                                conditionsMet = false;
                            }
                        };
                    }
                    await rdr.CloseAsync();
                }
            }
            catch (PostgresException e)
            {
                // PRIMARY: The ID column must exist
                if (e.SqlState == "42703") 
                { 
                    fishTable.SetLight(2);
                    fishTable.SetMessage(string.Format("The connected database's \"{0}\" table does not have a column named \"id\". This column must both exist and be unique.", tableName));
                    conditionsMet = false;
                }
                else
                {
                    fishTable.SetLight(2);
                    fishTable.SetMessage("Unrecognized & unhandled Postgres exception. When querying for fish IDs.");
                    conditionsMet = false;
                    throw e;
                } 
            }
        }
        else 
        {
            fishTable.SetLight(2);
            fishTable.SetMessage(string.Format("The \"{0}\" table was not found in the provided database.", tableName));
            conditionsMet = false;
        }

        // WARNING: There should be no null values
        if (conditionsMet) await this.NullCountCheck(fishTable, new List<string> {"id"});

        // INTERNAL: Determine which of the optional columns are present. This is necessary information for when building out the Fish.cs class.
        // Optional columns are: species, length, weight, sex, capture_type
        if (conditionsMet)
        {
            string sql = string.Format("SELECT * FROM information_schema.columns WHERE table_name = '{0}'", tableName);

            // Check by removing the column names from the list if they're found
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
            {
                if (!rdr.HasRows)
                { 
                    fishTable.SetLight(2); 
                    fishTable.SetMessage("No records were recovered while querying the table schema. Is this table populated? This information is necessary to know which optional columns are present"); 
                    conditionsMet = false; 
                }
                else
                {
                    while (await rdr.ReadAsync())
                    {
                        string columnName = Convert.ToString(rdr.GetValue(rdr.GetOrdinal("column_name")));
                        fishTable.presentColumns.Add(columnName);
                    };
                }
                await rdr.CloseAsync();
            }
        }

        // Debug.Log(string.Format("Table: {0}; Light: {1}; Message: {2}; Columns: \"{3}\"", fishTable.tableName, fishTable.lightColor, fishTable.tableMessage, string.Join(", ", fishTable.presentColumns)));
        return conditionsMet;
    }

    private async Task<Tuple<bool, int, int, float, float>> CheckHeightMap(string tableName)
    {
        // CHECKS
        // PRIMARY: The table is present, the table is populated
        // WARNING: There are no null values

        bool conditionsMet = true; 

        // Heightmap table
        int rowCount = 0;
        int columnCount = 0;
        Table heightMapTable = new Table(tableName);
        this.tables[tableName] = heightMapTable;

        // PRIMARY: The table must be present
        if (this.tableNames.Contains(tableName))
        {
            // We only need the row count and column count to contrast against the positions table
            string sql =  string.Format(@"select
                (select count(*) from {1}) as rows,
                (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE table_catalog = '{0}' AND table_name = '{1}') as columns 
                ", connection.Database, tableName);

            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
            {
                // PRIMARY: The table must have data
                if (!rdr.HasRows)
                {
                    heightMapTable.SetLight(2);
                    heightMapTable.SetMessage("No records were recovered. Is this table populated?");
                    conditionsMet = false;
                }
                else
                {
                    while (await rdr.ReadAsync())
                    {
                        rowCount = rdr.GetInt32(rdr.GetOrdinal("rows"));
                        columnCount = rdr.GetInt32(rdr.GetOrdinal("columns"));
                    };
                }
                await rdr.CloseAsync();
            }
        }
        else 
        {
            heightMapTable.SetLight(2);
            heightMapTable.SetMessage(string.Format("The \"{0}\" table was not found in the provided database.", tableName));
            conditionsMet = false;
        }

        // WARNING: There should be no null values
        float minVal = float.MaxValue;
        float maxVal = float.MinValue;
        if (conditionsMet)
        {
            // Collect all information to procedurally generate the actual SQL statement, such that specific column names aren't necessary
            List<string> columnMapping = new List<string>();
            string sql = string.Format("SELECT column_name FROM information_schema.columns WHERE table_name = '{0}'", tableName);

            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
            {
                // PRIMARY: The table must have data
                if (!rdr.HasRows)
                {
                    heightMapTable.SetLight(2);
                    heightMapTable.SetMessage("No records were recovered when querying the table schema. Is this table populated?");
                    conditionsMet = false;
                }
                else
                {
                    while (await rdr.ReadAsync())
                    {
                        string columnName = Convert.ToString(rdr.GetValue(rdr.GetOrdinal("column_name")));
                        columnMapping.Add(columnName);
                    };
                }
                await rdr.CloseAsync();
            }

            // Now build the actual SQL statement
            sql = string.Format(@"select min(rowMins), max(rowMaxes), sum(nullCount) nullCount
                from (SELECT 
                (SELECT min(v) FROM (VALUES ({0})) AS value(v)) as rowMins,
                (SELECT max(v) FROM (VALUES ({0})) AS value(v)) as rowMaxes,
                (SELECT count(v) FROM (VALUES ({0})) AS value(v) where v is null) as nullCount
                FROM {1}) a", string.Join("), (", columnMapping), tableName)
            ; 

            cmd = new NpgsqlCommand(sql, connection);
            await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
            {
                // PRIMARY: The table must have data
                if (!rdr.HasRows)
                {
                    heightMapTable.SetLight(2);
                    heightMapTable.SetMessage("No records were recovered when querying the table schema. Is this table populated?");
                    conditionsMet = false;
                }
                else
                {
                    while (await rdr.ReadAsync())
                    {
                        minVal = Convert.ToSingle(rdr.GetValue(rdr.GetOrdinal("min")));
                        maxVal = Convert.ToSingle(rdr.GetValue(rdr.GetOrdinal("max")));

                        int nullCount = Convert.ToInt32(rdr.GetValue(rdr.GetOrdinal("nullCount")));
                        if (nullCount > 0)
                        {
                            heightMapTable.SetLight(1); // yellow warning light
                            if (nullCount == 1) heightMapTable.SetMessage(string.Format("{0} cell was found to have a null value. Should you decide to continue without first correcting it, the depth value of that cell will be set to the maximum float value (and accordingly become very visible when rendering).", nullCount)); 
                            else heightMapTable.SetMessage(string.Format("{0} cells were found to have null values. Should you decide to continue without first correcting these, the depth values of those cells will be set to the maximum float value (and accordingly become very visible when rendering).", nullCount)); 
                        }
                    };
                }
                await rdr.CloseAsync();
            }
        }

        // Debug.Log(string.Format("Table: {0}; Light: {1}; Message: {2}", heightMapTable.tableName, heightMapTable.lightColor, heightMapTable.tableMessage));
        return new Tuple<bool, int, int, float, float> (conditionsMet, rowCount, columnCount, minVal, maxVal);
    }

    private async Task<bool> CheckPositionsTable(string tableName, int rowCount, int columnCount, float minDepth, float maxDepth)
    {
        // CHECKS
        // PRIMARY: The table is present, the table is populated, the id, timestamp, x, y and z columns exist
        // WARNING: The positions should fall within the bounds of the height map, there should be no null values

        bool conditionsMet = true; 

        // Positions table
        Table positionsTable = new Table(tableName);
        this.tables[tableName] = positionsTable;
        List<string> requiredColumns = new List<string> {"id", "timestamp", "x", "y", "z"};

        // PRIMARY: The table must be present
        if (this.tableNames.Contains(tableName))
        {
            // PRIMARY: The table must have all of an id, timestamp, x, y and z columns
            conditionsMet = await this.RequiredColumnsCheck(positionsTable, requiredColumns);

            // WARNING: The max/min x, y and z should fall within the boundaries of the height map
            if (conditionsMet)
            {
                string sql = string.Format("SELECT max(x) max_x, min(x) min_x, max(y) max_y, min(y) min_y, max(z) max_z, min(z) min_z from {0}", tableName);

                NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
                {
                    if (!rdr.HasRows)
                    { 
                        positionsTable.SetLight(1); // yellow warning light
                        positionsTable.SetMessage("Was unable to successfully query for bounds testing against the height map bounds.");
                    }
                    else
                    {
                        while (await rdr.ReadAsync())
                        {
                            float maxX = Convert.ToSingle(rdr.GetValue(rdr.GetOrdinal("max_x")));
                            float minX = Convert.ToSingle(rdr.GetValue(rdr.GetOrdinal("min_x")));
                            float maxY = Convert.ToSingle(rdr.GetValue(rdr.GetOrdinal("max_y")));
                            float minY = Convert.ToSingle(rdr.GetValue(rdr.GetOrdinal("min_y")));
                            float maxZ = Convert.ToSingle(rdr.GetValue(rdr.GetOrdinal("max_z")));
                            float minZ = Convert.ToSingle(rdr.GetValue(rdr.GetOrdinal("min_z")));

                            if (minX < 0 || maxX > columnCount || minY < 0 || maxY > rowCount)
                            {
                                positionsTable.SetLight(1); // yellow warning light
                                positionsTable.SetMessage(string.Format("The (x, y) position values in the provided table exceed the local bounds of the heightmap. This will cause some fish to appear to be \"swimming\" on land or in empty space."));
                            }

                            if (minZ < minDepth || maxZ > maxDepth)
                            {
                                positionsTable.SetLight(1); // yellow warning light
                                positionsTable.SetMessage(string.Format("The depth values in the provided table exceed the local bounds of the heightmap. This will cause some fish to appear to be \"swimming\" above or below the lake."));
                            }
                        };
                    }
                    await rdr.CloseAsync();
                }
            }

            // WARNING: Check for null values
            if (conditionsMet) await this.NullCountCheck(positionsTable, requiredColumns);
        }
        else
        { 
            positionsTable.SetLight(2); 
            positionsTable.SetMessage(string.Format("The \"{0}\" table was not found in the provided database.", tableName)); 
            conditionsMet = false; 
        }

        // Debug.Log(string.Format("Table: {0}; Light: {1}; Message: {2}; Columns: \"{3}\"", positionsTable.tableName, positionsTable.lightColor, positionsTable.tableMessage, string.Join(", ", positionsTable.presentColumns)));
        return conditionsMet;
    }

    private async Task<bool> CheckThermoTable(string tableName)
    {
        // CHECKS
        // PRIMARY: The table is present, the table is populated, the timestamp, depth, and temperature columns exist
        // WARNING: There should be no null values

        bool conditionsMet = true; 

        // Thermocline table
        Table thermoTable = new Table(tableName);
        this.tables[tableName] = thermoTable;
        List<string> requiredColumns = new List<string> {"timestamp", "depth", "temperature"};

        // PRIMARY: The table must be present
        if (this.tableNames.Contains(tableName))
        {
            // PRIMARY: The table must have all of a timestamp, depth, and temperature columns. The oxygen column is optional.
            conditionsMet = await this.RequiredColumnsCheck(thermoTable, requiredColumns);

            // WARNING: Check for null values
            if (conditionsMet)
            {
                if (thermoTable.presentColumns.Contains("oxygen")) await this.NullCountCheck(thermoTable, new List<string> {"timestamp", "depth", "temperature", "oxygen"});
                else await this.NullCountCheck(thermoTable, requiredColumns);
            }
        }
        else
        { 
            thermoTable.SetLight(2); 
            thermoTable.SetMessage(string.Format("The \"{0}\" table was not found in the provided database.", tableName)); 
            conditionsMet = false; 
        }

        // Debug.Log(string.Format("Table: {0}; Light: {1}; Message: {2}; Columns: \"{3}\"", thermoTable.tableName, thermoTable.lightColor, thermoTable.tableMessage, string.Join(", ", thermoTable.presentColumns)));
        return conditionsMet;
    }

    private async Task<bool> CheckSpeciesTable(string tableName)
    {
        // CHECKS
        // PRIMARY: The table is present, the table is populated, the id and name columns exist
        // WARNING: Names beyond the in-game prefab library, there should be no null mappings

        bool conditionsMet = true; 

        // Thermocline table
        Table speciesTable = new Table(tableName);
        this.tables[tableName] = speciesTable;
        List<string> requiredColumns = new List<string> { "id", "name" };

        // PRIMARY: The table must be present
        if (this.tableNames.Contains(tableName))
        {
            // PRIMARY: The table must have all of an id and name columns.
            conditionsMet = await this.RequiredColumnsCheck(speciesTable, requiredColumns);

            // WARNING: Check for species beyond existing prefabs
            if (conditionsMet)
            {
                string sql = string.Format("select count(*) from {0} where LOWER(name) not in ('scaled carp', 'catfish', 'pike', 'tench', 'mirror carp', 'perch', 'roach')", tableName);

                NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
                await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
                {
                    if (!rdr.HasRows) 
                    {
                        speciesTable.SetLight(1); // yellow warning light
                        speciesTable.SetMessage("Was unable to successfully query for species types."); 
                    }
                    else
                    {
                        while (await rdr.ReadAsync())
                        {
                            int unaccountedSpecies = Convert.ToInt32(rdr.GetValue(rdr.GetOrdinal("count")));
                            if (unaccountedSpecies > 0)
                            {
                                speciesTable.SetLight(1); // yellow warning light
                                speciesTable.SetMessage(string.Format("{0} species exist in the table that aren't currently supported by the renderer. Fish species outside of 'scaled carp', 'catfish', 'pike', 'tench', 'mirror carp', 'perch' and 'roach' will be rendered using the roach in-game model.", unaccountedSpecies)); 
                            }
                        };
                    }
                    await rdr.CloseAsync();
                }
            }

            // WARNING: Check for null values
            if (conditionsMet) await this.NullCountCheck(speciesTable, requiredColumns);
        }
        else
        { 
            speciesTable.SetLight(2); 
            speciesTable.SetMessage(string.Format("The \"{0}\" table was not found in the provided database.", tableName)); 
            conditionsMet = false; 
        }

        // Debug.Log(string.Format("Table: {0}; Light: {1}; Message: {2}; Columns: \"{3}\"", speciesTable.tableName, speciesTable.lightColor, speciesTable.tableMessage, string.Join(", ", speciesTable.presentColumns)));
        return conditionsMet;
    }

    private async Task<bool> CheckWeatherTable(string tableName)
    {
        // CHECKS
        // PRIMARY: The table is present, the table is populated, the timestamp, winddirection and windspeed columns exist OR the timestamp and any of the temperature, humidity, airpressure, precipitation columns exist
        // WARNING: There should be no null values

        bool conditionsMet = true; 

        // Weather table
        Table weatherTable = new Table(tableName);
        this.tables[tableName] = weatherTable;

        // PRIMARY: The table must be present
        if (this.tableNames.Contains(tableName))
        {
            // PRIMARY: The table must match one of the preset configurations.
            List<string> requiredColumns = new List<string> { "timestamp" };
            string sql = string.Format("SELECT * FROM information_schema.columns WHERE table_name = '{0}'", tableName);

            // Check by removing the column names from the list if they're found
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
            {
                // PRIMARY: The table must have data
                if (!rdr.HasRows)
                { 
                    weatherTable.SetLight(2); 
                    weatherTable.SetMessage("No records were recovered while querying the table schema. Is this table populated?"); 
                    conditionsMet = false; 
                }
                else
                {
                    while (await rdr.ReadAsync())
                    {
                        string columnName = Convert.ToString(rdr.GetValue(rdr.GetOrdinal("column_name")));
                        weatherTable.presentColumns.Add(columnName);

                        if (requiredColumns.Contains(columnName)) requiredColumns.Remove(columnName); 
                    };

                    // If the list still has names, these are the missing columns
                    if (requiredColumns.Count > 0)
                    {
                        weatherTable.SetLight(2); 
                        weatherTable.SetMessage(string.Format("The \"{0}\" table must have a column named \"timestamp\". It was not found in the provided table.", tableName));
                        conditionsMet = false; 
                    }
                    else if (!(weatherTable.presentColumns.Contains("winddirection") && weatherTable.presentColumns.Contains("windspeed")) && 
                    !(weatherTable.presentColumns.Contains("temperature") || weatherTable.presentColumns.Contains("humidity") || weatherTable.presentColumns.Contains("airpressure") || weatherTable.presentColumns.Contains("precipitation")))
                    {
                        weatherTable.SetLight(2); 
                        weatherTable.SetMessage(string.Format("The \"{1}\" table must have either columns named \"winddirection\" and \"windspeed\" (both must be present), or any column named \"temperature\", \"airpressure\", \"humidity\" or \"precipitation\". Only column(s): \"{0}\", were found in the provided table.", string.Join("\", \"", weatherTable.presentColumns), tableName));
                        conditionsMet = false; 
                    }
                }
                await rdr.CloseAsync();
            }

            // WARNING: Check for null values
            if (conditionsMet)
            {
                // Assemble relevant column names for an accurate sql query string
                List<string> subList = new List<string>();
                List<string> relevantNames = new List<string>() { "winddirection", "windspeed", "temperature", "airpressure", "humidity", "precipitation" };
                foreach (string name in relevantNames) if (weatherTable.presentColumns.Contains(name)) subList.Add(name);

                await this.NullCountCheck(weatherTable, subList);
            }
        }
        else
        { 
            weatherTable.SetLight(2); 
            weatherTable.SetMessage(string.Format("The \"{0}\" table was not found in the provided database.", tableName)); 
            conditionsMet = false; 
        }

        // Debug.Log(string.Format("Table: {0}; Light: {1}; Message: {2}; Columns: \"{3}\"", weatherTable.tableName, weatherTable.lightColor, weatherTable.tableMessage, string.Join(", ", weatherTable.presentColumns)));
        return conditionsMet;
    }

    private async Task<bool> CheckMacroPolyTable(string tableName, int rowCount, int columnCount)
    {
        // CHECKS
        // PRIMARY: The table is present, the table is populated, the timestamp, x, y, poly_id, upper and lower columns exist
            // --> also check polygon vector ordering?
        // WARNING: There should be no null values

        bool conditionsMet = true; 

        // Polygon table
        Table polyTable = new Table(tableName);
        this.tables[tableName] = polyTable;
        List<string> requiredColumns = new List<string> { "timestamp", "x", "y", "poly_id", "upper", "lower" };

        // PRIMARY: The table must be present
        if (this.tableNames.Contains(tableName))
        {
            // PRIMARY: The table must match one of the preset configurations.
            conditionsMet = await this.RequiredColumnsCheck(polyTable, requiredColumns);

            // WARNING: The max/min x, y and z should fall within the boundaries of the height map
            if (conditionsMet) await this.XYBoundsCheck(polyTable, rowCount, columnCount);

            // WARNING: Check for null values
            if (conditionsMet) await this.NullCountCheck(polyTable, requiredColumns);
        }
        else
        { 
            polyTable.SetLight(2); 
            polyTable.SetMessage(string.Format("The \"{0}\" table was not found in the provided database.", tableName)); 
            conditionsMet = false; 
        }

        // Debug.Log(string.Format("Table: {0}; Light: {1}; Message: {2}; Columns: \"{3}\"", polyTable.tableName, polyTable.lightColor, polyTable.tableMessage, string.Join(", ", polyTable.presentColumns)));
        return conditionsMet;
    }

    private async Task<bool> CheckMacroHeightTable(string tableName, int rowCount, int columnCount)
    {
        // CHECKS
        // PRIMARY: The table is present, the table is populated, the timestamp, x, y, poly_id, upper and lower columns exist
            // --> also check polygon vector ordering?
        // WARNING: There should be no null values, x/y values within bounds of height map

        bool conditionsMet = true; 

        // Polygon table
        Table heightTable = new Table(tableName);
        this.tables[tableName] = heightTable;
        List<string> requiredColumns = new List<string> { "timestamp", "x", "y", "height_m" };

        // PRIMARY: The table must be present
        if (this.tableNames.Contains(tableName))
        {
            // PRIMARY: The table must match one of the preset configurations.
            conditionsMet = await this.RequiredColumnsCheck(heightTable, requiredColumns);

            // WARNING: The max/min x and y should fall within the boundaries of the height map
            if (conditionsMet) await this.XYBoundsCheck(heightTable, rowCount, columnCount);

            // WARNING: Check for null values
            if (conditionsMet) await this.NullCountCheck(heightTable, requiredColumns);
        }
        else
        { 
            heightTable.SetLight(2); 
            heightTable.SetMessage(string.Format("The \"{0}\" table was not found in the provided database.", tableName)); 
            conditionsMet = false; 
        }

        // Debug.Log(string.Format("Table: {0}; Light: {1}; Message: {2}; Columns: \"{3}\"", heightTable.tableName, heightTable.lightColor, heightTable.tableMessage, string.Join(", ", heightTable.presentColumns)));
        return conditionsMet;
    }



    // SUPPORT FUNCTIONS

    private async Task<bool> RequiredColumnsCheck(Table table, List<string> requiredColumns)
    {
        bool conditionsMet = true; 
        List<string> workingColumns = new List<string>();
        foreach (string column in requiredColumns) workingColumns.Add(column);
        
        string sql =  string.Format("SELECT * FROM information_schema.columns WHERE table_name = '{0}'", table.tableName);

        // Check by removing the column names from the list if they're found
        NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
        await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
        {
            // PRIMARY: The table must have data
            if (!rdr.HasRows)
            { 
                table.SetLight(2); 
                table.SetMessage("No records were recovered while querying the table schema. Is this table populated?"); 
                conditionsMet = false; 
            }
            else
            {
                while (await rdr.ReadAsync())
                {
                    string columnName = Convert.ToString(rdr.GetValue(rdr.GetOrdinal("column_name")));
                    table.presentColumns.Add(columnName);

                    if (workingColumns.Contains(columnName)) workingColumns.Remove(columnName); 
                };

                // If the list still has names, these are the missing columns
                if (workingColumns.Count > 0)
                {
                    table.SetLight(2); 
                    table.SetMessage(string.Format("The {0} table must have columns named \"{1}\". Column(s): \"{2}\", were not found in the provided table.", table.tableName, string.Join("\", \"", requiredColumns), string.Join("\", \"", workingColumns)));
                    conditionsMet = false; 
                }
            }
            await rdr.CloseAsync();
        }

        return conditionsMet;
    }

    private async Task XYBoundsCheck(Table table, int rowCount, int columnCount)
    {
        string sql = string.Format("SELECT max(x) max_x, min(x) min_x, max(y) max_y, min(y) min_y from {0}", table.tableName);

        NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
        await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
        {
            if (!rdr.HasRows)
            { 
                table.SetLight(1); // yellow warning light
                table.SetMessage("Was unable to successfully query for bounds testing against the height map bounds.");
            }
            else
            {
                while (await rdr.ReadAsync())
                {
                    float maxX = Convert.ToSingle(rdr.GetValue(rdr.GetOrdinal("max_x")));
                    float minX = Convert.ToSingle(rdr.GetValue(rdr.GetOrdinal("min_x")));
                    float maxY = Convert.ToSingle(rdr.GetValue(rdr.GetOrdinal("max_y")));
                    float minY = Convert.ToSingle(rdr.GetValue(rdr.GetOrdinal("min_y")));

                    if (minX < 0 || maxX > columnCount || minY < 0 || maxY > rowCount)
                    {
                        table.SetLight(1); // yellow warning light
                        table.SetMessage(string.Format("The (x, y) values in the provided table exceed the local bounds of the heightmap. This may cause to some fish to appear to be \"swimming\" on land or in empty space."));
                    }
                };
            }
            await rdr.CloseAsync();
        }
    }

    private async Task NullCountCheck(Table table, List<string> columns)
    {
        int nullCount = 0;
        string sql = string.Format("SELECT COUNT(*) FROM {0} WHERE {1} is null", table.tableName, string.Join(" is null or ", columns));

        NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
        await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
        {
            if (!rdr.HasRows) 
            {
                table.SetLight(1); // yellow warning light
                table.SetMessage("Was unable to successfully query for null counts."); 
            }
            else
            {
                while (await rdr.ReadAsync())
                {
                    nullCount = Convert.ToInt32(rdr.GetValue(rdr.GetOrdinal("count")));
                    if (nullCount > 0)
                    {
                        table.SetLight(1); // yellow warning light
                        table.SetMessage(string.Format("{0} records were found to have a null value in at least one of the required columns. Note that these records will be ignored when rendering.", nullCount)); 
                    }
                };
            }
            await rdr.CloseAsync();
        }
    }
}