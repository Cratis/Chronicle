// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_reusing_a_pre_resume_token_after_an_aba_cycle : given.a_valid_mutation_state
{
    EventSequenceMutation _current;
    EventSequenceMutationStateToken _preResume;
    EventSequenceMutationTransitionResult _result;

    void Establish()
    {
        _preResume = Token(Mutation(EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Applying, stateVersion: 3));
        _current = Mutation(EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Applying, stateVersion: 5);
    }

    void Because() => _result = EventSequenceMutationStateMachine.Apply(_scope, _current, EventSequenceMutationTransition.Resume, _preResume);

    [Fact] void should_report_a_conflict() => _result.Outcome.ShouldEqual(EventSequenceMutationTransitionOutcome.Conflict);
    [Fact] void should_return_the_later_unchanged_current_state() => ReferenceEquals(_result.Mutation, _current).ShouldBeTrue();
    [Fact] void should_not_return_a_successor_token() => _result.Token.ShouldBeNull();
}
