// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsMetricsOwnerKeeper.when_pinging;

public class and_the_owner_call_fails : given.a_keeper
{
    Exception _exception;

    void Establish() => _owner.Ensure().Returns(Task.FromException(new InvalidOperationException()));

    async Task Because() => _exception = await Catch.Exception(_keeper.Ping);

    [Fact] void should_not_throw() => _exception.ShouldBeNull();
}
