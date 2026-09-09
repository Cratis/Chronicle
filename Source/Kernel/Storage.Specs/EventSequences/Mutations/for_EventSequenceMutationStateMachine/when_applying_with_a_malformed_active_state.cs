// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_applying_with_a_malformed_active_state : given.a_valid_mutation_state
{
    EventSequenceMutation _malformed;
    EventSequenceMutation _before;
    EventSequenceMutationTransitionResult _result;

    void Establish()
    {
        _malformed = _active with { BlockedFrom = EventSequenceMutationPhase.Applying };
        _before = _malformed with { };
    }

    void Because() => _result = EventSequenceMutationStateMachine.Apply(_scope, _malformed, EventSequenceMutationTransition.BeginApplying, _token);

    [Fact] void should_report_invalid() => _result.Outcome.ShouldEqual(EventSequenceMutationTransitionOutcome.Invalid);
    [Fact] void should_report_the_typed_composite_error() => _result.Validation.Error.ShouldEqual(EventSequenceMutationValidationError.InvalidComposite);
    [Fact] void should_not_return_a_mutation() => _result.Mutation.ShouldBeNull();
    [Fact] void should_not_return_a_token() => _result.Token.ShouldBeNull();
    [Fact] void should_leave_the_malformed_input_unchanged() => _malformed.ShouldEqual(_before);
}
