// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStrings.when_applying_pooling;

public class and_the_minimum_pool_size_exceeds_the_maximum : Specification
{
    string _result;

    void Because() => _result = SqlConnectionStrings.WithPooling(
        "Host=pg.example;Database=chronicle;Username=postgres;Minimum Pool Size=30",
        new PostgreSqlPooling { MaxPoolSize = 12, ConnectionIdleLifetimeSeconds = 25, ConnectionPruningIntervalSeconds = 4 });

    [Fact] void should_raise_the_maximum_to_the_minimum_pool_size() => new NpgsqlConnectionStringBuilder(_result).MaxPoolSize.ShouldEqual(30);
    [Fact] void should_keep_the_minimum_pool_size() => new NpgsqlConnectionStringBuilder(_result).MinPoolSize.ShouldEqual(30);
}
