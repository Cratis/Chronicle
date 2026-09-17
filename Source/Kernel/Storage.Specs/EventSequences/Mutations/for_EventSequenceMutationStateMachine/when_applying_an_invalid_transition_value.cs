// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_applying_an_invalid_transition_value : given.a_valid_mutation_state
{
    EventSequenceMutation _before;
    EventSequenceMutationTransitionResult[] _results;

    void Establish() => _before = _active with { };
    void Because() => _results =
    [
        EventSequenceMutationStateMachine.Apply(_scope, _active, EventSequenceMutationTransition.Unspecified, _token),
        EventSequenceMutationStateMachine.Apply(_scope, _active, (EventSequenceMutationTransition)int.MaxValue, _token)
    ];

    [Fact] void should_report_invalid() => _results.All(_ => _.Outcome == EventSequenceMutationTransitionOutcome.Invalid).ShouldBeTrue();
    [Fact] void should_report_the_typed_enum_error() => _results.All(_ => _.Validation.Error == EventSequenceMutationValidationError.InvalidEnum).ShouldBeTrue();
    [Fact] void should_identify_the_transition_field() => _results.All(_ => _.Validation.Field == "transition").ShouldBeTrue();
    [Fact] void should_not_return_a_mutation() => _results.All(_ => _.Mutation is null).ShouldBeTrue();
    [Fact] void should_not_return_a_token() => _results.All(_ => _.Token is null).ShouldBeTrue();
    [Fact] void should_leave_the_current_state_unchanged() => _active.ShouldEqual(_before);
}
