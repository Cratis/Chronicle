// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Reflection;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_reading;

public class without_appended_generation : given.a_storage_for_appended_generation
{
    AppendedEvent _read;

    async Task Establish()
    {
        (await _storage.AppendMany([EventAt(0, 1)])).IsSuccess.ShouldBeTrue();

        // In-memory storage has no historic documents to load. Remove only the metadata to simulate
        // a stored event whose original generation was never recorded.
        var metadata = (IDictionary<EventSequenceNumber, uint>)typeof(EventSequenceStorage)
            .GetField("_appendedGenerations", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(_storage)!;
        metadata.Clear();
    }

    async Task Because()
    {
        await _storage.ReplaceGenerationContent(0, Content());
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_expose_unknown_appended_generation() => _read.Context.AppendedGeneration.ShouldBeNull();
    [Fact] void should_leave_the_original_generation_unknown() => _storage.GetAppendedGeneration(0).ShouldBeNull();
    [Fact] void should_still_read_the_highest_generation() => _read.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_still_read_the_highest_generations_content() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual("migrated");
}
