// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.EventSequences.Mutations.for_EventSequenceMutationStateMachine;

public class when_preparing_terminal_archives : given.a_valid_mutation_state
{
    EventSequenceMutation[] _terminal;
    EventSequenceMutationArchiveResult[] _results;

    void Because()
    {
        _terminal =
        [
            Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.NotRequired, stateVersion: 4),
            Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.Accepted, stateVersion: 6),
            Mutation(EventSequenceMutationPhase.SourceCommitted, repairState: EventSequenceMutationRepairState.Unknown, stateVersion: 6)
        ];
        _results = _terminal
            .Select(mutation => EventSequenceMutationStateMachine.PrepareArchive(_scope, mutation, Token(mutation)))
            .ToArray();
    }

    [Fact] void should_prepare_every_terminal_repair_outcome() => _results.All(_ => _.Outcome == EventSequenceMutationArchiveOutcome.Prepared && _.IsSuccess).ShouldBeTrue();
    [Fact] void should_advance_the_final_version_once() => _results.Select(_ => _.History!.TerminalWitness.FinalStateVersion.Value).ShouldContainOnly([5L, 7L, 7L]);
    [Fact] void should_bind_the_definition_digest() => _results.All(_ => _.History!.TerminalWitness.DefinitionDigestV1 == _definition.DefinitionDigestV1).ShouldBeTrue();
    [Fact] void should_build_valid_terminal_receipts() => _results.All(_ => EventSequenceMutationValidator.ValidateHistory(_scope, _.History).IsValid).ShouldBeTrue();
    [Fact] void should_preserve_the_terminal_repair_states() => _results.Select(_ => _.History!.RepairState).ShouldContainOnly([EventSequenceMutationRepairState.NotRequired, EventSequenceMutationRepairState.Accepted, EventSequenceMutationRepairState.Unknown]);
    [Fact] void should_copy_every_source_identity_and_target_field() => _results.Zip(_terminal).All(_ =>
        _.First.History!.Id == _.Second.Id &&
        _.First.History.Ordinal == _.Second.Ordinal &&
        _.First.History.Origin == _.Second.Origin &&
        _.First.History.Kind == _.Second.Kind &&
        _.First.History.CommandHash == _.Second.Command.Hash &&
        _.First.History.Target == _.Second.Target).ShouldBeTrue();
    [Fact] void should_not_mutate_the_active_states() => _terminal.Select(_ => _.StateVersion.Value).ShouldContainOnly([4L, 6L, 6L]);
}
