// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using MongoDB.Bson;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.for_EventConverter.when_converting_event_to_appended_event;

public class and_appended_generation_is_missing : given.an_event_converter
{
    Event _event;
    AppendedEvent _result;

    void Establish()
    {
        _event = CreateEvent() with
        {
            Content = new Dictionary<string, BsonDocument>
            {
                ["1"] = BsonDocument.Parse("{\"name\":\"original\"}"),
                ["2"] = BsonDocument.Parse("{\"name\":\"migrated\"}")
            }
        };
        _eventTypesStorage.HasFor(Arg.Any<EventTypeId>(), Arg.Any<EventTypeGeneration>()).Returns(false);
    }

    async Task Because() => _result = await _converter.ToAppendedEvent(_event);

    [Fact] void should_keep_the_previous_highest_generation_behavior() => _result.Context.EventType.Generation.ShouldEqual((EventTypeGeneration)2);
}
