// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBDatabaseDropper.when_dropping_a_database;

public class and_both_stages_need_retries : given.a_database_dropper
{
    void Establish()
    {
        _drop = () => _drops == 1 ? Task.FromException(CommandFailure(10055)) : Task.CompletedTask;
        _create = () => Task.FromException(CommandFailure(215));
    }

    async Task Because() => _error = await Catch.Exception(() => MongoDBDatabaseDropper.DropAndWait(_client, DatabaseName, new MongoDBDatabaseDropper.SettleTiming(_timeout, () => _elapsed, Delay)));

    [Fact] void should_fail_closed() => _error.ShouldBeOfExactType<TimeoutException>();
    [Fact] void should_share_one_budget_across_stages() => _elapsed.ShouldEqual(_timeout);
    [Fact] void should_not_give_the_probe_a_fresh_budget() => _creates.ShouldEqual(1);
    [Fact] void should_retry_the_drop_once() => _drops.ShouldEqual(2);
    [Fact] void should_not_remove_an_uncreated_probe() => _removes.ShouldEqual(0);
}
