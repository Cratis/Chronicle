// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_compensating;

public class and_the_event_type_compensates_the_original : given.a_compensating_event_sequence
{
    AppendResult _result;

    async Task Because() => _result = await Compensate(OriginalSequenceNumber);

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_store_one_event() => _stored.Length.ShouldEqual(1);
    [Fact] void should_store_the_compensated_sequence_number() => _stored[0].CompensationFor.ShouldEqual(OriginalSequenceNumber);
    [Fact] void should_store_the_compensating_event_type() => _stored[0].EventType.Id.ShouldEqual(_compensatingEventType.Id);
    [Fact] void should_use_the_event_source_of_the_original() => _stored[0].EventSourceId.Value.ShouldEqual("account-1");
    [Fact] void should_use_the_event_source_type_of_the_original() => _stored[0].EventSourceType.Value.ShouldEqual("Account");
    [Fact] void should_use_the_event_stream_type_of_the_original() => _stored[0].EventStreamType.Value.ShouldEqual("Payments");
    [Fact] void should_use_the_event_stream_id_of_the_original() => _stored[0].EventStreamId.Value.ShouldEqual("September");
    [Fact] void should_use_the_subject_of_the_original() => _stored[0].Subject!.Value.ShouldEqual("person-1");
}
