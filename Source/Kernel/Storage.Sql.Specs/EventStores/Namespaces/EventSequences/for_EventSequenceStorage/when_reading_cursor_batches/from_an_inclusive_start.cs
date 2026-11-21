// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading_cursor_batches;

public class from_an_inclusive_start : given.a_recorded_multi_batch_sequence
{
    async Task Because() => await Drain(await _storage.GetFromSequenceNumber(0, _matching));

    [Fact] void should_return_every_matching_event_in_order() => _read.SequenceEqual(Expected(0, ulong.MaxValue)).ShouldBeTrue();
    [Fact] void should_read_in_full_batches_followed_by_the_remainder() => _batchSizes.SequenceEqual([100, 100, 5]).ShouldBeTrue();
    [Fact] void should_not_use_an_offset() => RecordedEventSelects.Any(select => select.Contains("OFFSET", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
    [Fact] void should_continue_from_the_last_sequence_number_after_the_first_page() => RecordedEventSelects.Skip(1).All(select => select.Contains("\"SequenceNumber\" >", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    [Fact] void should_read_event_batches_until_an_empty_probe() => RecordedEventSelects.Count().ShouldEqual(3);
}
