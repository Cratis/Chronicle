// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock;

public class when_acquiring_a_sql_server_lock : given.a_command_result
{
    [Theory]
    [InlineData(-1, DatabaseSchemaLockOutcome.Timeout, true)]
    [InlineData(-3, DatabaseSchemaLockOutcome.Deadlock, true)]
    [InlineData(-2, DatabaseSchemaLockOutcome.Canceled, false)]
    [InlineData(-999, DatabaseSchemaLockOutcome.Error, false)]
    [InlineData(-4, DatabaseSchemaLockOutcome.Error, false)]
    async Task should_preserve_the_outcome_and_its_retry_classification(int result, DatabaseSchemaLockOutcome outcome, bool transient)
    {
        Return(result);
        var error = await Catch.Exception(() => SqlServerDatabaseSchemaLock.Instance.Acquire(_connection, "resource", TimeSpan.Zero));
        error.ShouldBeOfExactType<DatabaseSchemaLockNotAcquired>();
        ((DatabaseSchemaLockNotAcquired)error).Outcome.ShouldEqual(outcome);
        new SqlStorageErrorClassifier().IsTransient(error).ShouldEqual(transient);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(1)]
    async Task should_accept_granted_locks(int result)
    {
        Return(result);
        var error = await Catch.Exception(() => SqlServerDatabaseSchemaLock.Instance.Acquire(_connection, "resource", TimeSpan.Zero));
        error.ShouldBeNull();
    }

    [Fact]
    async Task should_propagate_caller_cancellation_without_converting_it_to_a_lock_result()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var canceled = new OperationCanceledException(cancellation.Token);
        _command.ExecuteScalarAsync(cancellation.Token).Returns(Task.FromException<object?>(canceled));
        var error = await Catch.Exception(() => SqlServerDatabaseSchemaLock.Instance.Acquire(_connection, "resource", TimeSpan.Zero, cancellation.Token));
        error.ShouldEqual(canceled);
    }

    [Fact]
    async Task should_preserve_the_timeout_diagnostic_text()
    {
        Return(-1);
        var error = await Catch.Exception(() => SqlServerDatabaseSchemaLock.Instance.Acquire(_connection, "resource", TimeSpan.Zero));
        error.Message.ShouldEqual("DatabaseSchemaLockNotAcquired: the schema lock 'resource' of database 'database' was not acquired: timed out after 00:00:00. Another kernel may still be changing its schema.");
    }

    [Fact]
    async Task should_preserve_the_canceled_result_diagnostic_text()
    {
        Return(-2);
        var error = await Catch.Exception(() => SqlServerDatabaseSchemaLock.Instance.Acquire(_connection, "resource", TimeSpan.Zero));
        error.Message.ShouldEqual("DatabaseSchemaLockNotAcquired: the schema lock 'resource' of database 'database' was not acquired: sp_getapplock returned -2. Another kernel may still be changing its schema.");
    }
}
