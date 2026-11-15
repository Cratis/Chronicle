// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_history;

public class with_an_invalid_kind : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { Kind = EventSequenceMutationKind.Unknown }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { Kind = (EventSequenceMutationKind)int.MaxValue })
    ];

    [Fact] void should_reject_every_invalid_receipt_field() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidTerminal).ShouldBeTrue();
}
