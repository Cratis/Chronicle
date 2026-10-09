// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Captures.for_CaptureEventsSubscriptions.when_subscribing;

public class across_namespaces : given.a_capture_events_subscriptions
{
    async Task Because() => await _subscriptions.Subscribe(_eventStore, _definition);

    [Fact] void should_subscribe_in_every_namespace() => _observersByNamespace.Keys.Order().ShouldEqual(["first", "second"]);
    [Fact] void should_observe_the_inbox() => _keys.Values.ShouldEachConformTo(key => key.EventSequenceId.Value == Sequence);
    [Fact] void should_use_the_capture_observer_id() => _keys.Values.ShouldEachConformTo(key => key.ObserverId == CaptureObservers.For(_definition.Id));
    [Fact] void should_not_be_replayable() => SubscribeCall("first").GetArguments()[4].ShouldEqual(false);
    [Fact] void should_subscribe_the_resolved_event_types() =>
        ((IEnumerable<Cratis.Chronicle.Concepts.Events.EventType>)SubscribeCall("first").GetArguments()[1]).Select(_ => _.Id.Value).Order().ShouldEqual(["ShipmentDelivered", "ShipmentDispatched"]);

    NSubstitute.Core.ICall SubscribeCall(string @namespace) => _observersByNamespace[@namespace].ReceivedCalls().Single(call => call.GetMethodInfo().Name == "SubscribeAdditively");
}
