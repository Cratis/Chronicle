// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.Storage.InMemory.for_EventSequenceStorage.when_querying_with_named_tags;

public class and_paging_with_other_dimensions : given.a_storage_with_named_tags
{
    IEnumerable<EventSequenceNumber> _sequenceNumbers;

    async Task Because()
    {
        var criteria = new EventSequenceQueryCriteria(EventSourceId: _firstEventSourceId, OccurredTo: _secondDay)
        {
            NamedTags = [new(new TagName("account"))]
        };
        using var cursor = await _storage.GetPage(criteria, 1, 1);
        await cursor.MoveNext();
        _sequenceNumbers = cursor.Current.Select(_ => _.Context.SequenceNumber).ToArray();
    }

    [Fact] void should_combine_a_name_only_match_with_the_other_dimensions_before_paging() =>
        _sequenceNumbers.ShouldEqual([(EventSequenceNumber)4UL]);
}
