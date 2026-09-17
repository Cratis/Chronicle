// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending;

public class with_receipts_turned_off : given.an_acknowledged_append_without_receipts
{
    AppendResult _result;

    async Task Because() => _result = await _eventSequence.Append(_source, "single");

    [Fact] void should_not_ask_the_kernel_for_a_receipt() => _request.IncludeReceipt.ShouldBeFalse();
    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_report_the_assigned_sequence_number() => _result.SequenceNumber.Value.ShouldEqual(42UL);
    [Fact] void should_not_expose_a_receipt() => _result.Receipt.ShouldBeNull();
    [Fact] void should_still_notify() => _notifications.Length.ShouldEqual(1);
    [Fact] void should_describe_the_request_in_the_notification() => _notifications[0].Event.Context.EventSourceId.ShouldEqual(_source);
    [Fact] void should_not_carry_a_receipt_on_the_notification_result() => _notifications[0].Result.Receipt.ShouldBeNull();
}
