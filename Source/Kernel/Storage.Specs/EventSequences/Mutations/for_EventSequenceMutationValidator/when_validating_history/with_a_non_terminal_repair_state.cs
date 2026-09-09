// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_history;

public class with_a_non_terminal_repair_state : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        Validate(EventSequenceMutationRepairState.Unspecified),
        Validate(EventSequenceMutationRepairState.Pending),
        Validate(EventSequenceMutationRepairState.Dispatching),
        Validate((EventSequenceMutationRepairState)int.MaxValue)
    ];

    [Fact] void should_reject_every_non_terminal_and_undefined_state() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidTerminal).ShouldBeTrue();

    EventSequenceMutationValidationResult Validate(EventSequenceMutationRepairState repairState) =>
        EventSequenceMutationValidator.ValidateHistory(_scope, _history with { RepairState = repairState });
}
