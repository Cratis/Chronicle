// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBDatabaseDropper.when_dropping_a_database;

public class and_the_drop_is_already_running : given.a_database_dropper
{
    void Establish() => _drop = () => _drops == 1 ? Task.FromException(CommandFailure(10055)) : Task.CompletedTask;

    async Task Because() => _error = await Catch.Exception(() => MongoDBDatabaseDropper.DropAndWait(_client, DatabaseName, new MongoDBDatabaseDropper.SettleTiming(_timeout, () => _elapsed, Delay)));

    [Fact] void should_succeed() => _error.ShouldBeNull();
    [Fact] void should_retry_the_drop() => _drops.ShouldEqual(2);
    [Fact] void should_create_one_probe() => _creates.ShouldEqual(1);
    [Fact] void should_remove_one_probe() => _removes.ShouldEqual(1);
    [Fact] void should_remove_the_created_probe() => _removedProbes.ShouldEqual(_createdProbes);
    [Fact] void should_preserve_command_order() => _commands.ShouldEqual(["drop", "drop", "create", "remove"]);
    [Fact] void should_back_off_once() => _delays.ShouldEqual([TimeSpan.FromMilliseconds(100)]);
}
