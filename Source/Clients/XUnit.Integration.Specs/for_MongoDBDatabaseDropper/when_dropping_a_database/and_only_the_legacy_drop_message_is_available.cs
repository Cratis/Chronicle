// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBDatabaseDropper.when_dropping_a_database;

public class and_only_the_legacy_drop_message_is_available : given.a_database_dropper
{
    void Establish() => _drop = () => _drops == 1 ? Task.FromException(CommandFailure(999, "Database is CURRENTLY BEING DROPPED")) : Task.CompletedTask;

    async Task Because() => _error = await Catch.Exception(() => MongoDBDatabaseDropper.DropAndWait(_client, DatabaseName, new MongoDBDatabaseDropper.SettleTiming(_timeout, () => _elapsed, Delay)));

    [Fact] void should_succeed() => _error.ShouldBeNull();
    [Fact] void should_retry_the_drop() => _drops.ShouldEqual(2);
    [Fact] void should_remove_the_probe() => _removes.ShouldEqual(1);
    [Fact] void should_back_off_once() => _delays.Count.ShouldEqual(1);
}
