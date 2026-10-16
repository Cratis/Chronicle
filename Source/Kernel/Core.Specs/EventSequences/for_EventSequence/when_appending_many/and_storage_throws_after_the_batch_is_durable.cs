// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many;

public class and_storage_throws_after_the_batch_is_durable : given.an_append_with_settlement
{
    void Establish() => _throwAfterWrite = true;
    async Task Because() => await AppendBatch();
    [Fact] void should_not_throw() => _error.ShouldBeNull();
    [Fact] void should_succeed() => _batchResult.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_every_number() => _batchResult.SequenceNumbers.Count().ShouldEqual(2);
    [Fact] void should_index_every_event() => _constraintIndexSequenceNumbers.Count.ShouldEqual(2);
}
