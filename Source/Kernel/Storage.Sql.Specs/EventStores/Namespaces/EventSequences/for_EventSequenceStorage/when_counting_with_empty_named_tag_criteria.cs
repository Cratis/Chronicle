// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage;

public class when_counting_with_empty_named_tag_criteria : given.an_event_sequence_storage
{
    EventCount _count;

    async Task Establish() => await Append(EventSequenceNumber.First);

    async Task Because() => _count = await _storage.GetCountMatching(new EventSequenceQueryCriteria { NamedTags = [] });

    [Fact] void should_accept_the_query_without_narrowing() => _count.Value.ShouldEqual(1UL);
}
