// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStrings.when_applying_pooling;

public class and_it_is_applied_twice : Specification
{
    readonly PostgreSqlPooling _pooling = new() { MaxPoolSize = 12, ConnectionIdleLifetimeSeconds = 25, ConnectionPruningIntervalSeconds = 4 };
    string _appliedOnce;
    string _result;

    void Establish() => _appliedOnce = SqlConnectionStrings.WithPooling(
        "Host=pg.example;Database=chronicle;Username=postgres;Search Path=chronicle",
        _pooling);

    void Because() => _result = SqlConnectionStrings.WithPooling(_appliedOnce, _pooling);

    [Fact] void should_return_the_same_connection_string() => _result.ShouldEqual(_appliedOnce);
}
