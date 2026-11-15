// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBDatabaseDropper.when_dropping_a_database;

public class and_the_probe_fails_for_another_reason : given.a_database_dropper
{
    MongoCommandException _failure;

    void Establish()
    {
        _failure = CommandFailure(13);
        _create = () => Task.FromException(_failure);
    }

    async Task Because() => _error = await Catch.Exception(() => MongoDBDatabaseDropper.DropAndWait(_client, DatabaseName, new MongoDBDatabaseDropper.SettleTiming(_timeout, () => _elapsed, Delay)));

    [Fact] void should_propagate_the_original_error() => _error.ShouldBeSame(_failure);
    [Fact] void should_attempt_the_probe_once() => _creates.ShouldEqual(1);
    [Fact] void should_not_retry() => _delays.ShouldBeEmpty();
    [Fact] void should_not_remove_an_uncreated_probe() => _removes.ShouldEqual(0);
    [Fact] void should_not_drop_the_database_again() => _drops.ShouldEqual(1);
}
