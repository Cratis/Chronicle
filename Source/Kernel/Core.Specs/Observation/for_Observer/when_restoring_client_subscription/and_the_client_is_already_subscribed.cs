// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_restoring_client_subscription;

public class and_the_client_is_already_subscribed : given.an_observer_that_lost_its_client
{
    async Task Establish()
    {
        await _observer.Subscribe<NullObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);
        _namespaces.ClearReceivedCalls();
        _storageStats.ResetCounts();
    }

    Task Because() => _observer.RestoreClientSubscription<NullObserverSubscriber>(ObserverType.Reactor, [event_type], SiloAddress.Zero, _client);

    [Fact] void should_not_write_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] async Task should_not_look_up_namespaces() => await _namespaces.DidNotReceive().GetAll();
    [Fact] async Task should_keep_a_single_target() => (await _observer.GetSubscription()).Targets.Count.ShouldEqual(1);
}
