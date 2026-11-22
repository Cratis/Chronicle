// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading_cursor_batches;

public class with_an_unbounded_end : given.a_recorded_multi_batch_sequence
{
    async Task Because() => await Drain(await _storage.GetRange(100, EventSequenceNumber.Max, _matching));

    [Fact] void should_return_every_matching_event_from_the_start() => _read.SequenceEqual(Expected(100, ulong.MaxValue)).ShouldBeTrue();
    [Fact] void should_read_in_batches() => _batchSizes.SequenceEqual([100, 55]).ShouldBeTrue();
    [Fact] void should_not_use_an_offset() => RecordedEventSelects.Any(select => select.Contains("OFFSET", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
}
