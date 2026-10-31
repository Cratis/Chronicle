// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

public class when_reading_events_with_a_shared_schema : given.an_event_sequence_storage
{
    readonly List<AppendedEvent> _read = [];

    void Establish() => _eventTypesStorage.GetFor(_eventType.Id, _eventType.Generation).Returns(new EventTypeSchema(
        _eventType,
        EventTypeOwner.Client,
        EventTypeSource.Code,
        JsonSchema.FromJson("""{"type":"object","properties":{"amount":{"type":"number","format":"decimal"}}}""")));

    async Task Because()
    {
        dynamic content = new ExpandoObject();
        content.amount = 193.58m;
        for (ulong index = 0; index < 101; index++)
        {
            (await Append(new(index), (ExpandoObject)content, EventHash.NotSet)).IsSuccess.ShouldBeTrue();
        }
        _eventTypesStorage.ClearReceivedCalls();
        using var cursor = await _storage.GetFromSequenceNumber(EventSequenceNumber.First);
        while (await cursor.MoveNext())
        {
            _read.AddRange(cursor.Current);
        }
    }

    [Fact] void should_read_every_event_across_cursor_batches() => _read.Count.ShouldEqual(101);
    [Fact] void should_resolve_the_schema_once() => _eventTypesStorage.Received(1).GetFor(_eventType.Id, _eventType.Generation);
    [Fact] void should_not_probe_for_schema_existence() => _eventTypesStorage.DidNotReceive().HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>());
    [Fact] void should_use_the_decimal_schema_for_every_event() => _read.TrueForAll(@event => ((IDictionary<string, object?>)@event.Content)["amount"] is decimal).ShouldBeTrue();
}
