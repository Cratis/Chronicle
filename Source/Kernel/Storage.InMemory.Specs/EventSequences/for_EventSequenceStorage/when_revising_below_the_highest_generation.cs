// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Dynamic;
using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.for_EventSequenceStorage;

public class when_revising_below_the_highest_generation : given.a_storage_with_stored_generations
{
    AppendedEvent _read;

    async Task Because()
    {
        dynamic content = new ExpandoObject();
        content.value = "revised";
        await _storage.Revise(0, _eventType, CorrelationId.NotSet, [], [], DateTimeOffset.UtcNow, (ExpandoObject)content, "revision-hash");
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_read_the_revised_content() => ((IDictionary<string, object?>)_read.Content)["value"].ShouldEqual("revised");
    [Fact] void should_read_the_revised_generation() => _read.Context.EventType.Generation.ShouldEqual(EventTypeGeneration.First);
    [Fact] void should_read_the_revised_hash() => _read.Context.Hash.ShouldEqual(new EventHash("revision-hash"));
    [Fact] void should_be_revised() => _read.IsRevised.ShouldBeTrue();
}
