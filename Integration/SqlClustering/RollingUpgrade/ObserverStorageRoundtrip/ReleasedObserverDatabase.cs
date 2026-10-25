// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.ObserverStorageRoundtrip;

/// <summary>
/// Discovers an old kernel's real namespace row and witnesses its subsequent updates.
/// </summary>
public static class ReleasedObserverDatabase
{
    /// <summary>
    /// Finds a namespace row belonging to an old kernel-owned observer with specific event types.
    /// </summary>
    /// <param name="connectionString">The host-side PostgreSQL connection.</param>
    /// <returns>The namespace connection, identifier, and owning event sequence, or null until ready.</returns>
    public static async Task<(string ConnectionString, string Identifier, string Owner)?> FindObserver(string connectionString)
    {
        var connections = await Connections(connectionString);
        var definitions = new Dictionary<string, string>();
        foreach (var connection in connections)
        {
            await using var database = new NpgsqlConnection(connection);
            await database.OpenAsync();
            foreach (var schema in await Schemas(database, "ObserverDefinitions"))
            {
                await using var command = new NpgsqlCommand($"SELECT \"Id\", \"EventSequenceId\" FROM {Quote(schema)}.\"ObserverDefinitions\" WHERE \"Owner\" = 2 AND \"EventTypes\"::text NOT IN ('[]', '{{}}', 'null')", database);
                await using var rows = await command.ExecuteReaderAsync();
                while (await rows.ReadAsync()) definitions[rows.GetString(0)] = rows.GetString(1);
            }
        }
        foreach (var connection in connections)
        {
            await using var database = new NpgsqlConnection(connection);
            await database.OpenAsync();
            foreach (var schema in await Schemas(database, "Observers"))
            {
                await using var command = new NpgsqlCommand($"SELECT \"Id\" FROM {Quote(schema)}.\"Observers\"", database);
                await using var rows = await command.ExecuteReaderAsync();
                while (await rows.ReadAsync())
                {
                    var identifier = rows.GetString(0);
                    if (definitions.TryGetValue(identifier, out var owner))
                    {
                        return (new NpgsqlConnectionStringBuilder(connection) { SearchPath = schema }.ConnectionString, identifier, owner);
                    }
                }
            }
        }
        return null;
    }

    /// <summary>
    /// Installs test-only instrumentation after the new provider's write and before the old writer restarts.
    /// </summary>
    /// <param name="connectionString">The namespace connection.</param>
    /// <returns>The asynchronous operation.</returns>
    public static async Task WitnessUpdates(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("""
            CREATE TABLE "__ClaimsRoundtripWrites" ("Id" text NOT NULL);
            CREATE FUNCTION "__ClaimsRoundtripWitness"() RETURNS trigger LANGUAGE plpgsql AS $$
            BEGIN
                INSERT INTO "__ClaimsRoundtripWrites" ("Id") VALUES (NEW."Id");
                PERFORM pg_notify('claims_roundtrip_write', NEW."Id");
                RETURN NEW;
            END;
            $$;
            CREATE TRIGGER "__ClaimsRoundtripWitness" AFTER UPDATE ON "Observers"
                FOR EACH ROW EXECUTE FUNCTION "__ClaimsRoundtripWitness"();
            """, connection);
        await command.ExecuteNonQueryAsync();
    }

    /// <summary>
    /// Awaits an actual old-process update to the selected row, rather than accepting startup alone.
    /// </summary>
    /// <param name="connectionString">The namespace connection.</param>
    /// <param name="identifier">The selected observer.</param>
    /// <returns>Whether the old writer updated that row before the deadline.</returns>
    public static async Task<bool> WasUpdated(string connectionString, string identifier)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        var updated = false;
        connection.Notification += (_, notification) => updated |= notification.Payload == identifier;
        await using (var listen = new NpgsqlCommand("LISTEN claims_roundtrip_write", connection))
        {
            await listen.ExecuteNonQueryAsync();
        }
        await using (var read = new NpgsqlCommand("SELECT EXISTS (SELECT 1 FROM \"__ClaimsRoundtripWrites\" WHERE \"Id\" = @id)", connection))
        {
            read.Parameters.AddWithValue("id", identifier);
            if (await read.ExecuteScalarAsync() is true) return true;
        }
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(45));
        try
        {
            while (!updated) await connection.WaitAsync(deadline.Token);
            return true;
        }
        catch (OperationCanceledException) when (deadline.IsCancellationRequested)
        {
            return false;
        }
    }

    static async Task<string[]> Connections(string connectionString)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
        await using var command = new NpgsqlCommand("SELECT datname FROM pg_database WHERE NOT datistemplate", connection);
        await using var rows = await command.ExecuteReaderAsync();
        var connections = new List<string>();
        while (await rows.ReadAsync()) connections.Add(new NpgsqlConnectionStringBuilder(connectionString) { Database = rows.GetString(0) }.ConnectionString);
        return connections.ToArray();
    }

    static async Task<string[]> Schemas(NpgsqlConnection connection, string table)
    {
        await using var command = new NpgsqlCommand("SELECT table_schema FROM information_schema.tables WHERE table_name = @table", connection);
        command.Parameters.AddWithValue("table", table);
        await using var rows = await command.ExecuteReaderAsync();
        var schemas = new List<string>();
        while (await rows.ReadAsync()) schemas.Add(rows.GetString(0));
        return schemas.ToArray();
    }

    static string Quote(string identifier) => $"\"{identifier.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
}
