// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_historic_document_has_no_named_tags : given.an_event_converter
{
    AppendedEvent _result;
    bool _removed;

    void Establish() => _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(false);

    async Task Because()
    {
        // Written with a tag, so a document that still carried the element could not read back empty by accident.
        var document = (CreateEvent() with { NamedTags = [new NamedTagDocument("account", "one")] }).ToBsonDocument();

        // The element name comes from the registered class map, so the naming convention in use decides what is removed.
        var elementName = BsonClassMap.LookupClassMap(typeof(Event)).GetMemberMap(nameof(Event.NamedTags)).ElementName;
        _removed = document.Contains(elementName);
        document.Remove(elementName);
        _result = await _converter.ToAppendedEvent(BsonSerializer.Deserialize<Event>(document));
    }

    [Fact] void should_have_removed_the_named_tags_element() => _removed.ShouldBeTrue();
    [Fact] void should_read_empty_named_tags() => _result.Context.NamedTags.ShouldBeEmpty();
}
