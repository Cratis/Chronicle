// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Cratis.Chronicle.Storage.Sql;

#pragma warning disable CA2100 // Database names are delimited with QUOTENAME on the server; every other value is bound.
/// <summary>
/// Enables read-committed snapshot on a SQL Server database without waiting for sessions that hold it open.
/// </summary>
internal static class SqlServerReadCommittedSnapshot
{
    /// <summary>
    /// The Azure SQL Database engine edition, where read-committed snapshot is on by default.
    /// </summary>
    internal const int AzureSqlDatabaseEdition = 5;

    static readonly TimeSpan _defaultDiagnosticsAfter = TimeSpan.FromSeconds(2);

    /// <summary>
    /// Enables read-committed snapshot on a database unless it is already on or the engine turns it on by default.
    /// </summary>
    /// <param name="connectionString">The target connection string; the work runs through master.</param>
    /// <param name="prepareConnection">Prepares a connection with its resolved identity.</param>
    /// <param name="logger">The logger a blocked attempt is reported to.</param>
    /// <param name="diagnosticsAfter">How long to wait before reporting what blocks the change; defaults to two seconds.</param>
    /// <param name="rollbackAfterSeconds">The grace, in seconds, before remaining sessions are rolled back.</param>
    /// <returns>Awaitable task.</returns>
    /// <exception cref="ReadCommittedSnapshotNotEnabled">The change failed; the exception names what held the database.</exception>
    internal static async Task Enable(
        string connectionString,
        Func<DbConnection, Task>? prepareConnection,
        ILogger logger,
        TimeSpan? diagnosticsAfter = null,
        int rollbackAfterSeconds = 5)
    {
        var builder = new SqlConnectionStringBuilder(connectionString);
        var database = builder.InitialCatalog;
        await using var connection = await OpenMaster(connectionString, prepareConnection);

        int spid;
        await using (var state = connection.CreateCommand())
        {
            state.CommandText =
                "SELECT CONVERT(int, SERVERPROPERTY('EngineEdition')), " +
                "COALESCE((SELECT is_read_committed_snapshot_on FROM sys.databases WHERE name = @database), 0), @@SPID";
            state.Parameters.AddWithValue("database", database);
            await using var reader = await state.ExecuteReaderAsync();
            await reader.ReadAsync();
            if (!ShouldEnable(reader.GetInt32(0), Convert.ToBoolean(reader.GetValue(1))))
            {
                return;
            }

            spid = Convert.ToInt32(reader.GetValue(2));
        }

        await using var alter = connection.CreateCommand();
        alter.CommandTimeout = 60;
        alter.CommandText =
            "DECLARE @sql nvarchar(max) = N'ALTER DATABASE ' + QUOTENAME(@database) " +
            "+ N' SET READ_COMMITTED_SNAPSHOT ON WITH ROLLBACK AFTER ' + CONVERT(nvarchar(10), @seconds) + N' SECONDS'; " +
            "EXEC sp_executesql @sql;";
        alter.Parameters.AddWithValue("database", database);
        alter.Parameters.AddWithValue("seconds", Math.Max(0, rollbackAfterSeconds));

        ReadCommittedSnapshotDiagnostics? snapshot = null;
        var altering = alter.ExecuteNonQueryAsync();
        try
        {
            var delay = Task.Delay(diagnosticsAfter ?? _defaultDiagnosticsAfter);
            if (await Task.WhenAny(altering, delay) == delay)
            {
                snapshot = await Snapshot(connectionString, prepareConnection, database, spid);
                logger.Blocked(database, snapshot.Describe());
            }

            await altering;
        }
        catch (SqlException ex)
        {
            snapshot ??= await Snapshot(connectionString, prepareConnection, database, spid);
            throw new ReadCommittedSnapshotNotEnabled(database, snapshot, ex);
        }
    }

    /// <summary>
    /// Warns when an existing database has read-committed snapshot disabled; never changes its access.
    /// </summary>
    /// <param name="connectionString">The target connection string.</param>
    /// <param name="prepareConnection">Prepares a connection with its resolved identity.</param>
    /// <param name="logger">The logger.</param>
    /// <returns>Awaitable task.</returns>
    /// <remarks>
    /// Even an apparently empty database may contain metadata hidden from this identity or acquire tables concurrently.
    /// A failed earlier start therefore requires an operator to enable the setting in a maintenance window.
    /// </remarks>
    internal static async Task WarnIfOff(
        string connectionString,
        Func<DbConnection, Task>? prepareConnection,
        ILogger logger)
    {
        var database = new SqlConnectionStringBuilder(connectionString).InitialCatalog;
        bool off;
        await using (var connection = await OpenMaster(connectionString, prepareConnection))
        {
            await using var command = connection.CreateCommand();
            command.CommandText =
                "SELECT CONVERT(int, SERVERPROPERTY('EngineEdition')), " +
                "COALESCE((SELECT is_read_committed_snapshot_on FROM sys.databases WHERE name = @database), 1);";
            command.Parameters.AddWithValue("database", database);
            await using var reader = await command.ExecuteReaderAsync();
            await reader.ReadAsync();
            off = reader.GetInt32(0) != AzureSqlDatabaseEdition && !Convert.ToBoolean(reader.GetValue(1));
        }

        if (off)
        {
            logger.ReadCommittedSnapshotOff(database);
        }
    }

    /// <summary>
    /// Determines whether a newly created database needs an explicit setting change.
    /// </summary>
    /// <param name="engineEdition">The SQL Server engine edition.</param>
    /// <param name="alreadyOn">Whether the setting is already enabled.</param>
    /// <returns>Whether to enable the setting.</returns>
    internal static bool ShouldEnable(int engineEdition, bool alreadyOn) => engineEdition != AzureSqlDatabaseEdition && !alreadyOn;

    /// <summary>
    /// Bounds all diagnostic work, including identity callbacks that do not support cancellation.
    /// Late results and failures are observed, but never delay provisioning beyond the deadline.
    /// </summary>
    /// <param name="capture">The diagnostic operation.</param>
    /// <param name="timeout">The deadline for the entire operation.</param>
    /// <param name="cancellationToken">Caller cancellation, which is propagated rather than reported as a timeout.</param>
    /// <returns>The snapshot or a safe unavailable diagnostic.</returns>
    internal static async Task<ReadCommittedSnapshotDiagnostics> SnapshotWithin(
        Func<CancellationToken, Task<ReadCommittedSnapshotDiagnostics>> capture,
        TimeSpan timeout,
        CancellationToken cancellationToken = default)
    {
        using var deadline = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        deadline.CancelAfter(timeout);
        var token = deadline.Token;

        // Run even synchronous identity preparation off the provisioning continuation. CaptureFailure observes late faults
        // and always completes successfully, including after this method times out; no faulted background task escapes.
        var capturing = Task.Run(() => CaptureFailure(capture, token), CancellationToken.None);
        try
        {
            var diagnostics = await capturing.WaitAsync(token);
            cancellationToken.ThrowIfCancellationRequested();
            return diagnostics;
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            return Unavailable("timeout");
        }
    }

    static async Task<SqlConnection> OpenMaster(string connectionString, Func<DbConnection, Task>? prepareConnection, CancellationToken cancellationToken = default)
    {
        var builder = new SqlConnectionStringBuilder(connectionString) { Pooling = false, InitialCatalog = "master" };
        var connection = new SqlConnection(builder.ConnectionString);
        try
        {
            if (prepareConnection is not null)
            {
                await prepareConnection(connection);
            }

            cancellationToken.ThrowIfCancellationRequested();
            await connection.OpenAsync(cancellationToken);
            return connection;
        }
        catch
        {
            await connection.DisposeAsync();
            throw;
        }
    }

    static async Task<ReadCommittedSnapshotDiagnostics> CaptureFailure(
        Func<CancellationToken, Task<ReadCommittedSnapshotDiagnostics>> capture,
        CancellationToken cancellationToken)
    {
        try
        {
            return await capture(cancellationToken);
        }
        catch (Exception ex)
        {
            return Unavailable(ex is SqlException sql ? $"SQL error {sql.Number}" : "diagnostic failure");
        }
    }

    static ReadCommittedSnapshotDiagnostics Unavailable(string reason) => new(null, null, null, null, null, [], reason);

    static Task<ReadCommittedSnapshotDiagnostics> Snapshot(string connectionString, Func<DbConnection, Task>? prepareConnection, string database, int waiter) =>
        SnapshotWithin(token => CaptureSnapshot(connectionString, prepareConnection, database, waiter, token), TimeSpan.FromSeconds(5));

    static async Task<ReadCommittedSnapshotDiagnostics> CaptureSnapshot(string connectionString, Func<DbConnection, Task>? prepareConnection, string database, int waiter, CancellationToken cancellationToken)
    {
        await using var connection = await OpenMaster(connectionString, prepareConnection, cancellationToken);
        await using var command = connection.CreateCommand();
        command.CommandTimeout = 5;
        command.CommandText =
            "SELECT state_desc, user_access_desc, is_read_committed_snapshot_on FROM sys.databases WHERE name = @database; " +
            "SELECT TOP (1) wait_type, blocking_session_id FROM sys.dm_os_waiting_tasks WHERE session_id = @waiter; " +
            "SELECT TOP (10) l.request_session_id, r.wait_type, r.blocking_session_id " +
            "FROM sys.dm_tran_locks l " +
            "LEFT JOIN sys.dm_exec_requests r ON r.session_id = l.request_session_id " +
            "WHERE l.resource_type = 'DATABASE' AND l.resource_database_id = DB_ID(@database) AND l.request_session_id NOT IN (@waiter, @@SPID);";
        command.Parameters.AddWithValue(nameof(database), database);
        command.Parameters.AddWithValue(nameof(waiter), waiter);
        await using var reader = await command.ExecuteReaderAsync(cancellationToken);

        string? state = null;
        string? access = null;
        bool? on = null;
        if (await reader.ReadAsync(cancellationToken))
        {
            state = reader.GetString(0);
            access = reader.GetString(1);
            on = reader.GetBoolean(2);
        }

        await reader.NextResultAsync(cancellationToken);
        string? waitType = null;
        int? waitingOn = null;
        if (await reader.ReadAsync(cancellationToken))
        {
            waitType = await Text(reader, 0, cancellationToken);
            waitingOn = await Number(reader, 1, cancellationToken);
        }

        await reader.NextResultAsync(cancellationToken);
        var blockers = new List<ReadCommittedSnapshotBlocker>();
        while (await reader.ReadAsync(cancellationToken))
        {
            blockers.Add(new ReadCommittedSnapshotBlocker(
                Convert.ToInt32(reader.GetValue(0)),
                null,
                null,
                null,
                null,
                null,
                await Text(reader, 1, cancellationToken),
                await Number(reader, 2, cancellationToken)));
        }

        return new ReadCommittedSnapshotDiagnostics(state, access, on, waitType, waitingOn, blockers);
    }

    static async Task<string?> Text(DbDataReader reader, int ordinal, CancellationToken cancellationToken) => await reader.IsDBNullAsync(ordinal, cancellationToken) ? null : Convert.ToString(reader.GetValue(ordinal));

    static async Task<int?> Number(DbDataReader reader, int ordinal, CancellationToken cancellationToken) => await reader.IsDBNullAsync(ordinal, cancellationToken) ? null : Convert.ToInt32(reader.GetValue(ordinal));
}
