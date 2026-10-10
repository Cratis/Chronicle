// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage.when_appending_many;

public class and_appended_generations_are_mixed : given.a_storage_for_appended_generation
{
    AppendedEvent _read;

    async Task Because()
    {
        (await _storage.AppendMany([EventAt(0, 1), EventAt(1, 2)])).IsSuccess.ShouldBeTrue();
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_record_the_first_events_appended_generation() => _storage.GetAppendedGeneration(0).ShouldEqual((uint?)1);
    [Fact] void should_record_the_second_events_appended_generation() => _storage.GetAppendedGeneration(1).ShouldEqual((uint?)2);
    [Fact] void should_still_read_the_appended_generation() => _read.Context.EventType.Generation.ShouldEqual(EventTypeGeneration.First);
    [Fact] void should_still_read_the_appended_generations_content() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual("original");
}
