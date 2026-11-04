// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventTypes;
using Cratis.Chronicle.Schemas;
using Cratis.Chronicle.Storage.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_compensating;

public class and_the_event_type_is_not_a_compensation : given.a_compensating_event_sequence
{
    AppendResult _result;

    void Establish() => _eventTypesStorage.GetFor(_compensatingEventType.Id, Arg.Any<Concepts.Events.EventTypeGeneration?>())
        .Returns(new EventTypeSchema(_compensatingEventType, EventTypeOwner.Client, EventTypeSource.Code, new JsonSchema()));

    async Task Because() => _result = await Compensate(OriginalSequenceNumber);

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_that_the_type_does_not_compensate_the_event() => _result.Errors.ShouldContain(AppendError.EventTypeDoesNotCompensateEventType);
    [Fact] void should_not_append_anything() => _eventSequenceStorage.DidNotReceive().AppendMany(Arg.Any<IEnumerable<EventToAppendToStorage>>());
}
