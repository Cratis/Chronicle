// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBDatabaseDropper.when_dropping_a_database;

public class and_the_probe_succeeds_after_the_deadline : given.a_database_dropper
{
    void Establish() => _create = () =>
    {
        _elapsed = _timeout;
        return Task.CompletedTask;
    };

    async Task Because() => _error = await Catch.Exception(() => MongoDBDatabaseDropper.DropAndWait(_client, DatabaseName, new MongoDBDatabaseDropper.SettleTiming(_timeout, () => _elapsed, Delay)));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_still_remove_the_created_probe() => _removes.ShouldEqual(1);
    [Fact] void should_remove_only_its_own_probe() => _removedProbes.ShouldEqual(_createdProbes);
    [Fact] void should_not_drop_the_database_again() => _drops.ShouldEqual(1);
    [Fact] void should_not_retry() => _delays.ShouldBeEmpty();
}
