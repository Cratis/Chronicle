// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_connections;

public class and_the_cluster_target_is_cached : given.a_database_for_target_resolution
{
    NpgsqlConnectionStringBuilder _connection;

    async Task Because()
    {
        await _clusterTarget();
        _resolved = new("Host=elsewhere.example;Database=elsewhere;Username=other;Password=other", "elsewhere", "new_schema");
        _connection = new((await _clusterTarget()).ConnectionString);
    }

    [Fact] void should_keep_the_configured_cluster_host() => _connection.Host.ShouldEqual("pg.example");
    [Fact] void should_keep_the_configured_cluster_database() => _connection.Database.ShouldEqual("chronicle");
    [Fact] void should_keep_the_configured_cluster_credentials() => _connection.Password.ShouldEqual("initial");
    [Fact] void should_apply_the_current_cluster_schema() => _connection.SearchPath.ShouldEqual("new_schema");
}
