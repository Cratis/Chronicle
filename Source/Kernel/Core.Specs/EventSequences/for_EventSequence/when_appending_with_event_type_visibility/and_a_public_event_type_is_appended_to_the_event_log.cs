// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_event_type_visibility;

public class and_a_public_event_type_is_appended_to_the_event_log : given.an_event_sequence_with_a_registered_visibility
{
    AppendResult _result;

    protected override EventSequenceId SequenceId => EventSequenceId.Log;
    protected override EventTypeVisibility Visibility => EventTypeVisibility.Public;

    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_fail() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_report_the_reason() => _result.Errors.ShouldContain(AppendError.PublicEventTypeCannotBeAppendedToEventLog);
    [Fact] void should_not_write_to_storage() => _appendedSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
}
