// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_is_registered_after_activation;

public class and_a_closing_constraint_requires_the_deferred_snapshot : given.an_empty_sequence_with_a_failing_first_snapshot
{
    AppendResult _result;

    void Establish()
    {
        _registeredConstraints.Add(new ClosesStreamConstraintDefinition(
            "closing", [_eventType.Id], ClosedStreamDimensions.EventStreamId, []));
        _constraintValidation.IndexedDefinitions.Returns(_ => _registeredConstraints.ToArray());
    }

    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_refuse_the_append_if_initialization_cannot_be_persisted() => _result.IsSuccess.ShouldBeFalse();
    [Fact] void should_attempt_the_deferred_snapshot() => _stateWriteAttempts.ShouldEqual(1);
    [Fact] void should_not_commit_an_event() => _appendedSequenceNumber.ShouldEqual(EventSequenceNumber.Unavailable);
    [Fact] void should_not_advance_the_sequence() => _stateStorage.State.SequenceNumber.ShouldEqual(EventSequenceNumber.First);
    [Fact] void should_not_dispatch_an_event() => _appendedEventsQueues.DidNotReceive().Enqueue(Arg.Any<IEnumerable<AppendedEvent>>());
    [Fact] void should_not_update_constraint_indexes() => _constraintIndexSequenceNumbers.ShouldBeEmpty();
}
