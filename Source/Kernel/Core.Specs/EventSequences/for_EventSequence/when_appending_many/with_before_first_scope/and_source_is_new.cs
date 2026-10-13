// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_many.with_before_first_scope;

public class and_source_is_new : given.a_before_first_batch
{
    async Task Because() => _result = await AppendWithScopes();

    [Fact] void should_accept_the_batch() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_append_all_three_events() => _storedBatch.Length.ShouldEqual(3);
    [Fact] void should_return_every_sequence_number() => _result.SequenceNumbers.ShouldEqual(_storedBatch.Select(@event => @event.SequenceNumber));
    [Fact] void should_check_the_source_only_once() => _checkedSources.ShouldContainOnly(_eventSourceId);
    [Fact] void should_check_before_writing_any_event() => _checksBeforeWrite.ShouldEqual(1);
    [Fact] void should_report_the_concurrency_check() => _result.ConcurrencyCheckPerformed.ShouldBeTrue();
}
