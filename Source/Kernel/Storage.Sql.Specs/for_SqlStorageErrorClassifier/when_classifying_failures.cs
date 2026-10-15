// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Microsoft.Data.Sqlite;
using Npgsql;

namespace Cratis.Chronicle.Storage.Sql.for_SqlStorageErrorClassifier;

public class when_classifying_failures : Specification
{
    readonly SqlStorageErrorClassifier _classifier = new();

    [Fact] void should_retry_cancellation() => _classifier.IsTransient(new OperationCanceledException()).ShouldBeTrue();
    [Fact] void should_retry_timeouts() => _classifier.IsTransient(new TimeoutException()).ShouldBeTrue();
    [Fact] void should_retry_nested_transient_errors() => _classifier.IsTransient(new InvalidOperationException("outer", new TimeoutException())).ShouldBeTrue();
    [Fact] void should_inspect_all_aggregate_errors() => _classifier.IsTransient(new AggregateException(new FormatException(), new TimeoutException())).ShouldBeTrue();
    [Fact] void should_honor_the_driver_transient_flag() => _classifier.IsTransient(new TransientDatabaseError()).ShouldBeTrue();
    [Fact] void should_retry_sqlite_busy() => _classifier.IsTransient(new SqliteException("busy", 5)).ShouldBeTrue();
    [Fact] void should_retry_sqlite_locked() => _classifier.IsTransient(new SqliteException("locked", 6)).ShouldBeTrue();
    [Fact] void should_not_retry_sqlite_constraint_errors() => _classifier.IsTransient(new SqliteException("constraint", 19)).ShouldBeFalse();
    [Fact] void should_retry_postgres_serialization_failure() => _classifier.IsTransient(Postgres("40001")).ShouldBeTrue();
    [Fact] void should_retry_postgres_deadlock() => _classifier.IsTransient(Postgres("40P01")).ShouldBeTrue();
    [Fact] void should_retry_postgres_connection_failure() => _classifier.IsTransient(Postgres("08006")).ShouldBeTrue();
    [Fact] void should_retry_postgres_cannot_connect_now() => _classifier.IsTransient(Postgres("57P03")).ShouldBeTrue();
    [Fact] void should_not_retry_postgres_unique_violation() => _classifier.IsTransient(Postgres("23505")).ShouldBeFalse();
    [Fact] void should_retry_sql_server_timeout() => SqlStorageErrorClassifier.IsTransientSqlServerNumber(-2).ShouldBeTrue();
    [Fact] void should_retry_sql_server_deadlock() => SqlStorageErrorClassifier.IsTransientSqlServerNumber(1205).ShouldBeTrue();
    [Fact] void should_not_retry_sql_server_unique_violation() => SqlStorageErrorClassifier.IsTransientSqlServerNumber(2627).ShouldBeFalse();
    [Fact] void should_not_retry_deterministic_errors() => _classifier.IsTransient(new FormatException()).ShouldBeFalse();

    static PostgresException Postgres(string state) => new("failure", "ERROR", "ERROR", state);

    sealed class TransientDatabaseError : DbException
    {
        public override bool IsTransient => true;
    }
}
