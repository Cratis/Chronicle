// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_event_has_multiple_generations : given.an_event_converter
{
    Event _event;
    AppendedEvent _result;

    void Establish()
    {
        _event = CreateEvent() with
        {
            Generation = 1,
            Content = new Dictionary<string, BsonDocument>
            {
                ["1"] = BsonDocument.Parse("{\"name\":\"original\"}"),
                ["2"] = BsonDocument.Parse("{\"name\":\"migrated\"}")
            }
        };
        _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(false);
    }

    async Task Because() => _result = await _converter.ToAppendedEvent(_event);

    [Fact] void should_report_the_appended_generation() => _result.Context.EventType.Generation.ShouldEqual((EventTypeGeneration)1);
    [Fact] void should_return_the_appended_content() => ((IDictionary<string, object?>)_result.Content)["name"].ShouldEqual("original");
    [Fact] void should_keep_the_migrated_content() => _result.GenerationalContent[2].ShouldContain("migrated");
}
