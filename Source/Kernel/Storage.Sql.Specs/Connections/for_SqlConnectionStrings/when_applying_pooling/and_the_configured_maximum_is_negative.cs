// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStrings.when_applying_pooling;

public class and_the_configured_maximum_is_negative : Specification
{
    readonly PostgreSqlPooling _pooling = new() { MaxPoolSize = -5 };
    NpgsqlConnectionStringBuilder _withoutOverride;
    NpgsqlConnectionStringBuilder _withOverride;

    void Because()
    {
        _withoutOverride = new(SqlConnectionStrings.WithPooling("Host=localhost;Database=chronicle", _pooling));
        _withOverride = new(SqlConnectionStrings.WithPooling("Host=localhost;Database=chronicle;Maximum Pool Size=30", _pooling));
    }

    [Fact] void should_keep_at_least_one_connection_in_the_pool() => _withoutOverride.MaxPoolSize.ShouldEqual(1);
    [Fact] void should_use_the_connection_string_value() => _withOverride.MaxPoolSize.ShouldEqual(30);
}
