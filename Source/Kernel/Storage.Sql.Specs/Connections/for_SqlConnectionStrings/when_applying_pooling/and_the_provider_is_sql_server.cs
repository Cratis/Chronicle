// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStrings.when_applying_pooling;

public class and_the_provider_is_sql_server : Specification
{
    const string ConnectionString = "Server=sql.example;Database=chronicle;User ID=sa;Password=secret";
    string _result;

    void Because() => _result = SqlConnectionStrings.WithPooling(
        ConnectionString,
        new PostgreSqlPooling { MaxPoolSize = 12, ConnectionIdleLifetimeSeconds = 25, ConnectionPruningIntervalSeconds = 4 });

    [Fact] void should_return_the_connection_string_unchanged() => _result.ShouldEqual(ConnectionString);
}
