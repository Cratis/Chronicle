// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;
using Cratis.Chronicle.Concepts.Events.Constraints;

namespace Cratis.Chronicle.EventSequences.for_EventSequence.when_a_constraint_is_registered_after_activation;

public class and_an_unconstrained_snapshot_already_completed_initialization : given.an_empty_sequence_with_a_failing_first_snapshot
{
    AppendResult _result;

    protected override int StatePersistenceInterval => 1;

    async Task Establish()
    {
        await AppendAnEvent();
        await AppendAnEvent();
        _registeredConstraints.Add(new ClosesStreamConstraintDefinition(
            "closing", [_eventType.Id], ClosedStreamDimensions.EventStreamId, []));
        _constraintValidation.IndexedDefinitions.Returns(_ => _registeredConstraints.ToArray());
    }

    async Task Because() => _result = await AppendAnEvent();

    [Fact] void should_allow_the_constrained_append() => _result.IsSuccess.ShouldBeTrue();
    [Fact] void should_not_require_another_initialization_snapshot() => _stateWriteAttempts.ShouldEqual(3);
    [Fact] void should_snapshot_the_constrained_append() => _snapshots.Written!.ConstraintIndexWatermark.ShouldEqual(EventSequenceNumber.First + 3);
}
