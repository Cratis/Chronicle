// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBDatabaseDropper.when_dropping_a_database;

public class and_the_probe_never_settles : given.a_database_dropper
{
    MongoCommandException _failure;

    void Establish()
    {
        _failure = CommandFailure(215);
        _create = () => Task.FromException(_failure);
    }

    async Task Because() => _error = await Catch.Exception(() => MongoDBDatabaseDropper.DropAndWait(_client, DatabaseName, new MongoDBDatabaseDropper.SettleTiming(_timeout, () => _elapsed, Delay)));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_name_the_database() => _error.Message.ShouldContain(DatabaseName);
    [Fact] void should_name_the_stage() => _error.Message.ShouldContain("probe");
    [Fact] void should_retain_the_last_pending_error() => _error.InnerException.ShouldBeSame(_failure);
    [Fact] void should_bound_attempts() => _creates.ShouldEqual(2);
    [Fact] void should_exhaust_the_budget() => _elapsed.ShouldEqual(_timeout);
    [Fact] void should_not_remove_an_uncreated_probe() => _removes.ShouldEqual(0);
    [Fact] void should_not_drop_the_database_again() => _drops.ShouldEqual(1);
}
