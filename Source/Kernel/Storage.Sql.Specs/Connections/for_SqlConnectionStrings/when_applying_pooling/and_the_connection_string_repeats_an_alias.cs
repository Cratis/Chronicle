// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Configuration;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStrings.when_applying_pooling;

public class and_the_connection_string_repeats_an_alias : Specification
{
    const string ConnectionString = "Host=a;Server=b;Host=c;Database=chronicle;Username=chronicle;Password=first;Pwd=second";
    NpgsqlConnectionStringBuilder _original;
    NpgsqlConnectionStringBuilder _result;

    void Establish() => _original = new(ConnectionString);

    void Because() => _result = new(SqlConnectionStrings.WithPooling(ConnectionString, new PostgreSqlPooling()));

    [Fact] void should_resolve_the_host_as_npgsql_does() => _result.Host.ShouldEqual(_original.Host);
    [Fact] void should_resolve_the_password_as_npgsql_does() => _result.Password.ShouldEqual(_original.Password);
}
