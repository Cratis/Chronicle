// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.XUnit.Integration.for_MongoDBDatabaseDropper.when_dropping_a_database;

public class and_the_budget_is_not_positive : given.a_database_dropper
{
    void Establish() => _timeout = TimeSpan.Zero;

    async Task Because() => _error = await Catch.Exception(() => MongoDBDatabaseDropper.DropAndWait(_client, DatabaseName, new MongoDBDatabaseDropper.SettleTiming(_timeout, () => _elapsed, Delay)));

    [Fact] void should_reject_the_budget() => _error.ShouldBeOfExactType<ArgumentOutOfRangeException>();
    [Fact] void should_not_drop_the_database() => _drops.ShouldEqual(0);
    [Fact] void should_not_probe() => _creates.ShouldEqual(0);
}
