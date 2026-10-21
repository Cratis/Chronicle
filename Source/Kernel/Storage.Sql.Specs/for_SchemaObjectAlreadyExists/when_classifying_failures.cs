// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Microsoft.Data.Sqlite;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaObjectAlreadyExists;

public class when_classifying_failures : Specification
{
    [Fact] void should_recognize_an_existing_sql_server_database() => SchemaObjectAlreadyExists.IsSqlServerNumber(1801).ShouldBeTrue();
    [Fact] void should_recognize_an_existing_sql_server_object() => SchemaObjectAlreadyExists.IsSqlServerNumber(2714).ShouldBeTrue();
    [Fact] void should_recognize_an_existing_sql_server_index() => SchemaObjectAlreadyExists.IsSqlServerNumber(1913).ShouldBeTrue();
    [Fact] void should_recognize_an_existing_sql_server_column() => SchemaObjectAlreadyExists.IsSqlServerNumber(2705).ShouldBeTrue();
    [Fact] void should_not_recognize_a_sql_server_unique_violation() => SchemaObjectAlreadyExists.IsSqlServerNumber(2627).ShouldBeFalse();
    [Fact] void should_recognize_an_existing_postgresql_database() => SchemaObjectAlreadyExists.Is(Postgres(PostgresErrorCodes.DuplicateDatabase)).ShouldBeTrue();
    [Fact] void should_recognize_an_existing_postgresql_schema() => SchemaObjectAlreadyExists.Is(Postgres(PostgresErrorCodes.DuplicateSchema)).ShouldBeTrue();
    [Fact] void should_recognize_an_existing_postgresql_table() => SchemaObjectAlreadyExists.Is(Postgres(PostgresErrorCodes.DuplicateTable)).ShouldBeTrue();
    [Fact] void should_recognize_an_existing_postgresql_column() => SchemaObjectAlreadyExists.Is(Postgres(PostgresErrorCodes.DuplicateColumn)).ShouldBeTrue();
    [Fact] void should_recognize_an_existing_postgresql_object() => SchemaObjectAlreadyExists.Is(Postgres(PostgresErrorCodes.DuplicateObject)).ShouldBeTrue();
    [Fact] void should_recognize_a_concurrent_catalog_insert() => SchemaObjectAlreadyExists.IsPostgreSqlState(PostgresErrorCodes.UniqueViolation, "pg_namespace_nspname_index").ShouldBeTrue();
    [Fact] void should_not_recognize_a_unique_violation_on_a_table_of_ours() => SchemaObjectAlreadyExists.IsPostgreSqlState(PostgresErrorCodes.UniqueViolation, "IX_events_Value").ShouldBeFalse();
    [Fact] void should_not_recognize_another_postgresql_failure() => SchemaObjectAlreadyExists.Is(Postgres(PostgresErrorCodes.UndefinedTable)).ShouldBeFalse();
    [Fact] void should_recognize_a_provider_that_only_says_so_in_the_message() => SchemaObjectAlreadyExists.Is(new SqliteException("SQLite Error 1: 'table \"events\" already exists'.", 1)).ShouldBeTrue();
    [Fact] void should_recognize_a_wrapped_failure() => SchemaObjectAlreadyExists.Is(new InvalidOperationException("outer", Postgres(PostgresErrorCodes.DuplicateTable))).ShouldBeTrue();
    [Fact] void should_recognize_an_aggregated_failure() => SchemaObjectAlreadyExists.Is(new AggregateException(new FormatException(), Postgres(PostgresErrorCodes.DuplicateColumn))).ShouldBeTrue();
    [Fact] void should_not_recognize_an_unrelated_failure() => SchemaObjectAlreadyExists.Is(new FormatException("bad input")).ShouldBeFalse();
    [Fact] void should_not_recognize_no_failure() => SchemaObjectAlreadyExists.Is(null).ShouldBeFalse();

    static PostgresException Postgres(string state) => new("failure", "ERROR", "ERROR", state);
}
