// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Events;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_getting_tail_sequence_number_with_cancellation;

public class without_filters_or_cancellation : given.an_asynchronous_event_sequence
{
    EventSequenceNumber _result;

    async Task Because() => _result = await _eventSequence.GetTailSequenceNumberWithCancellation(CancellationToken.None).WaitAsync(TimeSpan.FromSeconds(10));

    [Fact] void should_accept_none() => _result.ShouldEqual((EventSequenceNumber)75UL);
    [Fact] void should_acquire_with_none() => _acquisitionToken.ShouldEqual(CancellationToken.None);
    [Fact] void should_call_with_none() => _rpcToken.ShouldEqual(CancellationToken.None);
    [Fact] void should_leave_source_unspecified() => _request.EventSourceId.ShouldBeNull();
    [Fact] void should_leave_source_type_unspecified() => _request.EventSourceType.ShouldBeNull();
    [Fact] void should_leave_stream_type_unspecified() => _request.EventStreamType.ShouldBeNull();
    [Fact] void should_leave_stream_unspecified() => _request.EventStreamId.ShouldBeNull();
    [Fact] void should_leave_event_types_unspecified() => _request.EventTypeIds.ShouldBeNull();
}
