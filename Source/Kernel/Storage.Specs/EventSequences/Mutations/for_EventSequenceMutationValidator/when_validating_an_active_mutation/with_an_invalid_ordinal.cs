// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_an_active_mutation;

public class with_an_invalid_ordinal : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateActive(_scope, _mutation with { Ordinal = null! }),
        EventSequenceMutationValidator.ValidateActive(_scope, _mutation with { Ordinal = EventSequenceMutationOrdinal.NotSet }),
        EventSequenceMutationValidator.ValidateActive(_scope, _mutation with { Ordinal = -1L })
    ];

    [Fact] void should_require_a_positive_ordinal() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidOrdinal).ShouldBeTrue();
}
