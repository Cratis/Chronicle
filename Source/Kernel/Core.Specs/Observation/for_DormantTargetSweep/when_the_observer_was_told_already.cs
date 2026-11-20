// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation.for_DormantTargetSweep;

public class when_the_observer_was_told_already : given.a_dormant_target_sweep
{
    async Task Because() => await _sweep.ConnectionLeft(new ConnectionId(LeavingConnection), [_held]);

    [Fact] void should_not_tell_it_again() => _heldObserver.ReceivedCalls().ShouldBeEmpty();
}
