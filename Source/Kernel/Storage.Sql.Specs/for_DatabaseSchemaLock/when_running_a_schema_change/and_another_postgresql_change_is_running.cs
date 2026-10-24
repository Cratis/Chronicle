// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.Sql.Sinks;

namespace Cratis.Chronicle.Storage.Sql.for_DatabaseSchemaLock.when_running_a_schema_change;

[Collection(PostgreSqlCollection.Name)]
public class and_another_postgresql_change_is_running(PostgreSqlFixture fixture) : given.a_postgresql_database(fixture)
{
    given.two_concurrent_changes _changes;

    async Task Because()
    {
        _changes = new(this);
        await _changes.Run();
    }

    [Fact] void should_hold_the_lock_in_the_database() => _changes.HeldElsewhereDuringFirst.ShouldBeTrue();
    [Fact] void should_not_start_while_the_first_runs() => _changes.SecondStartedWhileFirstHeld.ShouldBeFalse();
    [Fact] void should_start_once_the_first_is_done() => _changes.SecondStarted.ShouldBeTrue();
}
