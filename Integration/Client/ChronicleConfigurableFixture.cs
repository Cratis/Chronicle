// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle;
using Cratis.Chronicle.Connections;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using Microsoft.Data.SqlClient;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Represents a configurable fixture for running integration specifications in-process or out-of-process.
/// </summary>
public class ChronicleConfigurableFixture : XUnit.Integration.ChronicleFixture
{
    const string MongoReplicaSetCommand = "mongod --replSet rs0 --bind_ip_all > /proc/1/fd/1 2>/proc/1/fd/2 & until mongosh --quiet --eval 'db.adminCommand(\"ping\")' >/dev/null 2>&1; do sleep 0.1; done; mongosh --eval 'rs.initiate({_id:\"rs0\",members:[{_id:0,host:\"localhost:27017\"}]})' || true; tail -f /dev/null";

    const string PostgreSqlHostName = "chronicle-postgres";
    const string MsSqlHostName = "chronicle-mssql";
    const string PostgreSqlPassword = "Chronicle_P@ss1";
    const string MsSqlPassword = "Chronicle_P@ss1";

    readonly string _imageName = Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_LOCAL_IMAGE") ?? "cratis/chronicle:latest-development";
    readonly string _outOfProcessSqlDatabaseName = $"chronicle_{Guid.NewGuid():N}";

    IContainer? _databaseContainer;
    IContainer? _outOfProcessContainer;

    /// <summary>
    /// Gets the selected runtime options.
    /// </summary>
    public ChronicleRuntimeOptions Options { get; } = ChronicleRuntimeOptions.Parse();

    /// <summary>
    /// Gets the storage type string for the in-process silo (matches Chronicle server StorageType constants).
    /// Returns null for MongoDB (uses the default MongoDB path).
    /// </summary>
    public string? InProcessStorageType =>
        Options.StorageProvider switch
        {
            ChronicleStorageProvider.Sqlite => "Sqlite",
            ChronicleStorageProvider.PostgreSql => "PostgreSql",
            ChronicleStorageProvider.MsSql => "MsSql",
            _ => null,
        };

    /// <summary>
    /// Gets the connection string for the in-process silo to use the selected SQL storage backend.
    /// Only valid when <see cref="InProcessStorageType"/> is non-null.
    /// </summary>
    /// <returns>The SQL connection string for the in-process silo.</returns>
    public string GetInProcessConnectionString() =>
        Options.StorageProvider switch
        {
            ChronicleStorageProvider.Sqlite =>
                Environment.GetEnvironmentVariable("CHRONICLE_SQLITE_CONNECTION_DETAILS") ?? "Data Source=/tmp/chronicle-inprocess-test.db",
            ChronicleStorageProvider.PostgreSql when _databaseContainer is not null =>
                $"Host=localhost;Port={_databaseContainer.GetMappedPublicPort(5432)};Database=chronicle-inprocess;Username=postgres;Password={PostgreSqlPassword}",
            ChronicleStorageProvider.PostgreSql =>
                GetRequiredEnvironmentVariable("CHRONICLE_POSTGRESQL_CONNECTION_DETAILS"),
            ChronicleStorageProvider.MsSql when _databaseContainer is not null =>
                $"Server=localhost,{_databaseContainer.GetMappedPublicPort(1433)};Database=chronicle-inprocess;User Id=sa;Password={MsSqlPassword};TrustServerCertificate=True",
            ChronicleStorageProvider.MsSql =>
                GetRequiredEnvironmentVariable("CHRONICLE_MSSQL_CONNECTION_DETAILS"),
            _ => string.Empty,
        };

    /// <inheritdoc/>
    public override async ValueTask DisposeAsync()
    {
        await (_databaseContainer?.DisposeAsync() ?? ValueTask.CompletedTask);
        await (_outOfProcessContainer?.DisposeAsync() ?? ValueTask.CompletedTask);
        await base.DisposeAsync();
    }

    /// <inheritdoc/>
    public override async Task RemoveAllDatabases(IEnumerable<string>? excludePrefixes = null)
    {
        // The base class connects to MongoDB to drop databases. For outofprocess non-MongoDB
        // modes there is no MongoDB reachable at the mapped port, so the MongoDB driver waits
        // its full serverSelectionTimeout (30 s) before failing — once per test-class boundary,
        // and twice because OnBeforeInitializeAsync calls RemoveAllDatabases twice.
        // Only invoke the base cleanup when it is actually needed.
        // For outofprocess MongoDB, deactivate server grains BEFORE dropping the databases.
        // This ensures OnDeactivateAsync writes go to the old MongoDB which is then dropped,
        // leaving a clean slate. Without this, server grains keep stale in-memory state
        // (stream subscriptions, sequence numbers) after the DB is wiped, causing intermittent
        // failures such as Orleans stream notifications being lost because the stream consumer
        // grain's subscription no longer matches the newly-reactivated projection grain.
        if (Options.Mode == ChronicleRuntimeMode.OutOfProcess &&
            Options.StorageProvider == ChronicleStorageProvider.MongoDB &&
            excludePrefixes is null)
        {
            await ResetOutOfProcessKernelState();
        }

        if (Options.Mode == ChronicleRuntimeMode.InProcess || Options.StorageProvider == ChronicleStorageProvider.MongoDB)
        {
            await base.RemoveAllDatabases(excludePrefixes);
        }

        switch (Options.StorageProvider)
        {
            case ChronicleStorageProvider.PostgreSql:
                // Truncate rather than drop/recreate: schema stays, migrations never re-run.
                // Drop/recreate caused grains' first write in a new test class to re-run EF
                // migrations, which take >30s on CI and hit the 30s Orleans grain timeout.
                await TruncateAllPostgreSqlTables(GetInProcessConnectionString());
                if (Options.Mode == ChronicleRuntimeMode.OutOfProcess && _databaseContainer is not null && excludePrefixes is null)
                {
                    // Use the development reset endpoint to deactivate all server-side grains
                    // instead of restarting the container. This avoids the 20-45s container
                    // restart cost (per test class, 80 classes = 26-60 min) and replaces it
                    // with a ~2s grain deactivation call.
                    // Order: reset first (let grains write OnDeactivateAsync to old DB),
                    // then truncate (removes all data including those writes).
                    var serverConnectionString = $"Host=localhost;Port={_databaseContainer.GetMappedPublicPort(5432)};Database={_outOfProcessSqlDatabaseName};Username=postgres;Password={PostgreSqlPassword}";
                    await ResetOutOfProcessKernelState();
                    await TruncateAllPostgreSqlTables(serverConnectionString);
                }

                NpgsqlConnection.ClearAllPools();
                return;

            case ChronicleStorageProvider.MsSql:
                // Same reasoning: truncate keeps schema, avoids migration re-run timeouts.
                await TruncateAllMsSqlTables(GetInProcessConnectionString());
                if (Options.Mode == ChronicleRuntimeMode.OutOfProcess && _databaseContainer is not null && _outOfProcessContainer is not null && excludePrefixes is null)
                {
                    // Same reasoning as PostgreSql: use reset endpoint instead of container restart.
                    var serverConnectionString = $"Server=localhost,{_databaseContainer.GetMappedPublicPort(1433)};Database={_outOfProcessSqlDatabaseName};User Id=sa;Password={MsSqlPassword};TrustServerCertificate=True";
                    await ResetOutOfProcessKernelState();
                    await TruncateAllMsSqlTables(serverConnectionString);
                }

                SqlConnection.ClearAllPools();
                return;
        }

        if (Options.StorageProvider != ChronicleStorageProvider.Sqlite)
        {
            return;
        }

        // For outofprocess SQLite, the SQLite file must be deleted after the reset endpoint
        // clears connection pools. The reset endpoint (when storage is SQLite) calls
        // SqliteConnection.ClearAllPools() so pooled file descriptors are released before
        // we delete the file. No container restart is needed — this eliminates the 30-45s
        // restart overhead that was consuming the 120s Because() timeout budget.
        // Only run on the first cleanup call (excludePrefixes == null); the second call is
        // for inprocess grain state cleanup only.
        if (Options.Mode == ChronicleRuntimeMode.OutOfProcess && _outOfProcessContainer is not null && excludePrefixes is null)
        {
            // 1. Deactivate grains + clear SQLite pools (server-side) via the reset endpoint.
            await ResetOutOfProcessKernelState();

            // 2. Delete all Chronicle SQLite files — connections are now closed so the next open
            //    creates fresh files. The server creates one file per event-store namespace
            //    (e.g. chronicle.db, chronicle_Testing.db, chronicle_Testing_default.db), so we
            //    must delete the base file AND all the underscore-suffixed namespace variants.
            var outOfProcessConnectionDetails = Environment.GetEnvironmentVariable("CHRONICLE_SQLITE_CONNECTION_DETAILS") ?? "Data Source=/tmp/chronicle.db";
            var outOfProcessDbPath = ExtractSqliteDataSource(outOfProcessConnectionDetails);
            var outOfProcessDirPath = Path.GetDirectoryName(outOfProcessDbPath) ?? "/tmp";
            var outOfProcessBaseName = Path.GetFileNameWithoutExtension(outOfProcessDbPath);
            var outOfProcessExt = Path.GetExtension(outOfProcessDbPath);
            await _outOfProcessContainer.ExecAsync(["/bin/sh", "-c", $"rm -f {outOfProcessDbPath} {outOfProcessDirPath}/{outOfProcessBaseName}_*{outOfProcessExt}"]);
        }

        // SQLite: clean up the local SQLite files that the in-process silo writes to.
        // This applies to both InProcess and OutOfProcess modes because the in-process kernel
        // used by the test always targets local SQLite files (Data Source=...).
        var inProcessConnectionString = GetInProcessConnectionString();
        var inProcessBuilder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = inProcessConnectionString };
        if (!inProcessBuilder.TryGetValue("Data Source", out var inProcessDataSourceObj) &&
            !inProcessBuilder.TryGetValue("Filename", out inProcessDataSourceObj))
        {
            return;
        }

        var inProcessDataSource = inProcessDataSourceObj?.ToString() ?? string.Empty;
        var inProcessDirectory = Path.GetDirectoryName(inProcessDataSource) ?? string.Empty;
        var inProcessBaseName = Path.GetFileNameWithoutExtension(inProcessDataSource);
        var inProcessExtension = Path.GetExtension(inProcessDataSource);

        // Only delete event-store and namespace databases (files with an underscore after the
        // base name, e.g. chronicle-inprocess-test_Testing.db). Never delete the cluster
        // database (chronicle-inprocess-test.db) — it holds auth state that must survive
        // between test classes within the same test run.
        var pattern = $"{inProcessBaseName}_*{inProcessExtension}";
        var matchingFiles = Directory.GetFiles(
            string.IsNullOrEmpty(inProcessDirectory) ? "." : inProcessDirectory,
            pattern);

        // Release all pooled SQLite connections before deleting files.
        // On Linux, deleting a file while a connection is open merely unlinks the directory
        // entry; the old inode stays accessible via the pool and new connections reuse it,
        // meaning the "deleted" database is still in use. Clearing the pool ensures the next
        // DbContext creation opens a fresh connection to the newly-created file.
        SqliteConnection.ClearAllPools();

        foreach (var file in matchingFiles)
        {
            if (excludePrefixes?.Any() == true)
            {
                var fileBaseName = Path.GetFileNameWithoutExtension(file);
                var suffix = fileBaseName.Length > inProcessBaseName.Length + 1
                    ? fileBaseName[(inProcessBaseName.Length + 1)..]
                    : string.Empty;
                if (excludePrefixes.Any(p => suffix.StartsWith(p, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }
            }

            try { File.Delete(file); } catch { /* ignore */ }
        }
    }

    static async Task TruncateAllPostgreSqlTables(string connectionString)
    {
        await using var conn = new NpgsqlConnection(connectionString);
        try
        {
            await conn.OpenAsync();
        }
        catch (NpgsqlException)
        {
            // Database may not exist yet on the first test class boundary — skip truncation
            // and let EF Core create and migrate the database on first grain access.
            return;
        }

        const string sql = """
            DO $$ DECLARE
                r RECORD;
            BEGIN
                FOR r IN (SELECT tablename FROM pg_tables
                          WHERE schemaname = 'public' AND tablename != '__EFMigrationsHistory') LOOP
                    EXECUTE 'TRUNCATE TABLE "' || r.tablename || '" CASCADE';
                END LOOP;
            END $$;
            """;
        await using var cmd = new NpgsqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    static async Task TruncateAllMsSqlTables(string connectionString)
    {
        await using var conn = new SqlConnection(connectionString);
        try
        {
            await conn.OpenAsync();
        }
        catch (SqlException)
        {
            // Database may not exist yet on the first test class boundary.
            // The server will create and migrate it on next connection — skip truncation.
            return;
        }

        // Switch to SIMPLE recovery so the transaction log is automatically reclaimed after
        // each checkpoint — prevents unbounded log growth from repeated DELETE operations
        // that would cause MSSQL to auto-grow the log and pause all writes for 30+ seconds.
        var dbName = conn.Database;
#pragma warning disable CA2100
        await using var recoveryCmd = new SqlCommand($"IF EXISTS (SELECT 1 FROM sys.databases WHERE name = N'{dbName}' AND recovery_model_desc != 'SIMPLE') ALTER DATABASE [{dbName}] SET RECOVERY SIMPLE WITH NO_WAIT", conn);
#pragma warning restore CA2100
        await recoveryCmd.ExecuteNonQueryAsync();

        // Use TRUNCATE TABLE (minimally logged, no per-row log writes) instead of DELETE FROM.
        // Chronicle's SQL schema has no FK constraints, so TRUNCATE works directly.
        const string sql = """
            DECLARE @sql NVARCHAR(MAX) = N'';
            SELECT @sql += 'TRUNCATE TABLE [' + TABLE_SCHEMA + '].[' + TABLE_NAME + '];' + CHAR(10)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME != '__EFMigrationsHistory';
            EXEC sp_executesql @sql;
            CHECKPOINT;
            """;
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    /// <inheritdoc/>
    public override async Task RestartMongoDBAsync()
    {
        if (Options.Mode == ChronicleRuntimeMode.InProcess)
        {
            // Kill only the mongod process inside the still-running container.
            // The container (and its tmpfs /data/db) stays alive, so MongoDB restarts
            // with existing data intact and Chronicle can reconnect.
            await MongoDBContainer.ExecAsync(["/bin/sh", "-c", "kill $(pgrep mongod)"]);
            await Task.Delay(2000);
            await MongoDBContainer.ExecAsync(["/bin/sh", "-c", "mongod --replSet rs0 --bind_ip_all --fork --logpath /tmp/mongod.log"]);
        }
        else
        {
            // For outofprocess the container uses an overlay filesystem (not tmpfs)
            // so data survives a full container stop+start.
            await MongoDBContainer.StopAsync();
            await MongoDBContainer.StartAsync();
        }
    }

    /// <summary>
    /// Restarts the storage backend. For MongoDB this restarts the MongoDB server; for SQL backends
    /// this restarts the SQL database container so the server reconnects to a temporarily unavailable store.
    /// For SQLite (file-based), this restarts the out-of-process Chronicle server container instead.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    public async Task RestartStorageAsync()
    {
        switch (Options.StorageProvider)
        {
            case ChronicleStorageProvider.MongoDB:
                await RestartMongoDBAsync();
                break;

            case ChronicleStorageProvider.PostgreSql:
            case ChronicleStorageProvider.MsSql:
                // Restart the SQL database container so the Chronicle server must reconnect.
                if (_databaseContainer is not null)
                {
                    await _databaseContainer.StopAsync();
                    await _databaseContainer.StartAsync();
                }

                break;

            case ChronicleStorageProvider.Sqlite:
                // SQLite is a file embedded inside the Chronicle server container.
                // Restart the server container itself; data persists via overlay filesystem.
                if (_outOfProcessContainer is not null)
                {
                    await _outOfProcessContainer.StopAsync();
                    await _outOfProcessContainer.StartAsync();
                }

                break;
        }
    }

    /// <inheritdoc/>
    protected override IContainer BuildContainer(INetwork network)
    {
        if (Options.Mode == ChronicleRuntimeMode.InProcess)
        {
            return BuildInProcessContainer(network);
        }

        _outOfProcessContainer = BuildOutOfProcessContainer(network);
        return _outOfProcessContainer;
    }

    IContainer BuildInProcessContainer(INetwork network) =>
        new ContainerBuilder("mongo")
            .WithCommand("/bin/sh", "-c", MongoReplicaSetCommand)
            .WithTmpfsMount("/data/db", AccessMode.ReadWrite)
            .WithPortBinding(MongoDBPort, 27017)
            .WithHostname(Cratis.Chronicle.XUnit.Integration.ChronicleInProcessFixture.HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(network)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(27017)
                .UntilCommandIsCompleted("/bin/sh", "-c", "mongosh --quiet --eval 'rs.status().ok' | grep -q 1"))
            .Build();

    IContainer BuildOutOfProcessContainer(INetwork network)
    {
        var connectionDetails = Options.StorageProvider switch
        {
            ChronicleStorageProvider.MongoDB => $"mongodb://localhost:{MongoDBPort}",
            ChronicleStorageProvider.PostgreSql => BuildAndStartPostgreSql(network),
            ChronicleStorageProvider.MsSql => BuildAndStartMsSql(network),
            ChronicleStorageProvider.Sqlite => Environment.GetEnvironmentVariable("CHRONICLE_SQLITE_CONNECTION_DETAILS") ?? "Data Source=/tmp/chronicle.db",
            _ => $"mongodb://localhost:{MongoDBPort}",
        };

        var storageType = Options.StorageProvider switch
        {
            ChronicleStorageProvider.MongoDB => "MongoDB",
            ChronicleStorageProvider.PostgreSql => "PostgreSql",
            ChronicleStorageProvider.MsSql => "MsSql",
            ChronicleStorageProvider.Sqlite => "Sqlite",
            _ => "MongoDB",
        };

        // MongoDB mode uses fixed port 27018→27017 so the test client can reach MongoDB directly.
        // SQL modes don't need a fixed MongoDB port — the Chronicle server has no MongoDB —
        // so we use a random host port to allow multiple SQL outofprocess test processes to
        // run concurrently without conflicting on port 27018.
        var mongoPortBinding = Options.StorageProvider == ChronicleStorageProvider.MongoDB
            ? new ContainerBuilder(_imageName).WithPortBinding(MongoDBPort, 27017)
            : new ContainerBuilder(_imageName).WithPortBinding(27017, assignRandomHostPort: true);

        var builder = mongoPortBinding
            .WithImage(_imageName)
            .WithPortBinding(8081, 8080)
            .WithPortBinding(35001, 35000)
            .WithHostname(Cratis.Chronicle.XUnit.Integration.ChronicleOutOfProcessFixture.HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(network)
            .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("Storage__Type", storageType)
            .WithEnvironment("Storage__ConnectionDetails", connectionDetails);

        var waitStrategy = Options.StorageProvider == ChronicleStorageProvider.MongoDB
            ? Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(27017)
                .UntilHttpRequestIsSucceeded(req => req.ForPort(8080).ForPath("/health"))
            : Wait.ForUnixContainer()
                .UntilHttpRequestIsSucceeded(req => req.ForPort(8080).ForPath("/health"));

        builder = builder.WithWaitStrategy(waitStrategy);

        return builder.Build();
    }

    string BuildAndStartPostgreSql(INetwork network)
    {
        var envConnectionString = Environment.GetEnvironmentVariable("CHRONICLE_POSTGRESQL_CONNECTION_DETAILS");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            return envConnectionString;
        }

        _databaseContainer = new ContainerBuilder("postgres:16")
            .WithImage("postgres:16")
            .WithHostname(PostgreSqlHostName)
            .WithPortBinding(5432, assignRandomHostPort: true)
            .WithNetwork(network)
            .WithEnvironment("POSTGRES_PASSWORD", PostgreSqlPassword)
            .WithEnvironment("POSTGRES_DB", _outOfProcessSqlDatabaseName)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilCommandIsCompleted("pg_isready", "-U", "postgres"))
            .Build();

        _databaseContainer.StartAsync().GetAwaiter().GetResult();

        return $"Host={PostgreSqlHostName};Port=5432;Database={_outOfProcessSqlDatabaseName};Username=postgres;Password={PostgreSqlPassword}";
    }

    string BuildAndStartMsSql(INetwork network)
    {
        var envConnectionString = Environment.GetEnvironmentVariable("CHRONICLE_MSSQL_CONNECTION_DETAILS");
        if (!string.IsNullOrEmpty(envConnectionString))
        {
            return envConnectionString;
        }

        _databaseContainer = new ContainerBuilder("mcr.microsoft.com/mssql/server:2022-latest")
            .WithImage("mcr.microsoft.com/mssql/server:2022-latest")
            .WithHostname(MsSqlHostName)
            .WithPortBinding(1433, assignRandomHostPort: true)
            .WithNetwork(network)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", MsSqlPassword)
            .WithEnvironment("MSSQL_PID", "Developer")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(1433))
            .Build();

        _databaseContainer.StartAsync().GetAwaiter().GetResult();

        // Give MSSQL a moment to fully initialize after the port is available
        Task.Delay(5000).GetAwaiter().GetResult();

        return $"Server={MsSqlHostName},1433;Database={_outOfProcessSqlDatabaseName};User Id=sa;Password={MsSqlPassword};TrustServerCertificate=True";
    }

    static string GetRequiredEnvironmentVariable(string name) =>
        Environment.GetEnvironmentVariable(name) switch
        {
            { Length: > 0 } value => value,
            _ => throw new InvalidOperationException($"Missing required environment variable '{name}' for selected storage provider."),
        };

    static string ExtractSqliteDataSource(string connectionString)
    {
        var builder = new System.Data.Common.DbConnectionStringBuilder { ConnectionString = connectionString };
        if (builder.TryGetValue("Data Source", out var value) || builder.TryGetValue("Filename", out value))
        {
            return value?.ToString() ?? "/tmp/chronicle.db";
        }

        return "/tmp/chronicle.db";
    }

    /// <summary>
    /// Reset the outofprocess Chronicle server's in-memory state via the development-only
    /// gRPC operation on <see cref="Cratis.Chronicle.Contracts.Host.IServer"/>. The container
    /// stays running; only the grain state and transient pools are recycled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    async Task ResetOutOfProcessKernelState()
    {
        var connectionString = new ChronicleConnectionString("chronicle://localhost:35001/?disableTls=true");
        var options = new ChronicleClientOptions
        {
            ConnectionString = connectionString,
            EventStore = "Testing",
            AutoDiscoverAndRegister = false
        };

        using var resetClient = new ChronicleClient(options);
        var eventStore = await resetClient.GetEventStore("Testing");
        var services = (eventStore.Connection as Cratis.Chronicle.Contracts.IChronicleServicesAccessor)!.Services;

        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                await services.Server.ResetKernelState();
                return;
            }
            catch
            {
                // Server may be temporarily unavailable — keep retrying briefly.
                await Task.Delay(200);
            }
        }
    }

    static async Task WaitForOutOfProcessMongoDbPrimary(IContainer container)
    {
        var deadline = DateTime.UtcNow.AddSeconds(30);
        while (DateTime.UtcNow < deadline)
        {
            try
            {
                var result = await container.ExecAsync(["/bin/sh", "-c", "mongosh --quiet --eval \"rs.status().members.filter(m=>m.stateStr==='PRIMARY').length\" 2>/dev/null"]);
                if (result.ExitCode == 0 && result.Stdout.Trim() == "1")
                {
                    return;
                }
            }
            catch
            {
                // Container might not be fully ready yet — keep polling.
            }

            await Task.Delay(1000);
        }
    }
}
