// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBDatabaseDropper.when_dropping_a_database;

public class and_the_database_is_drop_pending : given.a_database_dropper
{
    void Establish() => _create = () => _creates == 1 ? Task.FromException(CommandFailure(215)) : Task.CompletedTask;

    async Task Because() => _error = await Catch.Exception(() => MongoDBDatabaseDropper.DropAndWait(_client, DatabaseName, new MongoDBDatabaseDropper.SettleTiming(_timeout, () => _elapsed, Delay)));

    [Fact] void should_succeed() => _error.ShouldBeNull();
    [Fact] void should_not_drop_the_database_again() => _drops.ShouldEqual(1);
    [Fact] void should_retry_the_probe_by_error_code() => _creates.ShouldEqual(2);
    [Fact] void should_remove_one_probe() => _removes.ShouldEqual(1);
    [Fact] void should_reuse_the_same_probe() => _createdProbes.Distinct().Count().ShouldEqual(1);
    [Fact] void should_remove_the_successful_probe() => _removedProbes.ShouldEqual(_createdProbes.Take(1));
    [Fact] void should_preserve_command_order() => _commands.ShouldEqual(["drop", "create", "create", "remove"]);
    [Fact] void should_back_off_once() => _delays.ShouldEqual([TimeSpan.FromMilliseconds(100)]);
}
