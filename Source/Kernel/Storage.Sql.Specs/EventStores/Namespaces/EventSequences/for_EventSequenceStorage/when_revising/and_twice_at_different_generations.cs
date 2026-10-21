// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_revising;

public class and_twice_at_different_generations : given.a_storage_with_two_stored_generations
{
    AppendedEvent _read;

    async Task Because()
    {
        await ReviseAt(0, 1, "first-revision", "first-hash");
        await ReviseAt(0, 2, "second-revision", "second-hash");
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_read_the_last_revision() => ValueOf(_read).ShouldEqual("second-revision");
    [Fact] void should_read_the_last_revisions_generation() => _read.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_read_the_last_revisions_hash() => _read.Context.Hash.ShouldEqual(new EventHash("second-hash"));
}
