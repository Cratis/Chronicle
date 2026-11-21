// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Storage.Sql.Sinks;
using Microsoft.Data.SqlClient;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.when_running_a_schema_change;

[Collection(SqlServerCollection.Name)]
public class and_the_sql_server_lock_is_held_elsewhere(SqlServerFixture fixture) : given.a_sql_server_database(fixture)
{
    DbConnection _holder;
    Exception _error;
    bool _changed;
    bool _reacquired;
    bool _holderReleased;
    bool _holderDisposed;
    Guid _holderConnectionId;
    int _holderSessionId;

    async Task Establish()
    {
        _holder = NewConnection();
        await RunStage("holder-open", () => _holder.OpenAsync());
        var holder = (SqlConnection)_holder;
        _holderConnectionId = holder.ClientConnectionId;
        _holderSessionId = holder.ServerProcessId;
        await RunStage("holder-acquire", () => SchemaLock.Acquire(_holder, Resource, TimeSpan.Zero));
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => RunStage("contended-acquire", () => DatabaseSchemaLock.Run(
            NewContext(),
            () =>
            {
                _changed = true;
                return Task.CompletedTask;
            },
            TimeSpan.FromMilliseconds(300))));
        await RunStage("holder-release", () => SchemaLock.Release(_holder, Resource));
        _holderReleased = true;
        await RunStage("holder-dispose", () => _holder.DisposeAsync().AsTask());
        _holderDisposed = true;
        await RunStage("reacquire", () => DatabaseSchemaLock.Run(
            NewContext(),
            () =>
            {
                _reacquired = true;
                return Task.CompletedTask;
            },
            TimeSpan.Zero));
    }

    async Task RunStage(string stage, Func<Task> operation)
    {
        try
        {
            await operation();
        }
        catch (SqlException exception)
        {
            ReportFailure(stage, exception);
            throw;
        }
    }

    void ReportFailure(string stage, SqlException exception)
    {
        try
        {
            var errors = string.Join("; ", exception.Errors.Cast<SqlError>().Select(error => $"Number={error.Number},State={error.State},Class={error.Class},Procedure={error.Procedure},Line={error.LineNumber}"));
            var diagnostic = $"DatabaseSchemaLock Stage={stage}, Database={_holder.Database}, " +
                $"HolderSessionId={_holderSessionId}, HolderConnectionId={_holderConnectionId}, HolderState={_holder.State}, " +
                $"HolderPooling={new SqlConnectionStringBuilder(ConnectionString).Pooling}, HolderReleased={_holderReleased}, HolderDisposed={_holderDisposed}, " +
                $"FailureConnectionId={exception.ClientConnectionId}, Errors=[{errors}]";
            exception.Data["DatabaseSchemaLockDiagnostic"] = diagnostic;
            Console.Error.WriteLine(diagnostic);
        }
        catch (Exception diagnosticException)
        {
            exception.Data["DatabaseSchemaLockDiagnosticFailure"] = diagnosticException.GetType().Name;
        }
    }

    async Task Destroy()
    {
        if (!_holderDisposed)
        {
            await RunStage("holder-cleanup", () => _holder.DisposeAsync().AsTask());
        }
    }

    [Fact] void should_give_up_waiting() => _error.ShouldBeOfExactType<DatabaseSchemaLockNotAcquired>();
    [Fact] void should_not_change_the_schema() => _changed.ShouldBeFalse();
    [Fact] void should_preserve_the_timeout_outcome() => ((DatabaseSchemaLockNotAcquired)_error).Outcome.ShouldEqual(DatabaseSchemaLockOutcome.Timeout);
    [Fact] void should_allow_startup_to_retry_contention() => new SqlStorageErrorClassifier().IsTransient(_error).ShouldBeTrue();
    [Fact] void should_reacquire_after_the_other_session_releases() => _reacquired.ShouldBeTrue();
}
