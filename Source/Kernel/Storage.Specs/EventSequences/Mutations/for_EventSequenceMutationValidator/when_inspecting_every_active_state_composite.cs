// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationValidator;

public class when_inspecting_every_active_state_composite : Specification
{
    (EventSequenceMutationPhase Phase, EventSequenceMutationPhase BlockedFrom, EventSequenceMutationRepairState Repair)[] _valid;
    (EventSequenceMutationPhase Phase, EventSequenceMutationPhase BlockedFrom, EventSequenceMutationRepairState Repair)[] _invalid;

    void Because()
    {
        var all = Enum.GetValues<EventSequenceMutationPhase>()
            .SelectMany(phase => Enum.GetValues<EventSequenceMutationPhase>()
                .SelectMany(blockedFrom => Enum.GetValues<EventSequenceMutationRepairState>()
                    .Select(repair => (phase, blockedFrom, repair))))
            .ToArray();
        _valid = all.Where(_ => EventSequenceMutationValidator.IsValidComposite(_.phase, _.blockedFrom, _.repair)).ToArray();
        _invalid = all.Where(_ => !EventSequenceMutationValidator.IsValidComposite(_.phase, _.blockedFrom, _.repair)).ToArray();
    }

    [Fact] void should_admit_exactly_the_ten_closed_states() => _valid.Length.ShouldEqual(10);
    [Fact] void should_admit_only_the_explicit_closed_state_allowlist() => _valid.ShouldContainOnly(
    [
        (EventSequenceMutationPhase.Reserved, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unspecified),
        (EventSequenceMutationPhase.Applying, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unspecified),
        (EventSequenceMutationPhase.Verifying, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unspecified),
        (EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Applying, EventSequenceMutationRepairState.Unspecified),
        (EventSequenceMutationPhase.Blocked, EventSequenceMutationPhase.Verifying, EventSequenceMutationRepairState.Unspecified),
        (EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.NotRequired),
        (EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Pending),
        (EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Dispatching),
        (EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Accepted),
        (EventSequenceMutationPhase.SourceCommitted, EventSequenceMutationPhase.None, EventSequenceMutationRepairState.Unknown)
    ]);
    [Fact] void should_reject_every_other_defined_composite() => _invalid.Length.ShouldEqual(206);
}
