// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Runtime.InteropServices;
using Cratis.Chronicle.Connections;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using MongoDB.Driver;

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Represents a configurable fixture for running integration specifications in-process or out-of-process.
/// </summary>
public class ChronicleConfigurableFixture : XUnit.Integration.ChronicleFixture
{
    const string MongoReplicaSetCommand = "mongod --replSet rs0 --bind_ip_all > /proc/1/fd/1 2>/proc/1/fd/2 & until mongosh --quiet --eval 'db.adminCommand(\"ping\")' >/dev/null 2>&1; do sleep 0.1; done; mongosh --eval 'rs.initiate({_id:\"rs0\",members:[{_id:0,host:\"localhost:27017\"}]})' || true; tail -f /dev/null";

    const string MongoDbHostName = "chronicle-mongodb";
    const string PostgreSqlHostName = "chronicle-postgres";
    const string MsSqlHostName = "chronicle-mssql";
    const string PostgreSqlPassword = "Chronicle_P@ss1";
    const string MsSqlPassword = "Chronicle_P@ss1";

    /// <summary>
    /// Extends the global wait-strategy timeout so MSSQL (which runs heavy EF Core migrations
    /// on first startup) doesn't time out before the Chronicle server's health endpoint
    /// responds. Also raises the default in-test polling timeout to 20 seconds for SQL
    /// backends: SQLite (and to a lesser extent PostgreSQL/MSSQL) processes subscription /
    /// reactor events markedly slower than MongoDB. The default 5-second polling deadline in
    /// <see cref="TimeSpanFactory.DefaultTimeout"/> hits before the in-process silo's
    /// subscription reactor has finished its commit-to-projection cycle on SQLite, which
    /// turns into spurious "Because() was cancelled before completing" failures even though
    /// the underlying behavior is correct.
    /// </summary>
    static ChronicleConfigurableFixture()
    {
        TestcontainersSettings.WaitStrategyTimeout = TimeSpan.FromMinutes(5);

        if (Environment.GetEnvironmentVariable("CHRONICLE_TEST_TIMEOUT_SECONDS") is null
            && ChronicleRuntimeOptions.Parse().StorageProvider != ChronicleStorageProvider.MongoDB)
        {
            Environment.SetEnvironmentVariable("CHRONICLE_TEST_TIMEOUT_SECONDS", "20");
        }
    }

    /// <summary>
    /// Gets a value indicating whether the SQL Server container must be forced to run as
    /// linux/amd64 instead of the host architecture. SQL Server ships no native linux/arm64
    /// image — only linux/amd64 — so on Apple Silicon and Linux ARM64 hosts the container
    /// must run via emulation (Rosetta on macOS, qemu on Linux). The kernel container's
    /// <c>Microsoft.Data.SqlClient</c> runs natively on ARM64 from 7.0.1 onward, so no
    /// architecture override is required for the kernel itself.
    /// </summary>
    bool ShouldForceAmd64ForMsSql =>
        Options.StorageProvider == ChronicleStorageProvider.MsSql
        && RuntimeInformation.OSArchitecture == Architecture.Arm64;

    readonly string _imageName = Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_LOCAL_IMAGE") ?? "cratis/chronicle:latest-development";
    readonly string _outOfProcessSqlDatabaseName = $"chronicle_{Guid.NewGuid():N}";

    IContainer? _databaseContainer;
    IContainer? _outOfProcessMongoContainer;
#pragma warning disable CA2213 // _outOfProcessKernelContainer is a reference to the container the base class owns and disposes via base.DisposeAsync.
    IContainer? _outOfProcessKernelContainer;
#pragma warning restore CA2213

    /// <summary>
    /// Gets the selected runtime options.
    /// </summary>
    public ChronicleRuntimeOptions Options { get; } = ChronicleRuntimeOptions.Parse();

    /// <summary>
    /// Gets a unique database name for the in-process Orleans silo when running in out-of-process MongoDB mode.
    /// This isolates the in-process silo's grain state from the OOP container's MongoDB databases,
    /// preventing cross-silo state conflicts when both share the same MongoDB instance.
    /// </summary>
    public string InProcessMongoDatabaseName { get; } = $"chronicle_inprocess_{Guid.NewGuid():N}";

    /// <summary>
    /// Gets a unique SQLite file path for the in-process Orleans silo. The first test class of a
    /// session does not run the wipe sequence (the factory is built lazily on first access, no
    /// prior state to wipe), so reusing a process-wide file path would carry data from earlier
    /// <c>dotnet test</c> invocations into the next session's first test. A fixture-scoped GUID
    /// guarantees every test session opens a fresh file regardless of what previous sessions
    /// left in <c>/tmp</c>.
    /// </summary>
    public string InProcessSqliteFilePath { get; } = Path.Combine(Path.GetTempPath(), $"chronicle-inprocess-{Guid.NewGuid():N}.db");

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
                Environment.GetEnvironmentVariable("CHRONICLE_SQLITE_CONNECTION_DETAILS") ?? $"Data Source={InProcessSqliteFilePath}",
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
        // The kernel container is owned by the base class (assigned during BuildContainer)
        // and is disposed by base.DisposeAsync — only the auxiliary containers we built
        // ourselves need disposing here.
        await (_databaseContainer?.DisposeAsync() ?? ValueTask.CompletedTask);
        await (_outOfProcessMongoContainer?.DisposeAsync() ?? ValueTask.CompletedTask);
        await base.DisposeAsync();

        // Best-effort cleanup of the in-process silo's SQLite files. Pattern matches the
        // cluster file and every per-event-store / per-namespace sibling created at runtime.
        if (Options.StorageProvider == ChronicleStorageProvider.Sqlite)
        {
            var directory = Path.GetDirectoryName(InProcessSqliteFilePath);
            var baseName = Path.GetFileNameWithoutExtension(InProcessSqliteFilePath);
            if (!string.IsNullOrEmpty(directory) && Directory.Exists(directory))
            {
                foreach (var file in Directory.GetFiles(directory, $"{baseName}*"))
                {
                    try
                    {
                        File.Delete(file);
                    }
                    catch
                    {
                        // The file may be held open by a still-shutting-down silo connection;
                        // the unique GUID in the path means future sessions are unaffected.
                    }
                }
            }
        }
    }

    /// <inheritdoc/>
    public override async Task RemoveAllDatabases(IEnumerable<string>? excludePrefixes = null)
    {
        // For out-of-process modes the docker chronicle owns its data. Resetting it via the
        // development-only gRPC operation wipes the backing store (per storage type, via
        // ICanPerformKernelStateReset) and re-bootstraps identity + the system event store —
        // all without restarting any container or process.
        //
        // The in-process silo's SQL storage is wiped from the per-test fixture by calling
        // IDatabase.Wipe() directly; that path lives where the silo's Services are accessible
        // and where the migration cache must be invalidated atomically with the wipe.
        if (Options.Mode == ChronicleRuntimeMode.OutOfProcess)
        {
            // Only reset the OOP container on the first (unconditional) call. The second call
            // from OnBeforeInitializeAsync uses excludePrefixes to do a selective drop — for
            // out-of-process mode this is a no-op because the first reset already wiped
            // everything. Calling ResetOutOfProcessKernelState twice per test boundary would
            // double the overhead and interrupt the re-bootstrapped state written between calls.
            if (excludePrefixes?.Any() != true)
            {
                await ResetOutOfProcessKernelState();
            }

            // For out-of-process MongoDB mode the in-process silo writes its grain state to a
            // dedicated database on the same shared MongoDB instance. That database is NOT
            // touched by ResetOutOfProcessKernelState (which only wipes the OOP container's
            // own databases via gRPC), so without an explicit drop here the in-process silo
            // would carry every previous test's grain state — observer NextEventSequenceNumber,
            // reminder rows, projection sinks — into the next test.
            if (Options.StorageProvider == ChronicleStorageProvider.MongoDB &&
                _outOfProcessMongoContainer is not null)
            {
                await DropInProcessMongoDatabase();
            }

            return;
        }

        // In-process mode: defer to the base MongoDB-drop behavior.
        await base.RemoveAllDatabases(excludePrefixes);
    }

    async Task DropInProcessMongoDatabase()
    {
        var urlBuilder = new MongoUrlBuilder($"mongodb://localhost:{_outOfProcessMongoContainer.GetMappedPublicPort(27017)}")
        {
            DirectConnection = true
        };
        var settings = MongoClientSettings.FromUrl(urlBuilder.ToMongoUrl());
        using var client = new MongoClient(settings);
        await client.DropDatabaseAsync(InProcessMongoDatabaseName);
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
            return BuildInProcessMongoContainer(network);
        }

        // Out-of-process always runs the kernel and its backing database in separate
        // containers — even for MongoDB. The kernel image carries no embedded database,
        // matching the production runtime layout.
        var backingDatabase = Options.StorageProvider switch
        {
            ChronicleStorageProvider.MongoDB => BuildAndStartMongoDB(network),
            ChronicleStorageProvider.PostgreSql => BuildAndStartPostgreSql(network),
            ChronicleStorageProvider.MsSql => BuildAndStartMsSql(network),
            ChronicleStorageProvider.Sqlite => Environment.GetEnvironmentVariable("CHRONICLE_SQLITE_CONNECTION_DETAILS") ?? "Data Source=/tmp/chronicle.db",
            _ => throw new InvalidOperationException($"Unsupported storage provider '{Options.StorageProvider}'."),
        };

        // The base class only knows how to start one container; returning the kernel here
        // means the base auto-starts the kernel. Auxiliary containers (MongoDB for OOP mode,
        // PostgreSQL/MSSQL for SQL modes) have already been built and started by the helpers
        // above and assigned to _outOfProcessMongoContainer / _databaseContainer so the rest
        // of the fixture can reach them via the overridden MongoDBContainer property and the
        // GetInProcessConnectionString helper.
        return BuildOutOfProcessKernelContainer(network, backingDatabase);
    }

    /// <summary>
    /// Gets the MongoDB container the test client should connect to.
    /// In OOP mode the kernel image has no embedded MongoDB, so the dedicated MongoDB
    /// container started alongside the kernel is exposed here. In every other case the
    /// base implementation returns the (single) container built by <see cref="BuildContainer"/>.
    /// </summary>
    public override IContainer MongoDBContainer => _outOfProcessMongoContainer ?? base.MongoDBContainer;

    /// <summary>
    /// Builds the in-process MongoDB container.
    /// </summary>
    /// <remarks>
    /// Random host port avoids 'port already allocated' races when one test session hands over
    /// to the next before Docker has released the previous binding. With a fixed host port,
    /// back-to-back dotnet test invocations could fail with the new container unable to bind,
    /// and the fixture would silently continue with an unstarted container — surfacing later
    /// as ArgumentOutOfRangeException from GetMappedPublicPort.
    /// </remarks>
    /// <param name="network">The network to attach the container to.</param>
    /// <returns>The built container.</returns>
    IContainer BuildInProcessMongoContainer(INetwork network) =>
        new ContainerBuilder("mongo")
            .WithCommand("/bin/sh", "-c", MongoReplicaSetCommand)
            .WithTmpfsMount("/data/db", AccessMode.ReadWrite)
            .WithPortBinding(27017, assignRandomHostPort: true)
            .WithHostname(ChronicleInProcessFixture.HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(network)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(27017)
                .UntilCommandIsCompleted("/bin/sh", "-c", "mongosh --quiet --eval 'rs.status().ok' | grep -q 1"))
            .Build();

    IContainer BuildOutOfProcessKernelContainer(INetwork network, string connectionDetails)
    {
        var storageType = Options.StorageProvider switch
        {
            ChronicleStorageProvider.MongoDB => "MongoDB",
            ChronicleStorageProvider.PostgreSql => "PostgreSql",
            ChronicleStorageProvider.MsSql => "MsSql",
            ChronicleStorageProvider.Sqlite => "Sqlite",
            _ => throw new InvalidOperationException($"Unsupported storage provider '{Options.StorageProvider}'."),
        };

        // Random host ports avoid back-to-back test sessions colliding on the previous
        // container's still-bound ports while Docker tears it down. The actual mapped
        // ports are exposed through KernelGrpcHostPort and KernelManagementHostPort and
        // forwarded to the in-process silo + the reset client.
        var builder = new ContainerBuilder(_imageName)
            .WithImage(_imageName)
            .WithPortBinding(8080, assignRandomHostPort: true)
            .WithPortBinding(35000, assignRandomHostPort: true)
            .WithHostname(ChronicleOutOfProcessFixture.HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(network)
            .WithEnvironment("DOTNET_ENVIRONMENT", "Development")
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Development")
            .WithEnvironment("Cratis__Chronicle__Storage__Type", storageType)
            .WithEnvironment("Cratis__Chronicle__Storage__ConnectionDetails", connectionDetails)
            .WithEnvironment("Logging__LogLevel__Default", "Information")
            .WithEnvironment("Logging__LogLevel__Cratis", "Debug");

        // SQL backends (especially MSSQL) run EF Core migrations against a freshly-started
        // database container on startup. Give the health endpoint 5 minutes to respond.
        var waitStrategy = Wait.ForUnixContainer()
            .UntilHttpRequestIsSucceeded(
                req => req.ForPort(8080).ForPath("/health"),
                s => s.WithRetries(300).WithInterval(TimeSpan.FromSeconds(1)));

        var kernelContainer = builder.WithWaitStrategy(waitStrategy).Build();
        _outOfProcessKernelContainer = kernelContainer;
        return kernelContainer;
    }

    /// <summary>
    /// Gets the host port the OOP kernel container's gRPC endpoint (internal port 35000)
    /// is mapped to. Valid only in <see cref="ChronicleRuntimeMode.OutOfProcess"/> mode after
    /// the container has been started.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when invoked before the OOP kernel container has been built (e.g. when running
    /// in <see cref="ChronicleRuntimeMode.InProcess"/> mode).
    /// </exception>
    public int KernelGrpcHostPort => _outOfProcessKernelContainer is not null
        ? _outOfProcessKernelContainer.GetMappedPublicPort(35000)
        : throw new InvalidOperationException("KernelGrpcHostPort is only valid in OutOfProcess mode after the kernel container has been built.");

    /// <summary>
    /// Gets the host port the OOP kernel container's management endpoint (internal port 8080)
    /// is mapped to. Valid only in <see cref="ChronicleRuntimeMode.OutOfProcess"/> mode after
    /// the container has been started.
    /// </summary>
    /// <exception cref="InvalidOperationException">
    /// Thrown when invoked before the OOP kernel container has been built (e.g. when running
    /// in <see cref="ChronicleRuntimeMode.InProcess"/> mode).
    /// </exception>
    public int KernelManagementHostPort => _outOfProcessKernelContainer is not null
        ? _outOfProcessKernelContainer.GetMappedPublicPort(8080)
        : throw new InvalidOperationException("KernelManagementHostPort is only valid in OutOfProcess mode after the kernel container has been built.");

    string BuildAndStartMongoDB(INetwork network)
    {
        // Initiate the replica set using the docker-network hostname as the member name so
        // that drivers connecting from another container (the kernel) follow the SRV record
        // back to a name the docker DNS can resolve. Initiating with 'localhost' breaks
        // discovery from any container other than the one that owns the mongod process.
        const string mongodStart = "mongod --replSet rs0 --bind_ip_all > /proc/1/fd/1 2>/proc/1/fd/2 & ";
        const string waitForPing = "until mongosh --quiet --eval 'db.adminCommand(\"ping\")' >/dev/null 2>&1; do sleep 0.1; done; ";
        var initiateReplicaSet = $"mongosh --eval 'rs.initiate({{_id:\"rs0\",members:[{{_id:0,host:\"{MongoDbHostName}:27017\"}}]}})' || true; ";
        const string tailLoop = "tail -f /dev/null";
        var replicaSetCommand = mongodStart + waitForPing + initiateReplicaSet + tailLoop;

        // Random host port avoids 'port already allocated' races when one test session
        // hands over to the next before Docker has released the previous binding, and
        // lets multiple test processes run side-by-side. The host port is discovered
        // dynamically through MongoDBContainer.GetMappedPublicPort by every caller.
        _outOfProcessMongoContainer = new ContainerBuilder("mongo")
            .WithImage("mongo")
            .WithCommand("/bin/sh", "-c", replicaSetCommand)
            .WithTmpfsMount("/data/db", AccessMode.ReadWrite)
            .WithPortBinding(27017, assignRandomHostPort: true)
            .WithHostname(MongoDbHostName)
            .WithNetwork(network)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(27017)
                .UntilCommandIsCompleted("/bin/sh", "-c", "mongosh --quiet --eval 'rs.status().ok' | grep -q 1"))
            .Build();

        _outOfProcessMongoContainer.StartAsync().GetAwaiter().GetResult();

        return $"mongodb://{MongoDbHostName}:27017/?replicaSet=rs0";
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

        var builder = new ContainerBuilder("mcr.microsoft.com/mssql/server:2025-latest")
            .WithImage("mcr.microsoft.com/mssql/server:2025-latest")
            .WithHostname(MsSqlHostName)
            .WithPortBinding(1433, assignRandomHostPort: true)
            .WithNetwork(network)
            .WithEnvironment("ACCEPT_EULA", "Y")
            .WithEnvironment("MSSQL_SA_PASSWORD", MsSqlPassword)
            .WithEnvironment("MSSQL_PID", "Developer")

            // Wait for MSSQL to accept actual queries, not just TCP connections. SQL Server
            // takes 20-60 seconds to fully initialize after the port opens; only checking the
            // TCP port causes the Chronicle server to fail its EF Core migrations and crash
            // before the health endpoint ever responds.
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilCommandIsCompleted(
                    "/opt/mssql-tools18/bin/sqlcmd",
                    "-S",
                    "localhost",
                    "-U",
                    "sa",
                    "-P",
                    MsSqlPassword,
                    "-Q",
                    "SELECT 1",
                    "-C"));

        if (ShouldForceAmd64ForMsSql)
        {
            builder = builder.WithCreateParameterModifier(parameters => parameters.Platform = "linux/amd64");
        }

        _databaseContainer = builder.Build();

        _databaseContainer.StartAsync().GetAwaiter().GetResult();

        return $"Server={MsSqlHostName},1433;Database={_outOfProcessSqlDatabaseName};User Id=sa;Password={MsSqlPassword};TrustServerCertificate=True";
    }

    static string GetRequiredEnvironmentVariable(string name) =>
        Environment.GetEnvironmentVariable(name) switch
        {
            { Length: > 0 } value => value,
            _ => throw new InvalidOperationException($"Missing required environment variable '{name}' for selected storage provider."),
        };

    /// <summary>
    /// Reset the outofprocess Chronicle server's in-memory state via the development-only
    /// gRPC operation on <see cref="Contracts.Host.IServer"/>. The container
    /// stays running; only the grain state and transient pools are recycled.
    /// </summary>
    /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
    async Task ResetOutOfProcessKernelState()
    {
        var connectionString = new ChronicleConnectionString($"chronicle://localhost:{KernelGrpcHostPort}/?disableTls=true");
        var options = new ChronicleClientOptions
        {
            ConnectionString = connectionString,
            EventStore = "Testing",
            AutoDiscoverAndRegister = false,
            ManagementPort = KernelManagementHostPort,

            // Reset is a one-shot operation that can take longer than the 5-second keep-alive
            // watchdog window (especially on SQL backends, where wiping enumerates and truncates
            // every per-event-store database). If the watchdog tripped mid-call it would dispose
            // the underlying gRPC channel and the in-flight ResetKernelState RPC would surface as
            // ObjectDisposedException on Grpc.Net.Client.GrpcChannel. We don't need a watchdog
            // here because the client lives only for the duration of this single RPC.
            SkipKeepAlive = true
        };

        var deadline = DateTime.UtcNow.AddMinutes(2);
        while (true)
        {
            try
            {
                using var resetClient = new ChronicleClient(options);
                var eventStore = await resetClient.GetEventStore("Testing");
                var services = ((Contracts.IChronicleServicesAccessor)eventStore.Connection).Services;
                await services.Server.ResetKernelState();
                return;
            }
            catch (Exception) when (DateTime.UtcNow < deadline)
            {
                await Task.Delay(500);
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
