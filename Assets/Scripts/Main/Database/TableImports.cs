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

    public TableImports(NpgsqlConnection connection) { this.connection = connection; this.tables = new Dictionary<string, Table>(); }

    public async Task<bool> VerifyTables()
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

        if (runningCount == 30) { return false; } // failed to open
        else
        {
            DataTable schema = await connection.GetSchemaAsync("Tables");

            this.tableNames = new List<string>();
            foreach(DataRow row in schema.Rows) { this.tableNames.Add(row["table_name"].ToString()); }

            // Run through checklist of tables
            return await this.Verificator();
        }
    }

    private async Task<bool> Verificator()
    {
        // NECESSARY TABLES ---------------------------------------------------------------------------------------------
        // fish
        this.fishAccepted = await this.CheckFishTable();

        // heightmap
        Tuple<bool, int, int> heightMapPacket = await this.CheckHeightMap();
        this.heightMapAccepted = heightMapPacket.Item1;

        // positions
        // this.positionsAccepted = await this.CheckPositionsTable(heightMapPacket.Item2, heightMapPacket.Item3);

        // bool requiredTablesPresent = (this.fishAccepted && this.heightMapAccepted && this.positionsAccepted) ? true : false;

        // OPTIONAL TABLES ---------------------------------------------------------------------------------------------
        this.speciesAccepted = await this.CheckSpeciesTable();
        this.weatherAccepted = await this.CheckWeatherTable();
        this.thermoclineAccepted = await this.CheckThermoTable();

        // return requiredTablesPresent;
        return true;
    }

    private async Task<bool> CheckFishTable()
    {
        // CHECKS
        // PRIMARY: The table is present, the table is populated, the id column exists, and the id column is unique
        // WARNING: None

        bool conditionsMet = true; 

        // Fish table
        Table fishTable = new Table("fish");
        this.tables["fish"] = fishTable;

        // PRIMARY: The table must be present
        if (this.tableNames.Contains("fish"))
        {
            // PRIMARY: The IDs must be unique
            string sql = "select count(id) base_count, count(distinct id) unique_count from fish where id is not null";

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
                    fishTable.SetMessage("The connected database's \"fish\" table does not have a column named \"id\". This column must both exist and be unique.");
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
            fishTable.SetMessage("The \"fish\" table was not found in the provided database.");
            conditionsMet = false;
        }

        // INTERNAL: Determine which of the optional columns are present. This is necessary information for when building out the Fish.cs class.
        // Optional columns are: species, length, weight, sex, capture_type
        if (conditionsMet)
        {
            // Get the table's schema for the column-based query
            DataTable schema = connection.GetSchema("Tables");
            string fishSchema = "";
            foreach(DataRow row in schema.Rows) if (row["table_name"].ToString() == "fish") { fishSchema = row["table_schema"].ToString(); }
            
            string sql =  string.Format(@"SELECT *
                FROM information_schema.columns
                WHERE table_schema = '{0}'
                AND table_name = 'fish'", fishSchema)
            ;

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

        Debug.Log(string.Format("Table: {0}; Light: {1}; Message: {2}; Columns: \"{3}\"", fishTable.tableName, fishTable.lightColor, fishTable.tableMessage, string.Join(", ", fishTable.presentColumns)));
        return conditionsMet;
    }

    // TODO: check for null values
    private async Task<Tuple<bool, int, int>> CheckHeightMap()
    {
        // CHECKS
        // PRIMARY: The table is present, the table is populated
        // WARNING: There are no null values

        bool conditionsMet = true; 

        // Heightmap table
        int rowCount = 0;
        int columnCount = 0;
        Table heightMapTable = new Table("meshmap");
        this.tables["meshmap"] = heightMapTable;

        // PRIMARY: The table must be present
        if (this.tableNames.Contains("meshmap"))
        {
            // We only need the row count and column count to contrast against the positions table
            string sql =  string.Format(@"select
                (select count(*) from meshmap) as rows,
                (SELECT COUNT(*) FROM INFORMATION_SCHEMA.COLUMNS WHERE table_catalog = '{0}' AND table_name = 'meshmap') as columns 
                ", connection.Database);

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
            heightMapTable.SetMessage("The \"meshmap\" table was not found in the provided database.");
            conditionsMet = false;
        }

        // WARNING: There should be no null values
        if (conditionsMet)
        {
            // Collect all information to procedurally generate the actual SQL statement, such that specific column names aren't necessary
            List<string> columnMapping = new List<string>();
            string sql =  @"SELECT column_name 
            FROM information_schema.columns
            WHERE table_name = 'meshmap'";

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
                FROM meshmap) a", string.Join("), (", columnMapping))
            ; 

            float minVal = float.MaxValue;
            float maxVal = float.MinValue;

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
                            // PHRASING FOR ONLY ONE CELL (SINGULAR)
                            heightMapTable.SetMessage(string.Format("{0} cells were found to have null values. Should you decide to continue without first correcting these, the depth values of those cells will be set to the maximum float value (and accordingly become very visible when rendering).", nullCount)); 
                        }
                    };
                }
                await rdr.CloseAsync();
            }
        }

        // CARRY FORWARD MAX/MIN INFO
        Debug.Log(string.Format("Table: {0}; Light: {1}; Message: {2}", heightMapTable.tableName, heightMapTable.lightColor, heightMapTable.tableMessage));
        return new Tuple<bool, int, int> (conditionsMet, rowCount, columnCount);
    }

    private async Task<bool> CheckPositionsTable(int rowCount, int columnCount)
    {
        // CHECKS
        // PRIMARY: The table is present, the table is populated, the id, timestamp, x, y and z columns exist
        // WARNING: The positions should fall within the bounds of the height map, there should be no null values

        bool conditionsMet = true; 

        // Positions table
        Table positionsTable = new Table("positions");
        this.tables["positions"] = positionsTable;

        // PRIMARY: The table must be present
        if (this.tableNames.Contains("positions_local"))
        {
            // PRIMARY: The table must have all of an id, timestamp, x, y and z columns
            List<string> requiredColumns = new List<string> {"id", "timestamp", "x", "y", "z"};
            
            string sql =  @"SELECT *
                FROM information_schema.columns
                WHERE table_name = 'positions_local'"
            ;

            // Check by removing the column names from the list if they're found
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
            {
                // PRIMARY: The table must have data
                if (!rdr.HasRows)
                { 
                    positionsTable.SetLight(2); 
                    positionsTable.SetMessage("No records were recovered while querying the table schema. Is this table populated?"); 
                    conditionsMet = false; 
                }
                else
                {
                    while (await rdr.ReadAsync())
                    {
                        string columnName = Convert.ToString(rdr.GetValue(rdr.GetOrdinal("column_name")));
                        positionsTable.presentColumns.Add(columnName);

                        if (requiredColumns.Contains(columnName)) requiredColumns.Remove(columnName); 
                    };

                    // If the list still has names, these are the missing columns
                    if (requiredColumns.Count > 0)
                    {
                        positionsTable.SetLight(2); 
                        positionsTable.SetMessage(string.Format("The positions table must have columns named \"id\", \"timestamp\", \"x\", \"y\" and \"z\". Column(s): \"{0}\", were not found in the provided table.", string.Join("\", \"", requiredColumns)));
                        conditionsMet = false; 
                    }
                }
                await rdr.CloseAsync();
            }

            // WARNING: The max/min x and y should fall within the boundaries of the height map
            // All columns need to be explicitly listed in the SQL query to test for the z-value (depth), so skip this
            if (conditionsMet)
            {
                sql = "SELECT max(x) max_x, min(x) min_x, max(y) max_y, min(y) min_y from positions_local";

                cmd = new NpgsqlCommand(sql, connection);
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

                            if (minX < 0 || maxX > columnCount || minY < 0 || maxY > rowCount)
                            {
                                positionsTable.SetLight(1); // yellow warning light
                                positionsTable.SetMessage(string.Format("The positions values in the provided table exceed the local bounds of the heightmap. This may lead to fish \"swimming\" on land or in empty space."));
                            }
                        };
                    }
                    await rdr.CloseAsync();
                }
            }

            // WARNING: Check for null values
            if (conditionsMet)
            {
                sql = "SELECT COUNT(*) FROM positions_local WHERE id is null or timestamp is null or x is null or y is null or z is null";

                cmd = new NpgsqlCommand(sql, connection);
                await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
                {
                    if (!rdr.HasRows) 
                    {
                        positionsTable.SetLight(1); // yellow warning light
                        positionsTable.SetMessage("Was unable to successfully query for null counts."); 
                    }
                    else
                    {
                        while (await rdr.ReadAsync())
                        {
                            int nullCount = Convert.ToInt32(rdr.GetValue(rdr.GetOrdinal("count")));
                            if (nullCount > 0)
                            {
                                positionsTable.SetLight(1); // yellow warning light
                                positionsTable.SetMessage(string.Format("{0} records were found to have a null value in at least one of the required columns. Note that these records will be ignored when rendering.", nullCount)); 
                            }
                        };
                    }
                    await rdr.CloseAsync();
                }
            }
        }
        else
        { 
            positionsTable.SetLight(2); 
            positionsTable.SetMessage("The \"positions\" table was not found in the provided database."); 
            conditionsMet = false; 
        }

        Debug.Log(string.Format("Table: {0}; Light: {1}; Message: {2}; Columns: \"{3}\"", positionsTable.tableName, positionsTable.lightColor, positionsTable.tableMessage, string.Join(", ", positionsTable.presentColumns)));
        return conditionsMet;
    }

    private async Task<bool> CheckThermoTable()
    {
        // CHECKS
        // PRIMARY: The table is present, the table is populated, the timestamp, depth, and temperature columns exist
        // WARNING: There should be no null values

        bool conditionsMet = true; 

        // Thermocline table
        Table thermoTable = new Table("thermocline");
        this.tables["thermocline"] = thermoTable;

        // PRIMARY: The table must be present
        if (this.tableNames.Contains("thermocline"))
        {
            // PRIMARY: The table must have all of a timestamp, depth, and temperature columns. The oxygen column is optional.
            List<string> requiredColumns = new List<string> {"timestamp", "depth", "temperature"};
            
            string sql = @"SELECT *
                FROM information_schema.columns
                WHERE table_name = 'thermocline'"
            ;

            // Check by removing the column names from the list if they're found
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
            {
                // PRIMARY: The table must have data
                if (!rdr.HasRows)
                { 
                    thermoTable.SetLight(2); 
                    thermoTable.SetMessage("No records were recovered while querying the table schema. Is this table populated?"); 
                    conditionsMet = false; 
                }
                else
                {
                    while (await rdr.ReadAsync())
                    {
                        string columnName = Convert.ToString(rdr.GetValue(rdr.GetOrdinal("column_name")));
                        thermoTable.presentColumns.Add(columnName);

                        if (requiredColumns.Contains(columnName)) requiredColumns.Remove(columnName); 
                    };

                    // If the list still has names, these are the missing columns
                    if (requiredColumns.Count > 0)
                    {
                        thermoTable.SetLight(2); 
                        thermoTable.SetMessage(string.Format("The thermocline table must have columns named \"timestamp\", \"depth\", and \"temperature\". Column(s): \"{0}\", were not found in the provided table.", string.Join("\", \"", requiredColumns)));
                        conditionsMet = false; 
                    }
                }
                await rdr.CloseAsync();
            }

            // WARNING: Check for null values
            if (conditionsMet)
            {
                sql = "";
                if (thermoTable.presentColumns.Contains("oxygen")) sql = "SELECT COUNT(*) FROM thermocline WHERE timestamp is null or temperature is null or depth is null or oxygen is null";
                else sql = "SELECT COUNT(*) FROM thermocline WHERE timestamp is null or temperature is null or depth is null";

                cmd = new NpgsqlCommand(sql, connection);
                await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
                {
                    if (!rdr.HasRows) 
                    {
                        thermoTable.SetLight(1); // yellow warning light
                        thermoTable.SetMessage("Was unable to successfully query for null counts."); 
                    }
                    else
                    {
                        while (await rdr.ReadAsync())
                        {
                            int nullCount = Convert.ToInt32(rdr.GetValue(rdr.GetOrdinal("count")));
                            if (nullCount > 0)
                            {
                                thermoTable.SetLight(1); // yellow warning light
                                thermoTable.SetMessage(string.Format("{0} records were found to have a null value in at least one of the present columns. Note that these records will be ignored when rendering.", nullCount)); 
                            }
                        };
                    }
                    await rdr.CloseAsync();
                }
            }
        }
        else
        { 
            thermoTable.SetLight(2); 
            thermoTable.SetMessage("The \"thermocline\" table was not found in the provided database."); 
            conditionsMet = false; 
        }

        Debug.Log(string.Format("Table: {0}; Light: {1}; Message: {2}; Columns: \"{3}\"", thermoTable.tableName, thermoTable.lightColor, thermoTable.tableMessage, string.Join(", ", thermoTable.presentColumns)));
        return conditionsMet;
    }

    private async Task<bool> CheckSpeciesTable()
    {
        // CHECKS
        // PRIMARY: The table is present, the table is populated, the id and name columns exist
        // WARNING: Names beyond the in-game prefab library, there should be no null mappings

        bool conditionsMet = true; 

        // Thermocline table
        Table speciesTable = new Table("species");
        this.tables["species"] = speciesTable;

        // PRIMARY: The table must be present
        if (this.tableNames.Contains("species"))
        {
            // PRIMARY: The table must have all of an id and name columns.
            List<string> requiredColumns = new List<string> { "id", "name" };

            // Get the table's schema for the column-based query
            DataTable schema = connection.GetSchema("Tables");
            string speciesSchema = "";
            foreach(DataRow row in schema.Rows) if (row["table_name"].ToString() == "species") { speciesSchema = row["table_schema"].ToString(); }
            
            string sql =  @"SELECT *
                FROM information_schema.columns
                WHERE table_name = 'species'"
            ;

            // Check by removing the column names from the list if they're found
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
            {
                // PRIMARY: The table must have data
                if (!rdr.HasRows)
                { 
                    speciesTable.SetLight(2); 
                    speciesTable.SetMessage("No records were recovered while querying the table schema. Is this table populated?"); 
                    conditionsMet = false; 
                }
                else
                {
                    while (await rdr.ReadAsync())
                    {
                        string columnName = Convert.ToString(rdr.GetValue(rdr.GetOrdinal("column_name")));
                        speciesTable.presentColumns.Add(columnName);

                        if (requiredColumns.Contains(columnName)) requiredColumns.Remove(columnName); 
                    };

                    // If the list still has names, these are the missing columns
                    if (requiredColumns.Count > 0)
                    {
                        speciesTable.SetLight(2); 
                        speciesTable.SetMessage(string.Format("The species table must have columns named \"id\", and \"name\". Column(s): \"{0}\", were not found in the provided table.", string.Join("\", \"", requiredColumns)));
                        conditionsMet = false; 
                    }
                }
                await rdr.CloseAsync();
            }

            // WARNING: Check for species beyond existing prefabs
            if (conditionsMet)
            {
                sql = "select count(*) from species where LOWER(name) not in ('scaled carp', 'catfish', 'pike', 'tench', 'mirror carp', 'perch', 'roach')";

                cmd = new NpgsqlCommand(sql, connection);
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
            if (conditionsMet)
            {
                sql = "SELECT COUNT(*) FROM species WHERE id is null or name is null";

                cmd = new NpgsqlCommand(sql, connection);
                await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
                {
                    if (!rdr.HasRows) 
                    {
                        speciesTable.SetLight(1); // yellow warning light
                        speciesTable.SetMessage("Was unable to successfully query for null counts."); 
                    }
                    else
                    {
                        while (await rdr.ReadAsync())
                        {
                            int nullCount = Convert.ToInt32(rdr.GetValue(rdr.GetOrdinal("count")));
                            if (nullCount > 0)
                            {
                                speciesTable.SetLight(1); // yellow warning light
                                speciesTable.SetMessage(string.Format("{0} records were found to have a null value in at least one of the present columns. Note that these records will be ignored when rendering.", nullCount)); 
                            }
                        };
                    }
                    await rdr.CloseAsync();
                }
            }
        }
        else
        { 
            speciesTable.SetLight(2); 
            speciesTable.SetMessage("The \"species\" table was not found in the provided database."); 
            conditionsMet = false; 
        }

        Debug.Log(string.Format("Table: {0}; Light: {1}; Message: {2}; Columns: \"{3}\"", speciesTable.tableName, speciesTable.lightColor, speciesTable.tableMessage, string.Join(", ", speciesTable.presentColumns)));
        return conditionsMet;
    }

    private async Task<bool> CheckWeatherTable()
    {
        // CHECKS
        // PRIMARY: The table is present, the table is populated, the timestamp, winddirection and windspeed columns exist OR the timestamp and any of the temperature, humidity, airpressure, precipitation columns exist
        // WARNING: There should be no null values

        bool conditionsMet = true; 

        // Weather table
        Table weatherTable = new Table("weather");
        this.tables["weather"] = weatherTable;

        // PRIMARY: The table must be present
        if (this.tableNames.Contains("weatherstation"))
        {
            // PRIMARY: The table must match one of the preset configurations.
            List<string> requiredColumns = new List<string> { "timestamp" };
            
            string sql = @"SELECT *
                FROM information_schema.columns
                WHERE table_name = 'weatherstation'"
            ;

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
                        weatherTable.SetMessage("The weather table must have a column named \"timestamp\". It was not found in the provided table.");
                        conditionsMet = false; 
                    }
                    else if (!(weatherTable.presentColumns.Contains("winddirection") && weatherTable.presentColumns.Contains("windspeed")) && 
                    !(weatherTable.presentColumns.Contains("temperature") || weatherTable.presentColumns.Contains("humidity") || weatherTable.presentColumns.Contains("airpressure") || weatherTable.presentColumns.Contains("precipitation")))
                    {
                        weatherTable.SetLight(2); 
                        weatherTable.SetMessage(string.Format("The weather table must have either columns named \"winddirection\" and \"windspeed\" (both must be present), or any column named \"temperature\", \"airpressure\", \"humidity\" or \"precipitation\". Only column(s): \"{0}\", were found in the provided table.", string.Join("\", \"", weatherTable.presentColumns)));
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

                sql = string.Format("SELECT COUNT(*) FROM weatherstation WHERE {0} is null", string.Join(" is null or ", subList));

                cmd = new NpgsqlCommand(sql, connection);
                await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
                {
                    if (!rdr.HasRows) 
                    {
                        weatherTable.SetLight(1); // yellow warning light
                        weatherTable.SetMessage("Was unable to successfully query for null counts."); 
                    }
                    else
                    {
                        while (await rdr.ReadAsync())
                        {
                            int nullCount = Convert.ToInt32(rdr.GetValue(rdr.GetOrdinal("count")));
                            if (nullCount > 0)
                            {
                                weatherTable.SetLight(1); // yellow warning light
                                weatherTable.SetMessage(string.Format("{0} records were found to have a null value in at least one of the present columns. Null wind values will be ignored, whereas nulls in other fields will be reported as such in-game.", nullCount)); 
                            }
                        };
                    }
                    await rdr.CloseAsync();
                }
            }
        }
        else
        { 
            weatherTable.SetLight(2); 
            weatherTable.SetMessage("The \"weather\" table was not found in the provided database."); 
            conditionsMet = false; 
        }

        Debug.Log(string.Format("Table: {0}; Light: {1}; Message: {2}; Columns: \"{3}\"", weatherTable.tableName, weatherTable.lightColor, weatherTable.tableMessage, string.Join(", ", weatherTable.presentColumns)));
        return conditionsMet;
    }
}