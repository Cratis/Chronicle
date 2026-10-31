// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Data.Common;
using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.when_running_a_schema_change;

[Collection(SqlServerCollection.Name)]
public class and_the_sql_server_lock_is_held_elsewhere(SqlServerFixture fixture) : given.a_sql_server_database(fixture)
{
    DbConnection _holder;
    Exception _error;
    bool _changed;

    async Task Establish()
    {
        _holder = NewConnection();
        await _holder.OpenAsync();
        await SchemaLock.Acquire(_holder, Resource, TimeSpan.Zero);
    }

    async Task Because() => _error = await Catch.Exception(() => DatabaseSchemaLock.Run(
        NewContext(),
        () =>
        {
            _changed = true;
            return Task.CompletedTask;
        },
        TimeSpan.FromMilliseconds(300)));

    async Task Destroy() => await _holder.DisposeAsync();

    [Fact] void should_give_up_waiting() => _error.ShouldBeOfExactType<DatabaseSchemaLockNotAcquired>();
    [Fact] void should_not_change_the_schema() => _changed.ShouldBeFalse();
}
