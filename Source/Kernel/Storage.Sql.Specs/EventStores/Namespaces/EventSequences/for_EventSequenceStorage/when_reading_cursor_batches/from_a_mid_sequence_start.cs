// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading_cursor_batches;

public class from_a_mid_sequence_start : given.a_recorded_multi_batch_sequence
{
    async Task Because() => await Drain(await _storage.GetFromSequenceNumber(4, _matching));

    [Fact] void should_return_every_remaining_matching_event_in_order() => _read.SequenceEqual(Expected(4, ulong.MaxValue)).ShouldBeTrue();
    [Fact] void should_read_in_full_batches_followed_by_the_remainder() => _batchSizes.SequenceEqual([100, 100, 3]).ShouldBeTrue();
    [Fact] void should_read_three_pages() => RecordedEventSelects.Count().ShouldEqual(3);
    [Fact] void should_include_the_start_on_the_first_page() => RecordedEventSelects.First().Contains("\"SequenceNumber\" >=", StringComparison.OrdinalIgnoreCase).ShouldBeTrue();
    [Fact] void should_continue_after_the_last_event() => RecordedEventSelects.Skip(1).All(select => select.Contains("\"SequenceNumber\" > ", StringComparison.OrdinalIgnoreCase)).ShouldBeTrue();
    [Fact] void should_not_reapply_the_initial_lower_bound() => RecordedEventSelects.Skip(1).Any(select => select.Contains("\"SequenceNumber\" >=", StringComparison.OrdinalIgnoreCase)).ShouldBeFalse();
}
