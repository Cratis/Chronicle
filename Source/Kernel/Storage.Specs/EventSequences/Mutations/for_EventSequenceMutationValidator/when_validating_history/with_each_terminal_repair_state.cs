// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_history;

public class with_each_terminal_repair_state : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        Validate(EventSequenceMutationRepairState.NotRequired),
        Validate(EventSequenceMutationRepairState.Accepted),
        Validate(EventSequenceMutationRepairState.Unknown)
    ];

    [Fact] void should_accept_every_terminal_repair_state() => _results.All(_ => _.IsValid).ShouldBeTrue();

    EventSequenceMutationValidationResult Validate(EventSequenceMutationRepairState repairState) =>
        EventSequenceMutationValidator.ValidateHistory(
            _scope,
            WithValidReceiptDigest(_history with { RepairState = repairState }));
}
