// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriptions.when_removing;

public class a_capture : given.a_capture_events_subscriptions
{
    async Task Because() => await _subscriptions.Remove(_eventStore, _definition);

    [Fact] void should_remove_the_observer_in_every_namespace() => _observersByNamespace.Values.ShouldEachConformTo(observer => observer.ReceivedCalls().Count() == 2);
    [Fact] void should_forget_the_state_of_the_first_namespace() => _captures.Received(1).Delete(CaptureObservers.ObservationIdFor(_definition.Id, "first"));
    [Fact] void should_forget_the_state_of_the_second_namespace() => _captures.Received(1).Delete(CaptureObservers.ObservationIdFor(_definition.Id, "second"));
    [Fact] void should_delete_the_observer_definition() => _observers.Received(1).Delete(CaptureObservers.For(_definition.Id));
}
