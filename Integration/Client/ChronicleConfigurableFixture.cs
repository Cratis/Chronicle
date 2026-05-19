// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

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
        // For out-of-process modes the docker chronicle owns its data. Resetting it via the
        // development-only gRPC operation wipes the backing store (per storage type, via
        // ICanPerformKernelStateReset) and re-bootstraps identity + the system event store —
        // all without restarting any container or process. The in-process test silo shares
        // the same MongoDB / SQL storage with the docker chronicle, so this single call is
        // also what restores a clean slate for the in-process side.
        if (Options.Mode == ChronicleRuntimeMode.OutOfProcess)
        {
            await ResetOutOfProcessKernelState();
            return;
        }

        // In-process mode: defer to the base MongoDB-drop behavior.
        await base.RemoveAllDatabases(excludePrefixes);
    }

    /// <inheritdoc/>
    public override Task RestartMongoDBAsync() => Task.CompletedTask;

    /// <summary>
    /// No-op kept for the existing <c>when_server_restarts</c> spec. The integration setup is
    /// explicitly designed around keeping every container and process running for the lifetime
    /// of the test session — restarts are too expensive and were the source of cross-test
    /// flakiness. The spec is now effectively a smoke test that appending events succeeds
    /// before and after a notional "restart" boundary.
    /// </summary>
    /// <returns>A completed <see cref="Task"/>.</returns>
    public Task RestartStorageAsync() => Task.CompletedTask;

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
        // Connection details are interpreted by the Chronicle kernel *inside* the container.
        // For MongoDB the kernel and mongod live in the same container, so it must connect
        // to localhost:27017 (internal mongod port), NOT the mapped host port.
        var connectionDetails = Options.StorageProvider switch
        {
            ChronicleStorageProvider.MongoDB => "mongodb://localhost:27017",
            ChronicleStorageProvider.PostgreSql => BuildAndStartPostgreSql(network),
            ChronicleStorageProvider.MsSql => BuildAndStartMsSql(network),
            ChronicleStorageProvider.Sqlite => Environment.GetEnvironmentVariable("CHRONICLE_SQLITE_CONNECTION_DETAILS") ?? "Data Source=/tmp/chronicle.db",
            _ => "mongodb://localhost:27017",
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
            .WithEnvironment("Cratis__Chronicle__Storage__Type", storageType)
            .WithEnvironment("Cratis__Chronicle__Storage__ConnectionDetails", connectionDetails);

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
    /// gRPC operation on <see cref="Contracts.Host.IServer"/>. The container
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
        var services = ((Contracts.IChronicleServicesAccessor)eventStore.Connection).Services;
        await services.Server.ResetKernelState();
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
