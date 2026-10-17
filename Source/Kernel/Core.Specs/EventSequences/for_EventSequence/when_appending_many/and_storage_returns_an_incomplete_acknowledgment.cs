// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_storage_returns_an_incomplete_acknowledgment : given.an_append_with_settlement
{
    void Establish() => _incomplete = true;
    async Task Because() => await AppendBatch();
    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_report_every_number() => _batchResult.SequenceNumbers.ShouldEqual(new EventSequenceNumber[] { 0UL, 1UL });
    [Fact] void should_read_the_batch_back() => _probes.ShouldEqual(1);
    [Fact] void should_index_each_event() => _constraintIndexSequenceNumbers.ShouldEqual(new EventSequenceNumber[] { 0UL, 1UL });
}
