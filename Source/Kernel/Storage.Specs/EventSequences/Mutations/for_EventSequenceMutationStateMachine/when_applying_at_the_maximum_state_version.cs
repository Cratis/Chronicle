// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_applying_at_the_maximum_state_version : given.a_valid_mutation_state
{
    EventSequenceMutation _current;
    EventSequenceMutation _before;
    EventSequenceMutationTransitionResult _result;

    void Establish()
    {
        _current = Mutation(EventSequenceMutationPhase.Reserved, stateVersion: long.MaxValue);
        _before = _current with { };
    }

    void Because() => _result = EventSequenceMutationStateMachine.Apply(_scope, _current, EventSequenceMutationTransition.BeginApplying, Token(_current));

    [Fact] void should_report_invalid() => _result.Outcome.ShouldEqual(EventSequenceMutationTransitionOutcome.Invalid);
    [Fact] void should_report_the_typed_exhaustion_error() => _result.Validation.Error.ShouldEqual(EventSequenceMutationValidationError.StateVersionExhausted);
    [Fact] void should_not_return_a_mutation() => _result.Mutation.ShouldBeNull();
    [Fact] void should_not_return_a_token() => _result.Token.ShouldBeNull();
    [Fact] void should_leave_the_current_state_unchanged() => _current.ShouldEqual(_before);
}
