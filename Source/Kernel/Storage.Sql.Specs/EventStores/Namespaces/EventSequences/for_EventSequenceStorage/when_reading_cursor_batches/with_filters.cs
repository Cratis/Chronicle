// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading_cursor_batches;

public class with_filters : given.a_sequence_with_single_criterion_distractors
{
    async Task Because() => await Drain(await _storage.GetFromSequenceNumber(0, _sourceId, _sourceType, _streamType, _streamId, [_typeA, _typeB], [_tagA, _tagB]));

    [Fact] void should_return_exactly_the_events_matching_every_criterion_in_order() => _read.SequenceEqual(Matches(0, ulong.MaxValue, 0)).ShouldBeTrue();
    [Fact] void should_span_more_than_one_batch() => _read.Count.ShouldEqual(Blocks);
}
