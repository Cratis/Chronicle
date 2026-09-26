// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_an_event;

public class and_the_event_has_multiple_generations : given.an_event_sequence_storage
{
    AppendedEvent _acknowledged;

    async Task Because()
    {
        var original = new ExpandoObject();
        ((IDictionary<string, object?>)original)["legacyValue"] = "original";
        var downcast = new ExpandoObject();
        ((IDictionary<string, object?>)downcast)["oldValue"] = "downcast";
        _acknowledged = (await _storage.Append(
            EventSequenceNumber.First,
            EventSourceType.Default,
            "some-source",
            EventStreamType.All,
            EventStreamId.Default,
            new EventType(_eventType.Id, 2),
            CorrelationId.NotSet,
            [],
            [],
            [],
            DateTimeOffset.UtcNow,
            new Dictionary<EventTypeGeneration, ExpandoObject> { [EventTypeGeneration.First] = downcast, [new EventTypeGeneration(2)] = original },
            new Dictionary<EventTypeGeneration, EventHash> { [EventTypeGeneration.First] = "downcast-hash", [new EventTypeGeneration(2)] = "original-hash" })).AsT0;
    }

    [Fact] void should_return_the_appended_generations_content() => ((IDictionary<string, object?>)_acknowledged.Content)["legacyValue"].ShouldEqual("original");
    [Fact] void should_return_both_generations() => _acknowledged.GenerationalContent.Keys.ShouldContainOnly([1, 2]);
    [Fact] void should_return_the_downcast_content() => _acknowledged.GenerationalContent[1].ShouldContain("downcast");
}
