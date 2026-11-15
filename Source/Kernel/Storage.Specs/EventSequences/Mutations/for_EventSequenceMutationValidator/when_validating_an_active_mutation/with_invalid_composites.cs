// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_an_active_mutation;

public class with_invalid_composites : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        Validate(EventSequenceMutationPhase.None, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unspecified),
        Validate(EventSequenceMutationPhase.Reserved, EventSequenceMutationPhase.Applying, EventSequenceMutationRepairState.Unspecified),
        Validate(EventSequenceMutationPhase.Applying, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Pending),
        Validate(EventSequenceMutationPhase.Verifying, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Accepted),
        Validate(EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unspecified),
        Validate(EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Reserved, EventSequenceMutationRepairState.Unspecified),
        Validate(EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Applying, EventSequenceMutationRepairState.Pending),
        Validate(EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.Applying, EventSequenceMutationRepairState.NotRequired),
        Validate(EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unspecified)
    ];

    [Fact] void should_reject_every_invalid_composite() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidComposite).ShouldBeTrue();

    EventSequenceMutationValidationResult Validate(
        EventSequenceMutationPhase phase,
        EventSequenceMutationPhase blockedFrom,
        EventSequenceMutationRepairState repairState) =>
        EventSequenceMutationValidator.ValidateActive(_scope, _mutation with { Phase = phase, BlockedFrom = blockedFrom, RepairState = repairState });
}
