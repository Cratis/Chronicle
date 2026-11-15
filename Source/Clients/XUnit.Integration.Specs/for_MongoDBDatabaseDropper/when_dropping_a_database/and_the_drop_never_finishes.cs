// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBDatabaseDropper.when_dropping_a_database;

public class and_the_drop_never_finishes : given.a_database_dropper
{
    MongoCommandException _failure;

    void Establish()
    {
        _failure = CommandFailure(10055);
        _drop = () => Task.FromException(_failure);
    }

    async Task Because() => _error = await Catch.Exception(() => MongoDBDatabaseDropper.DropAndWait(_client, DatabaseName, new MongoDBDatabaseDropper.SettleTiming(_timeout, () => _elapsed, Delay)));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_name_the_database() => _error.Message.ShouldContain(DatabaseName);
    [Fact] void should_name_the_stage() => _error.Message.ShouldContain("drop");
    [Fact] void should_retain_the_last_pending_error() => _error.InnerException.ShouldBeSame(_failure);
    [Fact] void should_bound_attempts() => _drops.ShouldEqual(2);
    [Fact] void should_exhaust_the_budget() => _elapsed.ShouldEqual(_timeout);
    [Fact] void should_not_probe() => _creates.ShouldEqual(0);
    [Fact] void should_not_remove_a_probe() => _removes.ShouldEqual(0);
}
