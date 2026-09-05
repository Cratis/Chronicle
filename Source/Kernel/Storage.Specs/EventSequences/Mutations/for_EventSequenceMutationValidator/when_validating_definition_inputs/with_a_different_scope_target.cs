// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_definition_inputs;

public class with_a_different_scope_target : given.a_mutation_validation
{
    EventSequenceMutationValidationResult _result;

    void Because() => _result = EventSequenceMutationValidator.ValidateDefinitionInputs(
        _scope,
        _request with { TargetSequence = Identity("other") },
        _target);

    [Fact] void should_reject_the_target_identity_mismatch() => _result.Error.ShouldEqual(EventSequenceMutationValidationError.InvalidScope);
}
