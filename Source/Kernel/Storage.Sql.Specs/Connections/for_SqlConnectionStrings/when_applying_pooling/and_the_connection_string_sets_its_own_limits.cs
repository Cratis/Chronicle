// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStrings.when_applying_pooling;

public class and_the_connection_string_sets_its_own_limits : Specification
{
    string _result;

    void Because() => _result = SqlConnectionStrings.WithPooling(
        "Host=pg.example;Database=chronicle;Username=postgres;MaxPoolSize=50;Connection Idle Lifetime=60",
        new PostgreSqlPooling { MaxPoolSize = 12, ConnectionIdleLifetimeSeconds = 25, ConnectionPruningIntervalSeconds = 4 });

    [Fact] void should_keep_the_maximum_pool_size_from_its_synonym() => new NpgsqlConnectionStringBuilder(_result).MaxPoolSize.ShouldEqual(50);
    [Fact] void should_keep_the_idle_lifetime() => new NpgsqlConnectionStringBuilder(_result).ConnectionIdleLifetime.ShouldEqual(60);
    [Fact] void should_apply_the_pruning_interval() => new NpgsqlConnectionStringBuilder(_result).ConnectionPruningInterval.ShouldEqual(4);
}
