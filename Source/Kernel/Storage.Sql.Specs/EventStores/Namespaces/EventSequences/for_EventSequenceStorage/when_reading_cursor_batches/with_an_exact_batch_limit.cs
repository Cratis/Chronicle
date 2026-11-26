// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.for_EventSequenceStorage.when_reading_cursor_batches;

public class with_an_exact_batch_limit : given.a_recorded_multi_batch_sequence
{
    async Task Because() => await Drain(await _storage.GetEventsWithLimit(0, 100, _matching));

    [Fact] void should_return_no_more_than_the_limit() => _read.SequenceEqual(Expected(0, ulong.MaxValue).Take(100)).ShouldBeTrue();
    [Fact] void should_read_a_single_full_batch() => _batchSizes.SequenceEqual([100]).ShouldBeTrue();
}
