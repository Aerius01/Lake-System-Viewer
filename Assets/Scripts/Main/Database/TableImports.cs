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
    private List<Table> tables;

    // die notwendig sind
    public bool fishExists { get; private set; }
    public bool positionsExists { get; private set; }
    public bool heightMapExists { get; private set; }

    // die optionale sind
    public bool speciesExists { get; private set; }
    public bool weatherExists { get; private set; }
    public bool thermoclineExists { get; private set; }
    public bool macroPolysExists { get; private set; }
    public bool macroHeightsExists { get; private set; }

    public TableImports(NpgsqlConnection connection) { this.connection = connection; this.tables = new List<Table>(); }

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
        bool requiredTablesPresent = true;


        // NECESSARY TABLES ---------------------------------------------------------------------------------------------
        // Fish table
        Table fishTable = new Table("fish");
        this.tables.Add(fishTable);

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
                        fishTable.SetStatus(false);
                        fishTable.SetMessage("No records were recovered. Is this table populated?");
                        requiredTablesPresent = false;
                    }
                    else
                    {
                        while (await rdr.ReadAsync())
                        {
                            int baseCount = rdr.GetInt32(rdr.GetOrdinal("base_count"));
                            int uniqueCount = rdr.GetInt32(rdr.GetOrdinal("unique_count"));

                            if (baseCount != uniqueCount)
                            {
                                fishTable.SetStatus(false);
                                fishTable.SetMessage("The ID column has duplicate values. Please ensure that the ID column is unique, and then try again.");
                                requiredTablesPresent = false;
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
                    fishTable.SetStatus(false);
                    fishTable.SetMessage("The connected database's \"fish\" table does not have a column named \"id\". This column must both exist and be unique.");
                    requiredTablesPresent = false;
                }
                else throw e;
            }
        }
        else { requiredTablesPresent = false; fishTable.SetStatus(false); }

        // Heightmap table
        int rowCount = 0;
        int columnCount = 0;
        Table heightMapTable = new Table("meshmap");
        this.tables.Add(heightMapTable);

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
                if (!rdr.HasRows) { requiredTablesPresent = false; heightMapTable.SetStatus(false); heightMapTable.SetMessage("No records were recovered. Is this table populated?"); }
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
        else { heightMapTable.SetStatus(false); requiredTablesPresent = false; }

        // Positions table
        Table positionsTable = new Table("positions");
        this.tables.Add(positionsTable);

        // PRIMARY: The table must be present
        if (this.tableNames.Contains("positions_local"))
        {
            // PRIMARY: The table must have all of an id, timestamp, x, y and z columns
            List<string> requiredColumns = new List<string> {"id", "timestamp", "x", "y", "z"};

            // Get the table's schema for the column-based query
            DataTable schema = connection.GetSchema("Tables");
            string positionsSchema = "";
            foreach(DataRow row in schema.Rows) if (row["table_name"].ToString() == "positions_local") { positionsSchema = row["table_schema"].ToString(); }
            
            string sql =  string.Format(@"SELECT *
                FROM information_schema.columns
                WHERE table_schema = '{0}'
                AND table_name = 'positions_local'", positionsSchema)
            ;

            // Check by removing the column names from the list if they're found
            NpgsqlCommand cmd = new NpgsqlCommand(sql, connection);
            await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
            {
                // PRIMARY: The table must have data
                if (!rdr.HasRows) { requiredTablesPresent = false; positionsTable.SetStatus(false); positionsTable.SetMessage("No records were recovered. Is this table populated?"); }
                else
                {
                    while (await rdr.ReadAsync())
                    {
                        string columnName = Convert.ToString(rdr.GetValue(rdr.GetOrdinal("column_name")));
                        if (requiredColumns.Contains(columnName)) requiredColumns.Remove(columnName);
                    };
                }
                await rdr.CloseAsync();
            }

            // If the list still has names, these are the missing columns
            if (requiredColumns.Count > 0)
            {
                requiredTablesPresent = false; 
                positionsTable.SetStatus(false); 
                positionsTable.SetMessage(string.Format("The positions table must have columns named \"id\", \"timestamp\", \"x\", \"y\" and \"z\". Column(s): \"{0}\", were not found in the provided table.", string.Join("\", \"", requiredColumns)));
            }

            // WARNING: The max/min x and y should fall within the boundaries of the height map
            // All columns need to be explicitly listed in the SQL query to test for the z-value (depth), so skip this
            if (requiredTablesPresent)
            {
                sql = "SELECT max(x) max_x, min(x) min_x, max(y) max_y, min(y) min_y from positions_local";

                cmd = new NpgsqlCommand(sql, connection);
                await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
                {
                    if (!rdr.HasRows) { positionsTable.SetMessage("Was unable to successfully query for bounds testing against the height map bounds."); }
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
            if (requiredTablesPresent)
            {
                sql = "SELECT COUNT(*) FROM positions_local WHERE id is null or timestamp is null or x is null or y is null or z is null";

                cmd = new NpgsqlCommand(sql, connection);
                await using (NpgsqlDataReader rdr = await cmd.ExecuteReaderAsync())
                {
                    if (!rdr.HasRows) positionsTable.SetMessage("Was unable to successfully query for null counts."); 
                    else
                    {
                        while (await rdr.ReadAsync())
                        {
                            int nullCount = Convert.ToInt32(rdr.GetValue(rdr.GetOrdinal("count")));
                            if (nullCount > 0)
                            {
                                positionsTable.SetLight(1); // yellow warning light
                                string message = string.Format(@"{0} records were found to have a null value in at least one of the required columns. Note that these records will be ignored when rendering.", nullCount);
                                positionsTable.SetMessage(message); 
                            }
                        };
                    }
                    await rdr.CloseAsync();
                }
            }
        }
        else { positionsTable.SetStatus(false); requiredTablesPresent = false; }


        // OPTIONAL TABLES ---------------------------------------------------------------------------------------------

        // now again with other tables
        Debug.Log(string.Format("{0}; {1}: {2}", requiredTablesPresent, this.tables[2].lightColor, this.tables[2].tableMessage));

        return requiredTablesPresent;
    }
}