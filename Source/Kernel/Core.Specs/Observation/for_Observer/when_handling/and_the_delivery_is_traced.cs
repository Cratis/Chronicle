// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Observation;

namespace Cratis.Chronicle.Observation.for_Observer.when_handling;

public class and_the_delivery_is_traced : given.a_traced_observer
{
    void Establish() => _definitionStorage.State = _definitionStorage.State with { Type = ObserverType.Reactor };

    Task Because() => _observer.Handle("partition",
    [
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 43UL),
        AppendedEvent.EmptyWithEventTypeAndEventSequenceNumber(event_type, 44UL)
    ]);

    [Fact] void should_trace_one_delivery() => _activities.Count.ShouldEqual(1);
    [Fact] void should_keep_the_operation_name() => _activities.Single().OperationName.ShouldEqual("cratis.chronicle.observer.handle");
    [Fact] void should_tag_the_observer_id() => _activities.Single().GetTagItem("cratis.observer.id").ShouldEqual(_observerKey.ObserverId.Value);
    [Fact] void should_tag_the_event_store() => _activities.Single().GetTagItem("cratis.eventstore.name").ShouldEqual(_observerKey.EventStore.Value);
    [Fact] void should_tag_the_namespace() => _activities.Single().GetTagItem("cratis.eventstore.namespace").ShouldEqual(_observerKey.Namespace.Value);
    [Fact] void should_tag_the_event_sequence() => _activities.Single().GetTagItem("cratis.eventsequence.id").ShouldEqual(_observerKey.EventSequenceId.Value);
    [Fact] void should_tag_the_observer_type() => _activities.Single().GetTagItem("cratis.observer.type").ShouldEqual("Reactor");
    [Fact] void should_tag_the_event_count_as_a_long() => _activities.Single().GetTagItem("cratis.event.count").ShouldEqual(2L);
}
