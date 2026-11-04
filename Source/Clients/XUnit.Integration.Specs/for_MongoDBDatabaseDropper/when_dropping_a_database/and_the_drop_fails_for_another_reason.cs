// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBDatabaseDropper.when_dropping_a_database;

public class and_the_drop_fails_for_another_reason : given.a_database_dropper
{
    MongoCommandException _failure;

    void Establish()
    {
        _failure = CommandFailure(13);
        _drop = () => Task.FromException(_failure);
    }

    async Task Because() => _error = await Catch.Exception(() => MongoDBDatabaseDropper.DropAndWait(_client, DatabaseName, new MongoDBDatabaseDropper.SettleTiming(_timeout, () => _elapsed, Delay)));

    [Fact] void should_propagate_the_original_error() => _error.ShouldBeSame(_failure);
    [Fact] void should_not_retry_the_drop() => _drops.ShouldEqual(1);
    [Fact] void should_not_delay() => _delays.ShouldBeEmpty();
    [Fact] void should_not_probe() => _creates.ShouldEqual(0);
}
