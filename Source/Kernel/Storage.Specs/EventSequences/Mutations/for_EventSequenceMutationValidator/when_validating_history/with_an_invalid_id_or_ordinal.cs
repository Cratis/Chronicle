// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_history;

public class with_an_invalid_id_or_ordinal : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateHistory(_scope, null),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { Id = null! }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { Id = EventSequenceMutationId.NotSet }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { Ordinal = null! }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { Ordinal = EventSequenceMutationOrdinal.NotSet }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { Ordinal = -1L })
    ];

    [Fact] void should_reject_every_missing_sentinel_and_non_positive_value() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidTerminal).ShouldBeTrue();
}
