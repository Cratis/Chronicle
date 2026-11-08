// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_definition_inputs;

public class with_an_empty_target : given.a_mutation_validation
{
    EventSequenceMutationValidationResult _result;

    void Because() => _result = EventSequenceMutationValidator.ValidateDefinitionInputs(_scope, _request, new(10UL, 10UL, EventCount.Zero));

    [Fact] void should_be_valid() => _result.IsValid.ShouldBeTrue();
}
