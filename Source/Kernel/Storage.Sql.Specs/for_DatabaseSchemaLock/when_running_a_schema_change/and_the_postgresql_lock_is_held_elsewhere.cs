// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.when_running_a_schema_change;

[Collection(PostgreSqlCollection.Name)]
public class and_the_postgresql_lock_is_held_elsewhere(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    DbConnection _holder;
    Exception _error;
    bool _changed;
    bool _reacquired;
    bool _holderDisposed;

    async Task Establish()
    {
        _holder = NewConnection();
        await _holder.OpenAsync();
        await SchemaLock.Acquire(_holder, Resource, TimeSpan.Zero);
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => DatabaseSchemaLock.Run(
            NewContext(),
            () =>
            {
                _changed = true;
                return Task.CompletedTask;
            },
            TimeSpan.FromMilliseconds(300)));
        await SchemaLock.Release(_holder, Resource);
        await _holder.DisposeAsync();
        _holderDisposed = true;
        await DatabaseSchemaLock.Run(
            NewContext(),
            () =>
            {
                _reacquired = true;
                return Task.CompletedTask;
            },
            TimeSpan.Zero);
    }

    async Task Destroy()
    {
        if (!_holderDisposed)
        {
            await _holder.DisposeAsync();
        }
    }

    [Fact] void should_give_up_waiting() => _error.ShouldBeOfExactType<DatabaseSchemaLockNotAcquired>();
    [Fact] void should_not_change_the_schema() => _changed.ShouldBeFalse();
    [Fact] void should_preserve_the_timeout_outcome() => ((DatabaseSchemaLockNotAcquired)_error).Outcome.ShouldEqual(DatabaseSchemaLockOutcome.Timeout);
    [Fact] void should_allow_startup_to_retry_contention() => new SqlStorageErrorClassifier().IsTransient(_error).ShouldBeTrue();
    [Fact] void should_reacquire_after_the_other_session_releases() => _reacquired.ShouldBeTrue();
}
