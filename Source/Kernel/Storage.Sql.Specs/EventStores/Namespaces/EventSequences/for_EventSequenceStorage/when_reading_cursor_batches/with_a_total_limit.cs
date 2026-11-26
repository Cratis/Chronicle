// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading_cursor_batches;

public class with_a_total_limit : given.a_recorded_multi_batch_sequence
{
    async Task Because() => await Drain(await _storage.GetEventsWithLimit(10, 150, _matching));

    [Fact] void should_return_only_the_first_matches_up_to_the_limit() => _read.SequenceEqual(Expected(10, ulong.MaxValue).Take(150)).ShouldBeTrue();
    [Fact] void should_read_in_batches_within_the_limit() => _batchSizes.SequenceEqual([100, 50]).ShouldBeTrue();
    [Fact] void should_not_use_an_offset() => RecordedEventSelects.Any(select => select.Contains("OFFSET", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
}
