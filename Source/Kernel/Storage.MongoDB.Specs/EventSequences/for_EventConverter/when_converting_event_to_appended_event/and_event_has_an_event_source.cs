// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_event_has_an_event_source : given.an_event_converter
{
    AppendedEvent _result;

    void Establish() => _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(false);

    async Task Because()
    {
        // Round-trip through BSON so the persisted representation, not just the in-memory record, is what is read back.
        var document = (CreateEvent() with { EventSource = new EventSourceName("ShoppingCart") }).ToBsonDocument();
        _result = await _converter.ToAppendedEvent(BsonSerializer.Deserialize<Event>(document));
    }

    [Fact] void should_carry_the_event_source_on_the_context() => _result.Context.EventSource.Value.ShouldEqual("ShoppingCart");
}
