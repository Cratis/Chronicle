// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading;

public class and_the_revised_generation_was_never_recorded : given.a_storage_with_two_stored_generations
{
    AppendedEvent _read;

    async Task Because()
    {
        await ReviseAt(0, 1, "revised", "revision-hash");
        await using var context = CreateContext();
        var entry = context.Events.Single(_ => _.SequenceNumber == 0);
        entry.RevisedGeneration = null;
        await context.SaveChangesAsync();
        _read = await _storage.GetEventAt(0);
    }

    [Fact] void should_read_the_highest_generation() => _read.Context.EventType.Generation.ShouldEqual(new EventTypeGeneration(2));
    [Fact] void should_read_the_highest_generations_content() => ValueOf(_read).ShouldEqual("migrated");
    [Fact] void should_not_be_revised() => _read.IsRevised.ShouldBeFalse();
}
