// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_querying_with_named_tags;

public class and_counting_with_multiple_values : given.a_storage_with_named_tags
{
    EventCount _count;

    async Task Because() => _count = await _storage.GetCountMatching(new()
    {
        NamedTags = [new(new TagName("account"), ["one", "three"])]
    });

    [Fact] void should_match_values_on_the_same_tag_element() => _count.Value.ShouldEqual(3UL);
}
