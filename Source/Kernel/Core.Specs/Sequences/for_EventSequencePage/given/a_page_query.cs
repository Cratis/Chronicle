// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using System.Text.Json;
using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Events;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_EventSequencePage.given;

public class a_page_query : Specification
{
    protected IStorage _storage;
    protected IEventSequenceStorage _sequence;
    protected IEventCompliance _compliance;
    protected IEventCursor _cursor;
    protected Concepts.Events.AppendedEvent[] _window = [];
    protected Concepts.Events.AppendedEvent[] _released = [];
    protected EventSequencePage _page;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        var eventStore = Substitute.For<IEventStoreStorage>();
        var @namespace = Substitute.For<IEventStoreNamespaceStorage>();
        _sequence = Substitute.For<IEventSequenceStorage>();
        _compliance = Substitute.For<IEventCompliance>();
        _cursor = Substitute.For<IEventCursor>();
        _storage.GetEventStore(Arg.Any<EventStoreName>()).Returns(eventStore);
        eventStore.GetNamespace(Arg.Any<EventStoreNamespaceName>()).Returns(@namespace);
        @namespace.GetEventSequence(Arg.Any<Concepts.EventSequences.EventSequenceId>()).Returns(_sequence);
        eventStore.EventTypes.GetFor(Arg.Any<IEnumerable<Concepts.Events.EventType>>()).Returns([]);
        _sequence.GetWindow(default!, default!, 0, false).ReturnsForAnyArgs(_cursor);
        _cursor.MoveNext().Returns(true, false);
        _cursor.Current.Returns(_ => _window);
        _compliance.Release(Arg.Any<IEnumerable<Concepts.Events.AppendedEvent>>(), Arg.Any<IDictionary<Concepts.Events.EventType, EventTypeSchema>>())
            .Returns(call =>
            {
                _released = call.Arg<IEnumerable<Concepts.Events.AppendedEvent>>().ToArray();
                return Task.FromResult(_released);
            });
    }

    protected Task<EventSequencePage> Query(ulong? from = null, int count = 2, bool backward = false, ulong? until = null) =>
        EventSequencePage.Page(_storage, _compliance, new JsonSerializerOptions(), "store", "namespace", "log", from, count, backward, until);

    protected static Concepts.Events.AppendedEvent EventAt(ulong number) => new(
        Concepts.Events.EventContext.Empty with
        {
            SequenceNumber = number,
            EventType = new Concepts.Events.EventType($"event-{number}", EventTypeGeneration.First)
        },
        new ExpandoObject());
}
