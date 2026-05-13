// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;

namespace Cratis.Chronicle.Integration;

/// <summary>
/// Represents a configurable fixture for running integration specifications in-process or out-of-process.
/// </summary>
public class ChronicleConfigurableFixture : Cratis.Chronicle.XUnit.Integration.ChronicleFixture
{
    const string MongoReplicaSetCommand = "mongod --replSet rs0 --bind_ip_all > /proc/1/fd/1 2>/proc/1/fd/2 & until mongosh --quiet --eval 'db.adminCommand(\"ping\")' >/dev/null 2>&1; do sleep 0.1; done; mongosh --eval 'rs.initiate({_id:\"rs0\",members:[{_id:0,host:\"localhost:27017\"}]})' || true; tail -f /dev/null";

    readonly ChronicleRuntimeOptions _options = ChronicleRuntimeOptions.Parse();
    readonly string _imageName = Environment.GetEnvironmentVariable("CRATIS_CHRONICLE_LOCAL_IMAGE") ?? "cratis/chronicle:latest-development";

    /// <summary>
    /// Gets the selected runtime options.
    /// </summary>
    public ChronicleRuntimeOptions Options => _options;

    /// <inheritdoc/>
    protected override IContainer BuildContainer(INetwork network) =>
        _options.Mode switch
        {
            ChronicleRuntimeMode.InProcess => BuildInProcessContainer(network),
            _ => BuildOutOfProcessContainer(network),
        };

    IContainer BuildInProcessContainer(INetwork network) =>
        new ContainerBuilder("mongo")
            .WithCommand("/bin/sh", "-c", MongoReplicaSetCommand)
            .WithTmpfsMount("/data/db", AccessMode.ReadWrite)
            .WithPortBinding(27017, assignRandomHostPort: true)
            .WithHostname(Cratis.Chronicle.XUnit.Integration.ChronicleInProcessFixture.HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(network)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(27017)
                .UntilCommandIsCompleted("/bin/sh", "-c", "mongosh --quiet --eval 'rs.status().ok' | grep -q 1"))
            .Build();

    IContainer BuildOutOfProcessContainer(INetwork network)
    {
        var builder = new ContainerBuilder(_imageName)
            .WithImage(_imageName)
            .WithPortBinding(8081, 8080)
            .WithPortBinding(35001, 35000)
            .WithHostname(Cratis.Chronicle.XUnit.Integration.ChronicleOutOfProcessFixture.HostName)
            .WithBindMount(Path.Combine(Directory.GetCurrentDirectory(), "backups"), "/backups")
            .WithNetwork(network);

        var storageType = _options.StorageProvider switch
        {
            ChronicleStorageProvider.MongoDB => "MongoDB",
            ChronicleStorageProvider.PostgreSql => "PostgreSql",
            ChronicleStorageProvider.MsSql => "MsSql",
            ChronicleStorageProvider.Sqlite => "Sqlite",
            _ => "MongoDB",
        };

        var connectionDetails = _options.StorageProvider switch
        {
            ChronicleStorageProvider.MongoDB => $"mongodb://localhost:{MongoDBPort}",
            ChronicleStorageProvider.PostgreSql => GetRequiredEnvironmentVariable("CHRONICLE_POSTGRESQL_CONNECTION_DETAILS"),
            ChronicleStorageProvider.MsSql => GetRequiredEnvironmentVariable("CHRONICLE_MSSQL_CONNECTION_DETAILS"),
            ChronicleStorageProvider.Sqlite => Environment.GetEnvironmentVariable("CHRONICLE_SQLITE_CONNECTION_DETAILS") ?? "Data Source=/tmp/chronicle.db",
            _ => $"mongodb://localhost:{MongoDBPort}",
        };

        builder = builder
            .WithEnvironment("Storage__Type", storageType)
            .WithEnvironment("Storage__ConnectionDetails", connectionDetails)
            .WithWaitStrategy(_options.StorageProvider == ChronicleStorageProvider.MongoDB
                ? Wait.ForUnixContainer()
                    .UntilInternalTcpPortIsAvailable(27017)
                    .UntilHttpRequestIsSucceeded(req => req.ForPort(8080).ForPath("/health"))
                : Wait.ForUnixContainer()
                    .UntilHttpRequestIsSucceeded(req => req.ForPort(8080).ForPath("/health")));

        if (_options.StorageProvider == ChronicleStorageProvider.MongoDB)
        {
            builder = builder.WithPortBinding(MongoDBPort, 27017);
        }

        return builder.Build();
    }

    static string GetRequiredEnvironmentVariable(string name) =>
        Environment.GetEnvironmentVariable(name) switch
        {
            { Length: > 0 } value => value,
            _ => throw new InvalidOperationException($"Missing required environment variable '{name}' for selected storage provider."),
        };

    /// <summary>
    /// Resets kernel grain state in development builds for the out-of-process server.
    /// </summary>
    /// <returns>A task that completes when the reset request has finished.</returns>
    public async Task ResetKernelState()
    {
        if (_options.Mode != ChronicleRuntimeMode.OutOfProcess)
        {
            return;
        }

        using var client = new HttpClient
        {
            BaseAddress = new Uri("http://localhost:8081")
        };

        var response = await client.PostAsync("/api/development/kernel-state/reset", content: null);
        response.EnsureSuccessStatusCode();
    }
}
