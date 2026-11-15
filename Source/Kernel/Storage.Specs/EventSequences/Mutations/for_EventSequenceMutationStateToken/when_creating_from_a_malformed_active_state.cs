// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateToken;

public class when_creating_from_a_malformed_active_state : for_EventSequenceMutationStateMachine.given.a_valid_mutation_state
{
    Exception _error;

    void Because() => _error = Catch.Exception(() => EventSequenceMutationStateToken.Create(_scope, _active with { RepairState = EventSequenceMutationRepairState.Accepted }));

    [Fact] void should_throw_a_typed_invalid_mutation() => _error.ShouldBeOfExactType<InvalidEventSequenceMutation>();
    [Fact] void should_report_the_composite_error() => ((InvalidEventSequenceMutation)_error).Validation.Error.ShouldEqual(EventSequenceMutationValidationError.InvalidComposite);
}
