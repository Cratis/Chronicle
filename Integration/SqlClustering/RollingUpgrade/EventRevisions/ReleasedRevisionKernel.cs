// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Connections;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Configurations;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Networks;
using NullLogger = Microsoft.Extensions.Logging.Abstractions.NullLogger;

namespace Cratis.Chronicle.Integration.SqlClustering.RollingUpgrade.EventRevisions;

internal sealed class ReleasedRevisionKernel(string image, bool postgres) : IAsyncDisposable
{
    readonly INetwork _network = new NetworkBuilder().WithLogger(NullLogger.Instance).Build();
    IContainer? _kernel;
    SqlClusterDatabase? _database;

    internal SqlClusterDatabase Database => _database!;

    internal ChronicleConnectionString ConnectionString => new ChronicleConnectionString(
        $"chronicle://{_kernel!.Hostname}:{_kernel.GetMappedPublicPort(35000)}/?skipTlsValidation=true&skipCompatibilityCheck=true")
        .WithCredentials(ChronicleConnectionString.DevelopmentClient, ChronicleConnectionString.DevelopmentClientSecret);

    internal async Task Start()
    {
        await _network.CreateAsync();
        _database = new(postgres, _network);
        await _database.Start(CancellationToken.None);
        await PrepareSchema();
        await Restart("Migrate");
    }

    internal async Task PrepareSchema()
    {
        await using var migration = Container("Migrate")
            .WithEntrypoint("/usr/bin/tini", "--", "/app/Cratis.Chronicle.Server")
            .WithCommand("--migrate-only", "--event-store", "released-revisions", "--namespace", "Default")
            .WithWaitStrategy(Wait.ForUnixContainer().UntilMessageIsLogged(
                "Schema action MigrateAndExit completed with exit code 0",
                options => options.WithMode(WaitStrategyMode.OneShot).WithTimeout(TimeSpan.FromMinutes(3))))
            .Build();
        await migration.StartAsync();
        (await migration.GetExitCodeAsync()).ShouldEqual(0);
    }

    internal async Task Restart(string schemaMode)
    {
        if (_kernel is not null) await _kernel.DisposeAsync();
        _kernel = Container(schemaMode)
            .WithPortBinding(35000, true)
            .WithWaitStrategy(Wait.ForUnixContainer().AddCustomWaitStrategy(
                new HttpsHealthWait(35000), options => options.WithTimeout(TimeSpan.FromMinutes(3))))
            .Build();
        try
        {
            await _kernel.StartAsync();
        }
        catch (TimeoutException)
        {
            var (stdout, stderr) = await _kernel.GetLogsAsync();
            var secret = postgres
                ? new Npgsql.NpgsqlConnectionStringBuilder(Database.KernelConnection).Password!
                : new Microsoft.Data.SqlClient.SqlConnectionStringBuilder(Database.KernelConnection).Password;
            await Console.Error.WriteLineAsync($"{stdout}\n{stderr}".Replace(secret, "[redacted]", StringComparison.Ordinal));
            throw;
        }
    }

    internal Task Stop() => _kernel!.StopAsync();

    ContainerBuilder Container(string schemaMode) => new ContainerBuilder(image)
        .WithLogger(NullLogger.Instance)
        .WithNetwork(_network)
        .WithEnvironment("Cratis__Chronicle__Storage__Type", postgres ? "PostgreSql" : "MsSql")
        .WithEnvironment("Cratis__Chronicle__Storage__ConnectionDetails", Database.KernelConnection)
        .WithEnvironment("Cratis__Chronicle__Storage__SchemaMode", schemaMode)
        .WithEnvironment("Cratis__Chronicle__Clustering__Type", "Sql");

    public async ValueTask DisposeAsync()
    {
        if (_kernel is not null) await _kernel.DisposeAsync();
        if (_database is not null) await _database.DisposeAsync();
        await _network.DisposeAsync();
    }
}
