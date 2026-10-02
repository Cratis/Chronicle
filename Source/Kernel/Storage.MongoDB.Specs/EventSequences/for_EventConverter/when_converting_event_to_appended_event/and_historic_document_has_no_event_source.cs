// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_historic_document_has_no_event_source : given.an_event_converter
{
    AppendedEvent _result;
    bool _removed;

    void Establish() => _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(false);

    async Task Because()
    {
        // Written with an event source, so a document that still carried the element could not read back as not set by accident.
        var document = (CreateEvent() with { EventSource = new EventSourceName("ShoppingCart") }).ToBsonDocument();

        // The element name comes from the registered class map, so the naming convention in use decides what is removed.
        var elementName = BsonClassMap.LookupClassMap(typeof(Event)).GetMemberMap(nameof(Event.EventSource)).ElementName;
        _removed = document.Contains(elementName);
        document.Remove(elementName);
        _result = await _converter.ToAppendedEvent(BsonSerializer.Deserialize<Event>(document));
    }

    [Fact] void should_have_removed_the_event_source_element() => _removed.ShouldBeTrue();
    [Fact] void should_read_the_event_source_as_not_set() => _result.Context.EventSource.ShouldEqual(EventSourceName.NotSet);
}
