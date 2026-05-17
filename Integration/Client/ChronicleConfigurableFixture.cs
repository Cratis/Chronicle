// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

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
public class ChronicleConfigurableFixture : Cratis.Chronicle.XUnit.Integration.ChronicleFixture
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
        if (Options.Mode == ChronicleRuntimeMode.InProcess || Options.StorageProvider == ChronicleStorageProvider.MongoDB)
        {
            await base.RemoveAllDatabases(excludePrefixes);
        }

        switch (Options.StorageProvider)
        {
            case ChronicleStorageProvider.PostgreSql:
                await DropAndRecreateSqlDatabase("postgres", "chronicle-inprocess", GetInProcessConnectionString());
                if (Options.Mode == ChronicleRuntimeMode.OutOfProcess && _databaseContainer is not null)
                {
                    // Truncate the outofprocess server's database, then restart the server container.
                    // Truncating keeps connections alive (no stale-pool issue from drop/recreate).
                    // Restarting the container is required to evict Orleans grains: without a restart
                    // grains stay active with their old in-memory state (e.g. observer head position)
                    // and skip events in the freshly-cleared event sequence, causing observer timeouts.
                    var serverConnectionString = $"Host=localhost;Port={_databaseContainer.GetMappedPublicPort(5432)};Database={_outOfProcessSqlDatabaseName};Username=postgres;Password={PostgreSqlPassword}";
                    await TruncateAllPostgreSqlTables(serverConnectionString);
                    if (_outOfProcessContainer is not null)
                    {
                        await _outOfProcessContainer.StopAsync();
                        await _outOfProcessContainer.StartAsync();
                    }
                }

                NpgsqlConnection.ClearAllPools();
                return;

            case ChronicleStorageProvider.MsSql:
                await DropAndRecreateMsSqlDatabase("master", "chronicle-inprocess", GetInProcessConnectionString());
                if (Options.Mode == ChronicleRuntimeMode.OutOfProcess && _databaseContainer is not null)
                {
                    // Same reasoning as PostgreSql: truncate + restart.
                    var serverConnectionString = $"Server=localhost,{_databaseContainer.GetMappedPublicPort(1433)};Database={_outOfProcessSqlDatabaseName};User Id=sa;Password={MsSqlPassword};TrustServerCertificate=True";
                    await TruncateAllMsSqlTables(serverConnectionString);
                    if (_outOfProcessContainer is not null)
                    {
                        await _outOfProcessContainer.StopAsync();
                        await _outOfProcessContainer.StartAsync();
                    }
                }

                SqlConnection.ClearAllPools();
                return;
        }

        if (Options.StorageProvider != ChronicleStorageProvider.Sqlite)
        {
            return;
        }

        // For outofprocess SQLite, the SQLite file must be deleted AND the container restarted.
        // Deleting the file while the server is running only unlinks the directory entry; the
        // server's connection pool keeps the old inode open via file descriptors. After the
        // container restarts all fds are closed, the unlinked inode is freed, and the server
        // creates a fresh database from scratch when it starts up again.
        if (Options.Mode == ChronicleRuntimeMode.OutOfProcess && _outOfProcessContainer is not null)
        {
            var outOfProcessConnectionDetails = Environment.GetEnvironmentVariable("CHRONICLE_SQLITE_CONNECTION_DETAILS") ?? "Data Source=/tmp/chronicle.db";
            var outOfProcessDbPath = ExtractSqliteDataSource(outOfProcessConnectionDetails);
            await _outOfProcessContainer.ExecAsync(["rm", "-f", outOfProcessDbPath]);
            await _outOfProcessContainer.StopAsync();
            await _outOfProcessContainer.StartAsync();
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
        await conn.OpenAsync();
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

        const string sql = """
            DECLARE @sql NVARCHAR(MAX) = N'';

            SELECT @sql += 'ALTER TABLE [' + TABLE_SCHEMA + '].[' + TABLE_NAME + '] NOCHECK CONSTRAINT all;' + CHAR(10)
            FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';
            EXEC sp_executesql @sql;

            SET @sql = N'';
            SELECT @sql += 'DELETE FROM [' + TABLE_SCHEMA + '].[' + TABLE_NAME + '];' + CHAR(10)
            FROM INFORMATION_SCHEMA.TABLES
            WHERE TABLE_TYPE = 'BASE TABLE' AND TABLE_NAME != '__EFMigrationsHistory';
            EXEC sp_executesql @sql;

            SET @sql = N'';
            SELECT @sql += 'ALTER TABLE [' + TABLE_SCHEMA + '].[' + TABLE_NAME + '] WITH CHECK CHECK CONSTRAINT all;' + CHAR(10)
            FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_TYPE = 'BASE TABLE';
            EXEC sp_executesql @sql;
            """;
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
    }

    static async Task DropAndRecreateSqlDatabase(string adminDatabase, string databaseName, string connectionString)
    {
        var builder = new NpgsqlConnectionStringBuilder(connectionString) { Database = adminDatabase };
        await using var conn = new NpgsqlConnection(builder.ConnectionString);
        await conn.OpenAsync();
        await using var dropCmd = new NpgsqlCommand(
            $"DROP DATABASE IF EXISTS \"{databaseName}\" WITH (FORCE)", conn);
        await dropCmd.ExecuteNonQueryAsync();
        await using var createCmd = new NpgsqlCommand(
            $"CREATE DATABASE \"{databaseName}\"", conn);
        await createCmd.ExecuteNonQueryAsync();
    }

    static async Task DropAndRecreateMsSqlDatabase(string adminDatabase, string databaseName, string connectionString)
    {
        var builder = new SqlConnectionStringBuilder(connectionString)
        {
            InitialCatalog = adminDatabase
        };
        await using var conn = new SqlConnection(builder.ConnectionString);
        await conn.OpenAsync();
        var sql = $@"
            IF EXISTS (SELECT name FROM sys.databases WHERE name = N'{databaseName}')
            BEGIN
                ALTER DATABASE [{databaseName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
                DROP DATABASE [{databaseName}];
            END
            CREATE DATABASE [{databaseName}];";
        await using var cmd = new SqlCommand(sql, conn);
        await cmd.ExecuteNonQueryAsync();
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

        var builder = new ContainerBuilder(_imageName)
            .WithImage(_imageName)
            .WithPortBinding(8081, 8080)
            .WithPortBinding(35001, 35000)
            .WithPortBinding(MongoDBPort, 27017)
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
}
