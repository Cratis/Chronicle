// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.when_running_a_schema_change;

[Collection(PostgreSqlCollection.Name)]
public class and_the_change_fails(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    readonly InvalidOperationException _failure = new("the change failed");
    Exception _error;
    bool _heldAfterwards;

    async Task Because()
    {
        _error = await Catch.Exception(() => DatabaseSchemaLock.Run(NewContext(), () => throw _failure));
        _heldAfterwards = await IsHeldElsewhere();
    }

    [Fact] void should_surface_the_failure() => _error.ShouldEqual(_failure);
    [Fact] void should_release_the_lock() => _heldAfterwards.ShouldBeFalse();
}
