// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStrings.when_applying_pooling;

public class and_the_idle_lifetime_is_shorter_than_the_pruning_interval : Specification
{
    string _result;

    void Because() => _result = SqlConnectionStrings.WithPooling(
        "Host=pg.example;Database=chronicle;Username=postgres;Connection Idle Lifetime=2",
        new PostgreSqlPooling { MaxPoolSize = 12, ConnectionIdleLifetimeSeconds = 25, ConnectionPruningIntervalSeconds = 4 });

    [Fact] void should_keep_the_idle_lifetime() => new NpgsqlConnectionStringBuilder(_result).ConnectionIdleLifetime.ShouldEqual(2);
    [Fact] void should_limit_the_pruning_interval_to_the_idle_lifetime() => new NpgsqlConnectionStringBuilder(_result).ConnectionPruningInterval.ShouldEqual(2);
}
