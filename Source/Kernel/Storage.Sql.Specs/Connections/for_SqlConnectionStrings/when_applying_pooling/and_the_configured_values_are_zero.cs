// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStrings.when_applying_pooling;

public class and_the_configured_values_are_zero : Specification
{
    NpgsqlConnectionStringBuilder _result;

    void Because() => _result = new(SqlConnectionStrings.WithPooling(
        "Host=localhost;Database=chronicle;Username=chronicle",
        new PostgreSqlPooling { MaxPoolSize = 0, ConnectionIdleLifetimeSeconds = 0, ConnectionPruningIntervalSeconds = 0 }));

    [Fact] void should_keep_at_least_one_connection_in_the_pool() => _result.MaxPoolSize.ShouldEqual(1);
    [Fact] void should_keep_an_idle_lifetime_of_at_least_one_second() => _result.ConnectionIdleLifetime.ShouldEqual(1);
    [Fact] void should_prune_at_least_every_second() => _result.ConnectionPruningInterval.ShouldEqual(1);
}
