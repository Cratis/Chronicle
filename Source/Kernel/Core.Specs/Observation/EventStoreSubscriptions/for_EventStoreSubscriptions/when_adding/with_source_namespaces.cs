// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation.EventStoreSubscriptions.for_EventStoreSubscriptions.when_adding;

public class with_source_namespaces : given.a_service_that_can_add
{
    async Task Because() => await _service.Add(RequestWith("Default", "Tenant1"));

    [Fact] void should_reserve_the_definition_with_the_source_namespaces() =>
        _subscriptionsManager.Received(1).Add(Arg.Is<Concepts.Observation.EventStoreSubscriptions.EventStoreSubscriptionDefinition>(
            definition => definition.SourceNamespaces.Select(_ => _.Value).SequenceEqual(new[] { "Default", "Tenant1" })));

    [Fact] void should_record_the_source_namespaces_on_the_event() =>
        _eventSequence.Received(1).Append(
            Arg.Any<Concepts.Events.EventSourceId>(),
            Arg.Is<object>(@event => @event is EventStoreSubscriptionAdded && ((EventStoreSubscriptionAdded)@event).SourceNamespaces.Select(_ => _.Value).SequenceEqual(new[] { "Default", "Tenant1" })),
            Arg.Any<CorrelationId>(),
            Arg.Any<IEnumerable<Concepts.Auditing.Causation>>(),
            Arg.Any<Concepts.Identities.Identity>(),
            Arg.Any<IEnumerable<Concepts.Events.Tag>>(),
            Arg.Any<Concepts.Events.EventSourceType>(),
            Arg.Any<Concepts.Events.EventStreamType>(),
            Arg.Any<Concepts.Events.EventStreamId>());
}
