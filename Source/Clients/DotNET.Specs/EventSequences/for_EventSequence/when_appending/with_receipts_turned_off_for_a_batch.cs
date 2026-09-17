// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_receipts_turned_off_for_a_batch : given.an_acknowledged_append_without_receipts
{
    AppendManyResult _result;

    async Task Because() => _result = await _eventSequence.AppendMany([new EventForEventSourceId(_source, "first"), new EventForEventSourceId(_source, "second")]);

    [Fact] void should_not_ask_the_kernel_for_receipts() => _batchRequest.IncludeReceipts.ShouldBeFalse();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_every_assigned_sequence_number() => _result.SequenceNumbers.Select(_ => _.Value).ShouldEqual([42UL, 47UL]);
    [Fact] void should_not_expose_receipts() => _result.Receipts.ShouldBeEmpty();
    [Fact] void should_still_notify_for_every_event() => _notifications.Length.ShouldEqual(2);
}
