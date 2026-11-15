// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_a_request;

public class with_an_invalid_kind : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateRequest(_request with { Kind = EventSequenceMutationKind.Unknown }),
        EventSequenceMutationValidator.ValidateRequest(_request with { Kind = (EventSequenceMutationKind)int.MaxValue })
    ];

    [Fact] void should_reject_unknown_and_undefined_kinds() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidEnum).ShouldBeTrue();
}
