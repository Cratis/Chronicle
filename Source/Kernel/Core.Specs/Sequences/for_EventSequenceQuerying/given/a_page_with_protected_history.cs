// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Sequences.for_EventSequenceQuerying.given;

public class a_page_with_protected_history : Events.for_EventCompliance.given.an_event_with_protected_history
{
    IStorage _storage;

    void Establish()
    {
        _storage = Substitute.For<IStorage>();
        var store = _storage.GetEventStore((EventStoreName)"store");
        store.EventTypes.GetDefinition(Arg.Any<EventTypeId>()).Returns(call => new EventTypeDefinition(call.Arg<EventTypeId>(), EventTypeOwner.Client, false, _schemas.Values.Where(schema => schema.Type.Id == call.Arg<EventTypeId>()).Select(schema => new EventTypeGenerationDefinition(schema.Type.Generation, schema.Schema)), []));
        store.EventTypes.GetFor(Arg.Any<IEnumerable<Concepts.Events.EventType>>()).Returns(call => call.Arg<IEnumerable<Concepts.Events.EventType>>().Where(_schemas.ContainsKey).Select(type => _schemas[type]).ToArray());
        var cursor = Substitute.For<IEventCursor>();
        cursor.MoveNext().Returns(true, false);
        cursor.Current.Returns([_event]);
        var sequence = store.GetNamespace((EventStoreNamespaceName)"default").GetEventSequence((Concepts.EventSequences.EventSequenceId)"event-log");
        sequence.GetCountMatching(Arg.Any<EventSequenceQueryCriteria>()).Returns((global::Cratis.Chronicle.Concepts.Events.EventCount)1UL);
        sequence.GetPage(Arg.Any<EventSequenceQueryCriteria>(), 0, 10, Arg.Any<EventSequenceQuerySort>(), Arg.Any<CancellationToken>()).Returns(cursor);
    }

    protected async Task ReadPage()
    {
        var (events, _) = await EventSequenceQuerying.QueryPage(_storage, _compliance, "store", "default", "event-log", EventSequenceQueryCriteria.Empty, 0, 10, EventSequenceQuerySort.Default);
        _result = events.ToArray();
    }
}
