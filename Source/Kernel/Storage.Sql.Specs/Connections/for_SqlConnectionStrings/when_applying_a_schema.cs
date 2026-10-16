// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.Connections.for_SqlConnectionStrings;

public class when_applying_a_schema : Specification
{
    const string SqlServer = "Server=sql.example;Database=chronicle;User ID=sa;Password=secret";
    const string PostgreSql = "Host=pg.example;Database=chronicle;Username=postgres;Password=secret";

    [Fact] void should_put_the_schema_first_on_the_postgresql_search_path() =>
        new NpgsqlConnectionStringBuilder(SqlConnectionStrings.WithSchema(PostgreSql, "chronicle")).SearchPath.ShouldEqual("chronicle");

    [Fact] void should_leave_sql_server_as_it_is() => SqlConnectionStrings.WithSchema(SqlServer, "chronicle").ShouldEqual(SqlServer);
    [Fact] void should_leave_the_connection_string_without_a_schema() => SqlConnectionStrings.WithSchema(PostgreSql, string.Empty).ShouldEqual(PostgreSql);
}
