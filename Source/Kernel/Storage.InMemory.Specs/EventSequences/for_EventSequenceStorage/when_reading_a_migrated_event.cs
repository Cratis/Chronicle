// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage;

public class when_reading_a_migrated_event : given.an_event_sequence_storage
{
    AppendedEvent _read;

    async Task Establish()
    {
        await Append(EventSequenceNumber.First, "source");
        var migrated = new ExpandoObject();
        ((IDictionary<string, object?>)migrated)["name"] = "upcast";
        await _storage.ReplaceGenerationContent(EventSequenceNumber.First, new Dictionary<EventTypeGeneration, ExpandoObject>
        {
            [EventTypeGeneration.First] = new ExpandoObject(),
            [(EventTypeGeneration)2] = migrated
        });
    }

    async Task Because()
    {
        using var cursor = await _storage.GetFromSequenceNumber(EventSequenceNumber.First);
        await cursor.MoveNext();
        _read = cursor.Current.Single();
    }

    [Fact] void should_report_the_appended_generation() => _read.Context.EventType.Generation.ShouldEqual(EventTypeGeneration.First);
    [Fact] void should_keep_the_upcast_content() => _read.GenerationalContent[2].ShouldContain("upcast");
}
