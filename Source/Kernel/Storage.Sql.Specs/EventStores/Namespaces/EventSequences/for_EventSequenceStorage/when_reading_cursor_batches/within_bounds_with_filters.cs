// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading_cursor_batches;

public class within_bounds_with_filters : given.a_sequence_with_single_criterion_distractors
{
    async Task Because() => await Drain(await _storage.GetRange(70, 840, _sourceId, [_typeA, _typeB], [_tagA, _tagB], _sourceType, _streamType, CancellationToken.None));

    [Fact] void should_return_the_matches_and_stream_id_distractors_between_the_inclusive_bounds_in_order() => _read.SequenceEqual(Matches(70, 840, 0, StreamIdDistractorSlot)).ShouldBeTrue();
    [Fact] void should_include_both_bounds() => (_read[0] == 70 && _read[^1] == 840).ShouldBeTrue();
    [Fact] void should_span_more_than_one_batch() => (_read.Count > 200).ShouldBeTrue();
}
