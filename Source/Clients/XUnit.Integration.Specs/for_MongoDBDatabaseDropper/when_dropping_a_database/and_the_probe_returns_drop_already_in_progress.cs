// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBDatabaseDropper.when_dropping_a_database;

public class and_the_probe_returns_drop_already_in_progress : given.a_database_dropper
{
    MongoCommandException _failure;

    void Establish()
    {
        _failure = CommandFailure(10055);
        _create = () => Task.FromException(_failure);
    }

    async Task Because() => _error = await Catch.Exception(() => MongoDBDatabaseDropper.DropAndWait(_client, DatabaseName, new MongoDBDatabaseDropper.SettleTiming(_timeout, () => _elapsed, Delay)));

    [Fact] void should_propagate_the_original_error() => _error.ShouldBeSame(_failure);
    [Fact] void should_not_retry_an_unrecognized_probe_code() => _creates.ShouldEqual(1);
    [Fact] void should_not_delay() => _delays.ShouldBeEmpty();
    [Fact] void should_not_remove_an_uncreated_probe() => _removes.ShouldEqual(0);
}
