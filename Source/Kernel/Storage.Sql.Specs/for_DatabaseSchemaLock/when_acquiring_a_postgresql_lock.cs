// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock;

public class when_acquiring_a_postgresql_lock : given.a_command_result
{
    [Fact]
    async Task should_preserve_wait_expiry_as_a_transient_timeout()
    {
        Return(false);
        var error = await Catch.Exception(() => PostgreSqlDatabaseSchemaLock.Instance.Acquire(_connection, "resource", TimeSpan.Zero));
        error.ShouldBeOfExactType<DatabaseSchemaLockNotAcquired>();
        ((DatabaseSchemaLockNotAcquired)error).Outcome.ShouldEqual(DatabaseSchemaLockOutcome.Timeout);
        new SqlStorageErrorClassifier().IsTransient(error).ShouldBeTrue();
    }

    [Fact]
    async Task should_propagate_caller_cancellation_without_converting_it_to_a_timeout()
    {
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        var canceled = new OperationCanceledException(cancellation.Token);
        _command.ExecuteScalarAsync(cancellation.Token).Returns(Task.FromException<object?>(canceled));
        var error = await Catch.Exception(() => PostgreSqlDatabaseSchemaLock.Instance.Acquire(_connection, "resource", TimeSpan.Zero, cancellation.Token));
        error.ShouldEqual(canceled);
    }
}
