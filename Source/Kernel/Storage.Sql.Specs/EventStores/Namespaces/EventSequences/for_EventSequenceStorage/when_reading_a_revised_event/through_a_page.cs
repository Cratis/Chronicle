// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading_a_revised_event;

public class through_a_page : given.a_storage_with_two_stored_generations
{
    AppendedEvent _read;

    async Task Because()
    {
        await ReviseAt(0, 1, "revised", "revision-hash");
        using var page = await _storage.GetPage(new(), 0, 10);
        await page.MoveNext();
        _read = page.Current.Single();
    }

    [Fact] void should_read_the_revised_content() => ValueOf(_read).ShouldEqual("revised");
    [Fact] void should_read_the_revised_generation() => _read.Context.EventType.Generation.ShouldEqual(EventTypeGeneration.First);
}
