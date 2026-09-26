// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_querying_with_named_tags;

public class and_isolating_sequences : given.a_storage_with_named_tags
{
    EventCount _count;

    async Task Establish()
    {
        await using var context = CreateContext();
        context.NamedTags.Add(NamedTagEntry.From("another-sequence", 0, 0, new(new TagName("foreign"), "one")));
        await context.SaveChangesAsync();
    }

    async Task Because() => _count = await _storage.GetCountMatching(new() { NamedTags = [new(new TagName("foreign"))] });

    [Fact] void should_not_match_a_tag_in_a_different_sequence() => _count.Value.ShouldEqual(0UL);
}
