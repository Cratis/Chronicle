// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_revising;

public class and_at_the_highest_stored_generation : given.a_storage_with_two_stored_generations
{
    AppendedEvent _read;

    async Task Because()
    {
        await ReviseAt(0, 2, "revised", "revision-hash");
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_read_the_revised_content() => ValueOf(_read).ShouldEqual("revised");
    [Fact] void should_read_the_highest_generation() => _read.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_read_the_revised_hash() => _read.Context.Hash.ShouldEqual(new EventHash("revision-hash"));
    [Fact] void should_report_the_revised_generation() => _read.RevisedGeneration.ShouldEqual(new EventTypeGeneration(2));
}
