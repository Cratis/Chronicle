// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBDatabaseDropper.when_dropping_a_database;

public class and_the_budget_has_less_than_one_retry_interval_left : given.a_database_dropper
{
    void Establish()
    {
        _timeout = TimeSpan.FromMilliseconds(150);
        _create = () => Task.FromException(CommandFailure(215));
    }

    async Task Because() => _error = await Catch.Exception(() => MongoDBDatabaseDropper.DropAndWait(_client, DatabaseName, new MongoDBDatabaseDropper.SettleTiming(_timeout, () => _elapsed, Delay)));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_cap_backoff_to_the_remaining_budget() => _delays.ShouldEqual([TimeSpan.FromMilliseconds(100), TimeSpan.FromMilliseconds(50)]);
    [Fact] void should_exhaust_only_the_given_budget() => _elapsed.ShouldEqual(_timeout);
    [Fact] void should_stop_attempting_at_the_deadline() => _creates.ShouldEqual(2);
}
