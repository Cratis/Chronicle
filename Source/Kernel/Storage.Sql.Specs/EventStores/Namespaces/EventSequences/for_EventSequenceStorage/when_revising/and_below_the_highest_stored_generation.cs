// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_revising;

public class and_below_the_highest_stored_generation : given.a_storage_with_two_stored_generations
{
    AppendedEvent _read;

    async Task Because()
    {
        await ReviseAt(0, 1, "revised", "revision-hash");
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_read_the_revised_content() => ValueOf(_read).ShouldEqual("revised");
    [Fact] void should_read_the_revised_generation() => _read.Context.EventType.Generation.ShouldEqual(EventTypeGeneration.First);
    [Fact] void should_read_the_revised_hash() => _read.Context.Hash.ShouldEqual(new EventHash("revision-hash"));
    [Fact] void should_report_the_revised_generation() => _read.RevisedGeneration.ShouldEqual(EventTypeGeneration.First);
    [Fact] void should_be_revised() => _read.IsRevised.ShouldBeTrue();
    [Fact] void should_keep_the_higher_generation_content() => _read.GenerationalContent.ContainsKey(2).ShouldBeTrue();
}
