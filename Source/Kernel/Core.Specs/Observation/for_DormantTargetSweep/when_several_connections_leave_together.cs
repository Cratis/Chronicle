// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Clients;

namespace Cratis.Chronicle.Observation.for_DormantTargetSweep;

public class when_several_connections_leave_together : given.a_dormant_target_sweep
{
    async Task Because()
    {
        var first = _sweep.ConnectionLeft(new ConnectionId(LeavingConnection), []);
        var second = _sweep.ConnectionLeft(new ConnectionId("another-connection"), []);
        await Task.WhenAll(first, second);
    }

    [Fact] void should_tell_each_observer_once() => _heldObserver.Received(1).UnsubscribeIfMatchesClient(new ConnectionId(LeavingConnection));
    [Fact] void should_tell_the_other_observer_about_its_own_connection() => _otherObserver.Received(1).UnsubscribeIfMatchesClient(new ConnectionId("another-connection"));
    [Fact] async Task should_read_the_subscriptions_once() => await _records.Received(1).GetAll(Arg.Any<Cratis.Chronicle.Concepts.EventStoreName>(), Arg.Any<Cratis.Chronicle.Concepts.EventStoreNamespaceName>(), Arg.Any<Cratis.Chronicle.Concepts.EventSequences.EventSequenceId>(), Arg.Any<bool>());
}
