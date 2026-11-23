// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Cratis.Chronicle.Storage.Sql.Connections;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_Database.when_resolving_connections;

public class and_the_resolved_target_changes : given.a_database_for_target_resolution
{
    SqlTarget _result;
    NpgsqlConnectionStringBuilder _connection;

    async Task Because()
    {
        await _target(_identity, ConnectionPurpose.Runtime, false);
        _resolved = new("Host=rotated.example;Database=old;Username=rotated;Password=new-resolved", "new_database", "new_schema");
        _result = await _target(_identity, ConnectionPurpose.Runtime, false);
        _connection = new(_result.ConnectionString);
    }

    [Fact] void should_connect_to_the_current_host() => _connection.Host.ShouldEqual("rotated.example");
    [Fact] void should_connect_to_the_current_database() => _connection.Database.ShouldEqual("new_database");
    [Fact] void should_use_the_current_credentials() => _connection.Password.ShouldEqual("new-resolved");
    [Fact] void should_use_the_current_schema() => _connection.SearchPath.ShouldEqual("new_schema");
    [Fact] void should_report_the_current_schema() => _result.Schema.ShouldEqual("new_schema");
}
