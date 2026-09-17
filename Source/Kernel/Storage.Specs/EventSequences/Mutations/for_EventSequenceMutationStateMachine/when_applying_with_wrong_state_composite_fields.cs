// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_applying_with_wrong_state_composite_fields : given.a_valid_mutation_state
{
    (EventSequenceMutation Current, EventSequenceMutationTransitionResult Result)[] _results;

    void Because()
    {
        var blockedFromApplying = Mutation(EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Applying);
        var sourceCommittedPending = Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.Pending);
        _results =
        [
            (_active, EventSequenceMutationStateMachine.Apply(_scope, _active, EventSequenceMutationTransition.BeginApplying, Token(Mutation(EventSequenceMutationPhase.Applying)))),
            (blockedFromApplying, EventSequenceMutationStateMachine.Apply(_scope, blockedFromApplying, EventSequenceMutationTransition.Resume, Token(Mutation(EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Verifying)))),
            (sourceCommittedPending, EventSequenceMutationStateMachine.Apply(_scope, sourceCommittedPending, EventSequenceMutationTransition.BeginRepairDispatch, Token(Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.Dispatching))))
        ];
    }

    [Fact] void should_cover_phase_blocked_source_and_repair_state() => _results.Length.ShouldEqual(3);
    [Fact] void should_report_every_valid_but_wrong_composite_as_a_conflict() => _results.All(_ => _.Result.Outcome == EventSequenceMutationTransitionOutcome.Conflict).ShouldBeTrue();
    [Fact] void should_return_each_unchanged_current_state() => _results.All(_ => ReferenceEquals(_.Result.Mutation, _.Current)).ShouldBeTrue();
    [Fact] void should_not_return_a_successor_token() => _results.All(_ => _.Result.Token is null).ShouldBeTrue();
    [Fact] void should_not_report_the_valid_inputs_as_malformed() => _results.All(_ => _.Result.Validation.IsValid).ShouldBeTrue();
}
