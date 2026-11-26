// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading_cursor_batches;

public class within_inclusive_bounds : given.a_recorded_multi_batch_sequence
{
    async Task Because() => await Drain(await _storage.GetRange(20, 300, _matching));

    [Fact] void should_include_both_bounds_and_nothing_outside() => _read.SequenceEqual(Expected(20, 300)).ShouldBeTrue();
    [Fact] void should_read_in_batches() => _batchSizes.SequenceEqual([100, 41]).ShouldBeTrue();
    [Fact] void should_not_use_an_offset() => RecordedEventSelects.Any(select => select.Contains("OFFSET", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
}
