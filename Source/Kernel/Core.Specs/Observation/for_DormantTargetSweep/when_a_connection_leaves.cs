// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation.for_DormantTargetSweep;

public class when_a_connection_leaves : given.a_dormant_target_sweep
{
    async Task Because() => await _sweep.ConnectionLeft(new ConnectionId(LeavingConnection), []);

    [Fact] void should_tell_the_dormant_observer_it_served() => _heldObserver.Received(1).UnsubscribeIfMatchesClient(new ConnectionId(LeavingConnection));
    [Fact] void should_leave_other_dormant_observers_alone() => _otherObserver.ReceivedCalls().ShouldBeEmpty();
}
