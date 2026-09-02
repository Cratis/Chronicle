// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_applying_with_a_predecessor_more_than_one_version_stale : given.a_valid_mutation_state
{
    EventSequenceMutation _current;
    EventSequenceMutationTransitionResult _result;

    void Establish() => _current = Mutation(EventSequenceMutationPhase.Applying, stateVersion: 3);
    void Because() => _result = EventSequenceMutationStateMachine.Apply(_scope, _current, EventSequenceMutationTransition.BeginApplying, _token);

    [Fact] void should_report_a_conflict() => _result.Outcome.ShouldEqual(EventSequenceMutationTransitionOutcome.Conflict);
    [Fact] void should_return_the_unchanged_current_state() => ReferenceEquals(_result.Mutation, _current).ShouldBeTrue();
    [Fact] void should_not_return_a_successor_token() => _result.Token.ShouldBeNull();
    [Fact] void should_not_report_the_valid_inputs_as_malformed() => _result.Validation.IsValid.ShouldBeTrue();
}
