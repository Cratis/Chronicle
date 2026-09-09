// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_history;

public class with_an_invalid_scope : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateHistory(null, _history),
        EventSequenceMutationValidator.ValidateHistory(EventSequenceKey.NotSet, _history)
    ];

    [Fact] void should_reject_missing_and_default_scopes() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidScope).ShouldBeTrue();
}
