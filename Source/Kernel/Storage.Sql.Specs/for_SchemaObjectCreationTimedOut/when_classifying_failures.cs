// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_SchemaObjectCreationTimedOut;

public class when_classifying_failures : Specification
{
    [Fact] void should_recognize_a_sql_server_command_timeout() => SchemaObjectCreationTimedOut.IsSqlServerNumber(-2).ShouldBeTrue();
    [Fact] void should_not_recognize_an_existing_sql_server_database() => SchemaObjectCreationTimedOut.IsSqlServerNumber(1801).ShouldBeFalse();
    [Fact] void should_recognize_a_postgresql_query_canceled() => SchemaObjectCreationTimedOut.Is(Postgres(PostgresErrorCodes.QueryCanceled)).ShouldBeTrue();
    [Fact] void should_recognize_a_postgresql_lock_not_available() => SchemaObjectCreationTimedOut.Is(Postgres(PostgresErrorCodes.LockNotAvailable)).ShouldBeTrue();
    [Fact] void should_not_recognize_another_postgresql_failure() => SchemaObjectCreationTimedOut.Is(Postgres(PostgresErrorCodes.UndefinedTable)).ShouldBeFalse();
    [Fact] void should_recognize_a_wrapped_failure() => SchemaObjectCreationTimedOut.Is(new InvalidOperationException("outer", Postgres(PostgresErrorCodes.QueryCanceled))).ShouldBeTrue();
    [Fact] void should_recognize_an_aggregated_failure() => SchemaObjectCreationTimedOut.Is(new AggregateException(new FormatException(), Postgres(PostgresErrorCodes.LockNotAvailable))).ShouldBeTrue();
    [Fact] void should_not_recognize_an_unrelated_failure() => SchemaObjectCreationTimedOut.Is(new FormatException("bad input")).ShouldBeFalse();
    [Fact] void should_not_recognize_no_failure() => SchemaObjectCreationTimedOut.Is(null).ShouldBeFalse();

    static PostgresException Postgres(string state) => new("failure", "ERROR", "ERROR", state);
}
