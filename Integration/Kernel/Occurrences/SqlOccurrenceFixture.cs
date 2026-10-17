// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Integration.Clustering.for_Occurrences;
using Cratis.Chronicle.Setup;
using Microsoft.Extensions.Options;
using Testcontainers.PostgreSql;

namespace Cratis.Chronicle.Kernel.Integration.Occurrences;

/// <summary>
/// One real PostgreSQL-backed silo, with two independent application connections. The database survives kernel restarts.
/// </summary>
public sealed class SqlOccurrenceFixture : OccurrenceFixture
{
    readonly PostgreSqlContainer _database = new PostgreSqlBuilder("postgres:16")
        .WithPassword(Guid.NewGuid().ToString("N"))
        .WithCommand("-c", "max_connections=300")
        .WithLabel("cratis.task", "4911-p3c-postgres")
        .Build();
    Configuration.ChronicleOptions _options = null!;

    protected override short SiloCount => 1;

    public override async Task InitializeAsync()
    {
        await _database.StartAsync();
        _options = new()
        {
            Storage = new Configuration.Storage { Type = "PostgreSql", ConnectionDetails = _database.GetConnectionString() }
        };
        try
        {
            await base.InitializeAsync();
        }
        catch
        {
            await _database.DisposeAsync();
            throw;
        }
    }

    public override async Task DisposeAsync()
    {
        try
        {
            await base.DisposeAsync();
        }
        finally
        {
            await _database.DisposeAsync();
        }
    }

    protected override Task WarmUpAsync()
    {
        SiloServices.GetRequiredService<Storage.IStorage>().System.ShouldBeOfExactType<Storage.Sql.SystemStorage>();
        return Task.CompletedTask;
    }

    protected override void ConfigureStorage(Configuration.IChronicleBuilder builder, string mongoUrl) => builder.WithSql(_options);

    protected override void ConfigureFixtureServices(IServiceCollection services)
    {
        services.AddSingleton(Options.Create(_options));
        base.ConfigureFixtureServices(services);
    }
}
