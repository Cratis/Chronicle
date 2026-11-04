// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBDatabaseDropper.when_dropping_a_database;

public class and_probe_cleanup_fails : given.a_database_dropper
{
    MongoCommandException _failure;

    void Establish()
    {
        _failure = CommandFailure(215);
        _remove = () => Task.FromException(_failure);
    }

    async Task Because() => _error = await Catch.Exception(() => MongoDBDatabaseDropper.DropAndWait(_client, DatabaseName, new MongoDBDatabaseDropper.SettleTiming(_timeout, () => _elapsed, Delay)));

    [Fact] void should_propagate_the_cleanup_error() => _error.ShouldBeSame(_failure);
    [Fact] void should_not_retry_the_create() => _creates.ShouldEqual(1);
    [Fact] void should_attempt_cleanup_once() => _removes.ShouldEqual(1);
    [Fact] void should_remove_only_the_created_probe() => _removedProbes.ShouldEqual(_createdProbes);
    [Fact] void should_not_drop_the_database_again() => _drops.ShouldEqual(1);
    [Fact] void should_not_retry_cleanup() => _delays.ShouldBeEmpty();
}
