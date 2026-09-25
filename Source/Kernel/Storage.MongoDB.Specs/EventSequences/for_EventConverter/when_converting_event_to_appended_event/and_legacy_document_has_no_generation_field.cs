// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_legacy_document_has_no_generation_field : given.an_event_converter
{
    Event _event;
    AppendedEvent _result;

    void Establish()
    {
        var document = CreateEvent() with { Generation = 1 };
        var bson = document.ToBsonDocument();
        bson.Remove(nameof(Event.Generation));
        _event = BsonSerializer.Deserialize<Event>(bson);
        _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(false);
    }

    async Task Because() => _result = await _converter.ToAppendedEvent(_event);

    [Fact] void should_read_the_legacy_document() => _result.Context.EventType.Generation.ShouldEqual(EventTypeGeneration.First);
}
