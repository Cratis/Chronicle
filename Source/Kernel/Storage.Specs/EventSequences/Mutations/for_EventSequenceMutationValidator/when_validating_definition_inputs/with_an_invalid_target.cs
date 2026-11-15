// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Events;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_definition_inputs;

public class with_an_invalid_target : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope, _request, null),
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope, _request, _target with { Start = null! }),
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope, _request, _target with { EndExclusive = null! }),
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope, _request, _target with { ExpectedCount = null! }),
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope, _request, _target with { Start = EventSequenceNumber.Unavailable }),
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope, _request, _target with { EndExclusive = EventSequenceNumber.Max }),
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope, _request, _target with { ExpectedCount = EventCount.NotSet }),
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope, _request, new(13UL, 10UL, EventCount.Zero)),
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope, _request, new(10UL, 13UL, 2UL)),
        EventSequenceMutationValidator.ValidateDefinitionInputs(_scope, _request, new(10UL, 11UL, ulong.MaxValue - 1))
    ];

    [Fact] void should_reject_every_malformed_range_count_and_overflow() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidTarget).ShouldBeTrue();
}
