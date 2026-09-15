// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_history;

public class with_a_malformed_witness : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { TerminalWitness = null! }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { TerminalWitness = _witness with { FinalStateVersion = null! } }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { TerminalWitness = _witness with { FinalStateVersion = EventSequenceMutationStateVersion.NotSet } }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { TerminalWitness = _witness with { FinalStateVersion = -1L } }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { TerminalWitness = _witness with { DefinitionDigestV1 = null! } }),
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { TerminalWitness = _witness with { ReceiptDigestV1 = null! } })
    ];

    [Fact] void should_reject_every_missing_and_non_positive_witness_field() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidTerminal).ShouldBeTrue();
}
