// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_archiving_a_terminal_mutation : given.a_mutation_registry
{
    EventSequenceMutationRegistryArchiveResult _ineligible;
    EventSequenceMutationRegistryArchiveResult _archived;
    EventSequenceMutationRegistryArchiveResult _retry;
    EventSequenceMutationBeginResult _permanentReplay;
    EventSequenceMutationBeginResult _permanentConflict;
    EventSequenceMutationRegistryArchiveResult _staleArchive;
    EventSequenceMutationRegistryArchiveResult _wrongVersionArchive;
    EventSequenceMutationRegistryTransitionResult _transitionReplay;
    EventSequenceMutationBeginResult _next;

    async Task Because()
    {
        var begin = await _registry.Begin(_request, _proposedTarget);
        _ineligible = await _registry.Archive(_target, begin.Token!);

        var applying = await Apply(_registry, begin, EventSequenceMutationTransition.BeginApplying);
        var verifying = await _registry.Transition(_target, applying.Token!, EventSequenceMutationTransition.BeginVerifying);
        var committed = await _registry.Transition(_target, verifying.Token!, EventSequenceMutationTransition.CommitSourceWithoutRepair);

        _archived = await _registry.Archive(_target, committed.Token!);
        _retry = await _registry.Archive(_target, committed.Token!);
        _staleArchive = await _registry.Archive(_target, begin.Token!);

        var wrongVersionToken = EventSequenceMutationStateToken.Create(
            committed.Token!.Scope,
            committed.Active! with { StateVersion = committed.Active.StateVersion.Next() });
        _wrongVersionArchive = await _registry.Archive(_target, wrongVersionToken);

        _permanentReplay = await _registry.Begin(_request, new(100UL, 101UL, 1UL));
        _permanentConflict = await _registry.Begin(_request with { Command = new("changed", "changed-hash") }, _proposedTarget);
        _transitionReplay = await _registry.Transition(_target, committed.Token!, EventSequenceMutationTransition.Block);
        _next = await _registry.Begin(Request(_target, originSequenceNumber: 43), _proposedTarget);
    }

    [Fact] void should_reject_a_nonterminal_archive() => _ineligible.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.StateConflict);
    [Fact] void should_archive_the_terminal_mutation() => _archived.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.Archived);
    [Fact] void should_return_a_payload_free_receipt() => _archived.History!.CommandHash.ShouldEqual(_request.Command.Hash);
    [Fact] void should_make_an_exact_archive_idempotent() => _retry.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.AlreadyArchived);
    [Fact] void should_return_the_exact_original_receipt_on_retry() => _retry.History.ShouldEqual(_archived.History);
    [Fact] void should_reject_a_stale_preterminal_archive_token() => _staleArchive.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.StateConflict);
    [Fact] void should_reject_a_wrong_version_archive_token() => _wrongVersionArchive.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.StateConflict);
    [Fact] void should_replay_the_permanent_archived_registration() => _permanentReplay.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Archived);
    [Fact] void should_permanently_reject_a_changed_request_after_archive() => _permanentConflict.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.DefinitionConflict);
    [Fact] void should_return_history_to_a_transition_after_archive() => _transitionReplay.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.AlreadyArchived);
    [Fact] void should_allocate_the_next_ordinal() => _next.Active!.Ordinal.Value.ShouldEqual(2L);
}
