// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_preparing_an_archive_with_invalid_state : given.a_valid_mutation_state
{
    EventSequenceMutationArchiveResult[] _mismatches;
    EventSequenceMutationArchiveResult _exhausted;

    void Because()
    {
        var terminal = Mutation(
            EventSequenceMutationPhase.SourceCommitted,
            repairState: EventSequenceMutationRepairState.NotRequired,
            stateVersion: 4);
        var anotherIdentity = EventSequenceMutationIdentity.TryCreate("another").Identity!;
        _mismatches =
        [
            EventSequenceMutationStateMachine.PrepareArchive(_scope, terminal, UncheckedToken(_scope with { EventSequenceId = "another" }, terminal)),
            EventSequenceMutationStateMachine.PrepareArchive(_scope, terminal, UncheckedToken(_scope, terminal with { Definition = _definition with { Request = _request with { TargetSequence = anotherIdentity } } })),
            EventSequenceMutationStateMachine.PrepareArchive(_scope, terminal, UncheckedToken(_scope, terminal with { Definition = _definition with { Request = _request with { Id = Guid.NewGuid() } } })),
            EventSequenceMutationStateMachine.PrepareArchive(_scope, terminal, UncheckedToken(_scope, terminal with { Ordinal = terminal.Ordinal.Value + 1 })),
            EventSequenceMutationStateMachine.PrepareArchive(_scope, terminal, UncheckedToken(_scope, terminal with { Definition = _definition with { DefinitionDigestV1 = new(new byte[32]) } })),
            EventSequenceMutationStateMachine.PrepareArchive(_scope, terminal, UncheckedToken(_scope, terminal with { StateVersion = terminal.StateVersion.Value + 1 })),
            EventSequenceMutationStateMachine.PrepareArchive(_scope, terminal, UncheckedToken(_scope, terminal with { RepairState = EventSequenceMutationRepairState.Accepted }))
        ];

        var exhausted = terminal with { StateVersion = long.MaxValue };
        _exhausted = EventSequenceMutationStateMachine.PrepareArchive(_scope, exhausted, Token(exhausted));
    }

    [Fact] void should_reject_every_mismatched_token_binding() => _mismatches.All(_ => !_.IsSuccess).ShouldBeTrue();
    [Fact] void should_not_prepare_history_for_a_mismatched_token() => _mismatches.All(_ => _.History is null).ShouldBeTrue();
    [Fact] void should_report_typed_state_version_exhaustion() => (_exhausted.Outcome == EventSequenceMutationArchiveOutcome.Invalid && _exhausted.Validation.Error == EventSequenceMutationValidationError.StateVersionExhausted).ShouldBeTrue();
    [Fact] void should_not_prepare_history_after_exhaustion() => _exhausted.History.ShouldBeNull();
}
