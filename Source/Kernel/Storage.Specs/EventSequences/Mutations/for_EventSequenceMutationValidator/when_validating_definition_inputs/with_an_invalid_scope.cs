// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_definition_inputs;

public class with_an_invalid_scope : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateDefinitionInputs(null, _request, _target),
        EventSequenceMutationValidator.ValidateDefinitionInputs(EventSequenceKey.NotSet, _request, _target),
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope with { EventSequenceId = null! }, _request, _target),
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope with { EventStore = null! }, _request, _target),
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope with { Namespace = null! }, _request, _target),
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope with { EventSequenceId = "contains\0nul" }, _request, _target),
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope with { EventStore = "\ud800" }, _request, _target),
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope with { Namespace = "\udc00" }, _request, _target)
    ];

    [Fact] void should_reject_every_missing_default_unsupported_and_non_strict_scope() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidScope).ShouldBeTrue();
}
