// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_subscribing;

public class and_the_activation_has_removed_its_source : given.an_observer
{
    Exception _error;
    Exception _allEventsError;

    async Task Establish()
    {
        await _observer.Remove();
        _storageStats.ResetCounts();
        _eventTypesStorage.ClearReceivedCalls();
    }

    async Task Because()
    {
        _error = await Catch.Exception(() => _observer.Subscribe<ObserverSubscriber>(ObserverType.Reactor, [], SiloAddress.Zero));
        _allEventsError = await Catch.Exception(() => _observer.SubscribeToAllEvents<ObserverSubscriber>(ObserverType.Reactor, SiloAddress.Zero));
    }

    [Fact] void should_reject_subscription() => _error.ShouldBeOfExactType<ObserverActivationSealed>();
    [Fact] void should_reject_all_event_subscription() => _allEventsError.ShouldBeOfExactType<ObserverActivationSealed>();
    [Fact] void should_not_write_subscription_state() => _storageStats.Writes.ShouldEqual(0);
    [Fact] void should_not_begin_schema_work() => _eventTypesStorage.ReceivedCalls().ShouldBeEmpty();
}
