// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_connections;

public class and_the_configured_target_changes : given.a_database_for_target_resolution
{
    NpgsqlConnectionStringBuilder _connection;

    async Task Because()
    {
        await _target(_identity, ConnectionPurpose.Runtime, false);
        _settings = new()
        {
            Storage = new()
            {
                Type = StorageType.PostgreSql,
                ConnectionDetails = "Host=new.example;Database=chronicle;Username=app;Password=new-configured",
                PostgreSqlPooling = new() { MaxPoolSize = 42 }
            }
        };
        _connection = new((await _target(_identity, ConnectionPurpose.Runtime, false)).ConnectionString);
    }

    [Fact] void should_use_the_current_configured_host() => _connection.Host.ShouldEqual("new.example");
    [Fact] void should_use_the_current_configured_credentials() => _connection.Password.ShouldEqual("new-configured");
    [Fact] void should_keep_the_resolved_database() => _connection.Database.ShouldEqual("tenant");
    [Fact] void should_use_the_current_pool_settings() => _connection.MaxPoolSize.ShouldEqual(42);
}
