// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_compensating;

public class and_the_event_type_compensates_another_type : given.a_compensating_event_sequence
{
    AppendResult _result;

    void Establish() => SchemaCompensating(_compensatingEventType, new EventType("some-other-event", EventTypeGeneration.First));

    async Task Because() => _result = await Compensate(OriginalSequenceNumber);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_that_the_type_does_not_compensate_the_event() => _result.Errors.ShouldContain(AppendError.EventTypeDoesNotCompensateEventType);
    [Fact] void should_not_append_anything() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
