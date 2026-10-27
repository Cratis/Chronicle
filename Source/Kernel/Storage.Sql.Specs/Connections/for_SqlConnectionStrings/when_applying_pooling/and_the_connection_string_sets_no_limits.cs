// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStrings.when_applying_pooling;

public class and_the_connection_string_sets_no_limits : Specification
{
    string _result;

    void Because() => _result = SqlConnectionStrings.WithPooling(
        "Host=pg.example;Database=chronicle;Username=postgres;Search Path=chronicle",
        new PostgreSqlPooling { MaxPoolSize = 12, ConnectionIdleLifetimeSeconds = 25, ConnectionPruningIntervalSeconds = 4 });

    [Fact] void should_apply_the_maximum_pool_size() => new NpgsqlConnectionStringBuilder(_result).MaxPoolSize.ShouldEqual(12);
    [Fact] void should_apply_the_idle_lifetime() => new NpgsqlConnectionStringBuilder(_result).ConnectionIdleLifetime.ShouldEqual(25);
    [Fact] void should_apply_the_pruning_interval() => new NpgsqlConnectionStringBuilder(_result).ConnectionPruningInterval.ShouldEqual(4);
    [Fact] void should_keep_the_host() => new NpgsqlConnectionStringBuilder(_result).Host.ShouldEqual("pg.example");
    [Fact] void should_keep_the_database() => new NpgsqlConnectionStringBuilder(_result).Database.ShouldEqual("chronicle");
    [Fact] void should_keep_the_username() => new NpgsqlConnectionStringBuilder(_result).Username.ShouldEqual("postgres");
    [Fact] void should_keep_the_search_path() => new NpgsqlConnectionStringBuilder(_result).SearchPath.ShouldEqual("chronicle");
}
