// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Connections;
using Microsoft.Data.SqlClient;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStrings;

public class when_applying_placement : Specification
{
    const string SqlServer = "Server=sql.example;Database=chronicle;User ID=sa;Password=secret;Encrypt=true";
    const string PostgreSql = "Host=pg.example;Database=chronicle;Username=postgres;Password=secret";

    [Fact] void should_place_a_sql_server_database() =>
        new SqlConnectionStringBuilder(SqlConnectionStrings.WithPlacement(SqlServer, new(Database: "tenant-acme"))).InitialCatalog.ShouldEqual("tenant-acme");

    [Fact] void should_keep_the_sql_server_credential() =>
        new SqlConnectionStringBuilder(SqlConnectionStrings.WithPlacement(SqlServer, new(Database: "tenant-acme"))).UserID.ShouldEqual("sa");

    [Fact] void should_place_a_postgresql_database() =>
        new NpgsqlConnectionStringBuilder(SqlConnectionStrings.WithPlacement(PostgreSql, new(Database: "tenant_acme"))).Database.ShouldEqual("tenant_acme");

    [Fact] void should_use_a_resolved_connection_string() =>
        new NpgsqlConnectionStringBuilder(SqlConnectionStrings.WithPlacement(PostgreSql, new("Host=pg-07.example;Database=tenant;Username=app"))).Host.ShouldEqual("pg-07.example");

    [Fact] void should_place_the_database_in_a_resolved_connection_string() =>
        new NpgsqlConnectionStringBuilder(SqlConnectionStrings.WithPlacement(PostgreSql, new("Host=pg-07.example;Database=tenant;Username=app", "tenant_acme"))).Database.ShouldEqual("tenant_acme");

    [Fact] void should_own_placement_with_a_database() => SqlConnectionStrings.OwnsPlacement(new(Database: "tenant")).ShouldBeTrue();
    [Fact] void should_own_placement_with_a_connection_string() => SqlConnectionStrings.OwnsPlacement(new("Host=pg")).ShouldBeTrue();
    [Fact] void should_not_own_placement_with_only_an_identity() => SqlConnectionStrings.OwnsPlacement(new(Identity: new(AccessToken: "token"))).ShouldBeFalse();
    [Fact] void should_not_own_placement_with_nothing() => SqlConnectionStrings.OwnsPlacement(ResolvedConnection.Unchanged).ShouldBeFalse();
}
