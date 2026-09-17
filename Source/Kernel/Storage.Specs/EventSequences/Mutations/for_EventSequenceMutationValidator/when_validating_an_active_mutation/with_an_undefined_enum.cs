// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator.when_validating_an_active_mutation;

public class with_an_undefined_enum : given.a_mutation_validation
{
    EventSequenceMutationValidationResult[] _results;

    void Because() => _results =
    [
        EventSequenceMutationValidator.ValidateActive(_scope, _mutation with { Phase = (EventSequenceMutationPhase)int.MaxValue }),
        EventSequenceMutationValidator.ValidateActive(_scope, _mutation with { BlockedFrom = (EventSequenceMutationPhase)int.MaxValue }),
        EventSequenceMutationValidator.ValidateActive(_scope, _mutation with { RepairState = (EventSequenceMutationRepairState)int.MaxValue })
    ];

    [Fact] void should_reject_every_undefined_enum() => _results.All(_ => _.Error == EventSequenceMutationValidationError.InvalidEnum).ShouldBeTrue();
}
