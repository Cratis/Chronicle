// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Alerts.for_AlertIncidentsMetricsOwnerKeeper.when_pinging;

public class and_the_owner_answers : given.a_keeper
{
    void Establish() => _owner.Ensure().Returns(Task.CompletedTask);

    async Task Because() => await _keeper.Ping();

    [Fact] void should_ensure_the_owner() => _owner.Received(1).Ensure();
}
