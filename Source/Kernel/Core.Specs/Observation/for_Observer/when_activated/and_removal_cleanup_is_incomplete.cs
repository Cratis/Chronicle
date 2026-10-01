// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_activated;

public class and_removal_cleanup_is_incomplete : given.an_observer
{
    Exception _subscriptionError;
    Exception _allEventsError;

    async Task Establish() => await _observer.Remove();

    async Task Because()
    {
        await Crash();
        _subscriptionError = await Catch.Exception(() => _observer.Subscribe<ObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero));
        _allEventsError = await Catch.Exception(() => _observer.SubscribeToAllEvents<ObserverSubscriber>(ObserverType.Reactor, SiloAddress.Zero));
    }

    [Fact] void should_retain_the_marker() => _stateStorage.State.AlertDisposition.ShouldEqual(AlertDisposition.Removing);
    [Fact] void should_reject_subscription() => _subscriptionError.ShouldBeOfExactType<ObserverRemovalInProgress>();
    [Fact] void should_reject_all_event_subscription() => _allEventsError.ShouldBeOfExactType<ObserverRemovalInProgress>();
}
