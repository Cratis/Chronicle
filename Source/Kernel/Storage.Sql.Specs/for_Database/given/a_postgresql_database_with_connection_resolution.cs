// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ReadModels;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_Database.given;

public class a_postgresql_database_with_connection_resolution(PostgreSqlFixture fixture) : a_postgresql_database(fixture)
{
    internal readonly RecordingConnectionResolver Resolver = new();
    ServiceProvider? _resolutionServices;

    internal string DatabaseName => new NpgsqlConnectionStringBuilder(ConnectionString).Database!;

    internal Database ResolvingDatabase(Action<ConnectionResolutionOptions>? configure = null, string schema = "", bool createIfMissing = true)
    {
        var resolution = new ConnectionResolutionOptions();
        configure?.Invoke(resolution);
        _resolutionServices ??= new ServiceCollection()
            .AddSingleton(Services.GetRequiredService<IReadModelMigrator>())
            .AddSingleton(JobsStorage)
            .AddSingleton(Resolver)
            .BuildServiceProvider();

        return CreateDatabase(
            new()
            {
                Type = StorageType.PostgreSql,
                ConnectionDetails = ConnectionString,
                Schema = schema,
                CreateIfMissing = createIfMissing,
                ConnectionResolution = resolution
            },
            _resolutionServices);
    }

    internal async Task<long> Count(string connectionString, string sql)
    {
        await using var connection = new NpgsqlConnection(connectionString);
        await connection.OpenAsync();
#pragma warning disable CA2100 // Constant specification SQL.
        await using var command = new NpgsqlCommand(sql, connection);
#pragma warning restore CA2100
        return Convert.ToInt64(await command.ExecuteScalarAsync());
    }

    async Task Destroy()
    {
        if (_resolutionServices is not null)
        {
            await _resolutionServices.DisposeAsync();
        }
    }
}
