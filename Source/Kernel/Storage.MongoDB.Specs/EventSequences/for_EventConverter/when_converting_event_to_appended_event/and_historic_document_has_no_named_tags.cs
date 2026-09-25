// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_historic_document_has_no_named_tags : given.an_event_converter
{
    AppendedEvent _result;

    void Establish() => _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(false);

    async Task Because()
    {
        var document = CreateEvent().ToBsonDocument();
        document.Remove("NamedTags");
        _result = await _converter.ToAppendedEvent(BsonSerializer.Deserialize<Event>(document));
    }

    [Fact] void should_read_empty_named_tags() => _result.Context.NamedTags.ShouldBeEmpty();
}
