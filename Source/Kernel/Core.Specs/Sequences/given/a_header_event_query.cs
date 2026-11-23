// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Arc.Queries;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Events.for_PublicEventContentRedactor.given;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.given;

public class a_header_event_query : Specification
{
    protected IStorage _storage;
    protected IEventCompliance _compliance;
    protected IQueryContextManager _queryContext;
    protected Concepts.Events.AppendedEvent[] _stored;

    void Establish()
    {
        _stored = header_events.All();
        _storage = Substitute.For<IStorage>();
        _compliance = Substitute.For<IEventCompliance>();
        _queryContext = Substitute.For<IQueryContextManager>();
        var store = Substitute.For<IEventStoreStorage>();
        var @namespace = Substitute.For<IEventStoreNamespaceStorage>();
        var sequence = Substitute.For<IEventSequenceStorage>();
        var cursor = Substitute.For<IEventCursor>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(store);
        store.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(@namespace);
        @namespace.GetEventSequence(Arg.Any<Concepts.EventSequences.EventSequenceId>()).Returns(sequence);
        store.EventTypes.GetFor(Arg.Any<IEnumerable<Concepts.Events.EventType>>()).Returns([]);
        store.EventTypes.GetDefinition(Arg.Any<Concepts.Events.EventTypeId>())
            .Returns(call => Task.FromResult(new Concepts.Events.EventTypeDefinition(call.Arg<Concepts.Events.EventTypeId>(), Concepts.Events.EventTypeOwner.Server, false, [], [])));
        sequence.GetCountMatching(Arg.Any<EventSequenceQueryCriteria>()).Returns(new Concepts.Events.EventCount((ulong)_stored.Length));
        sequence.GetPage(Arg.Any<EventSequenceQueryCriteria>(), 0, int.MaxValue, Arg.Any<EventSequenceQuerySort>()).Returns(cursor);
        sequence.GetFromSequenceNumberWithHistory(Arg.Any<Concepts.Events.EventSequenceNumber>(), Arg.Any<Concepts.Events.EventSourceId?>(), eventTypes: Arg.Any<IEnumerable<Concepts.Events.EventType>>()).Returns(cursor);
        cursor.MoveNext().Returns(true, false);
        cursor.Current.Returns(_stored);
        _queryContext.Current.Returns(QueryContext.NotSet);
        _compliance.Release(Arg.Any<IEnumerable<Concepts.Events.AppendedEvent>>(), Arg.Any<IDictionary<Concepts.Events.EventType, EventTypeSchema>>())
            .Returns(call => Task.FromResult(call.ArgAt<IEnumerable<Concepts.Events.AppendedEvent>>(0).ToArray()));
    }
}
