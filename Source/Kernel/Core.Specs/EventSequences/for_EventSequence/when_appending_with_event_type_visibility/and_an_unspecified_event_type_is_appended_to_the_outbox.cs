// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_appending_with_event_type_visibility;

public class and_an_unspecified_event_type_is_appended_to_the_outbox : given.an_event_sequence_with_a_registered_visibility
{
    AppendResult _result;

    protected override EventSequenceId SequenceId => EventSequenceId.Outbox;
    protected override EventTypeVisibility Visibility => EventTypeVisibility.Unspecified;

    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_succeed() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_write_to_storage() => _appendedSequenceNumber.ShouldNotEqual(EventSequenceNumber.Unavailable);
}
