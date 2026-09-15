// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Namespaces;

namespace Cratis.Chronicle.Observation.Reactors.Kernel.for_ReactorsReactor;

/// <summary>
/// A namespace is brand new the instant it is added, so it never has data at this point. Registering reactors for
/// it here would materialize its storage for a namespace that may never receive a single event - the next server
/// rehydration picks up reactor registration for namespaces that do go on to receive data.
/// </summary>
public class when_namespace_added_with_no_data : given.a_reactors_reactor
{
    NamespaceAdded _event;
    EventContext _context;
    EventStoreName _eventStoreName;
    EventStoreNamespaceName _namespaceName;

    void Establish()
    {
        _eventStoreName = new EventStoreName("some-store");
        _namespaceName = new EventStoreNamespaceName("some-namespace");
        _event = new NamespaceAdded(_eventStoreName, _namespaceName);
        _context = EventContext.From(
            EventStoreName.System,
            EventStoreNamespaceName.Default,
            EventType.Unknown,
            EventSourceType.Default,
            new EventSourceId("namespace-added"),
            EventStreamType.All,
            EventStreamId.Default,
            EventSequenceNumber.First,
            new CorrelationId(Guid.NewGuid()));

        _namespaceStorage.HasData().Returns(Task.FromResult(false));
    }

    async Task Because() => await _reactor.NamespaceAdded(_event, _context);

    [Fact] void should_not_discover_and_register_reactors_for_the_event_store_and_namespace() =>
        _reactors.DidNotReceive().DiscoverAndRegister(_eventStoreName, _namespaceName);
}
