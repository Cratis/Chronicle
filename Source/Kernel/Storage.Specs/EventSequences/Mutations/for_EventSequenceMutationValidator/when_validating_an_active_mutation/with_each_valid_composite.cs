// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_an_active_mutation;

public class with_each_valid_composite : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        Validate(EventSequenceMutationPhase.Reserved, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unspecified),
        Validate(EventSequenceMutationPhase.Applying, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unspecified),
        Validate(EventSequenceMutationPhase.Verifying, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unspecified),
        Validate(EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Applying, EventSequenceMutationRepairState.Unspecified),
        Validate(EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Verifying, EventSequenceMutationRepairState.Unspecified),
        Validate(EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.NotRequired),
        Validate(EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Pending),
        Validate(EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Dispatching),
        Validate(EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Accepted),
        Validate(EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unknown)
    ];

    [Fact] void should_accept_every_valid_composite() => _results.All(_ => _.IsValid).ShouldBeTrue();

    EventSequenceMutationValidationResult Validate(
        EventSequenceMutationPhase phase,
        EventSequenceMutationPhase blockedFrom,
        EventSequenceMutationRepairState repairState) =>
        EventSequenceMutationValidator.ValidateActive(_scope, _mutation with { Phase = phase, BlockedFrom = blockedFrom, RepairState = repairState });
}
