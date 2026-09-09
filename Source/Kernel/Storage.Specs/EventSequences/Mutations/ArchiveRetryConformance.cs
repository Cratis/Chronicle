// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Specifies payload-free retries after registry recreation and reuse of the target head.
/// </summary>
public abstract class ArchiveRetryConformance : Specification
{
    EventSequenceMutationRegistryArchiveResult _original;
    EventSequenceMutationRegistryArchiveResult _retry;
    EventSequenceMutationRegistryArchiveResult _stale;
    EventSequenceMutationRegistryArchiveResult _wrongBinding;
    EventSequenceMutationRegistryArchiveResult _future;
    EventSequenceMutationRegistryTransitionResult _transition;
    EventSequenceMutationBeginResult _next;
    EventSequenceMutationBeginResult _resumed;
    EventSequenceMutationBeginResult _replayed;
    EventSequenceMutationBeginResult _changed;

    /// <summary>
    /// Gets a fresh namespace wrapper over the same provider storage.
    /// </summary>
    /// <returns>A new registry using the existing storage.</returns>
    protected abstract IEventSequenceMutationRegistry RecreateRegistry();

    async Task Because()
    {
        var registry = RecreateRegistry();
        await MutationRegistryConformance.Verify(registry);
        var target = EventSequenceMutationIdentity.TryCreate("retry-target").Identity!;
        var origin = new EventSequenceMutationOrigin(target, 42UL);
        var request = new EventSequenceMutationRequest(
            EventSequenceMutationDigestCalculator.CalculateId(target, target, 42UL, EventSequenceMutationKind.Revision),
            target,
            origin,
            EventSequenceMutationKind.Revision,
            new("private command", "command-hash"));
        var range = new EventSequenceMutationTarget(10UL, 13UL, 3UL);
        var begin = await registry.Begin(request, range);
        var applying = await registry.Transition(target, begin.Token!, EventSequenceMutationTransition.BeginApplying);
        var verifying = await registry.Transition(target, applying.Token!, EventSequenceMutationTransition.BeginVerifying);
        var terminal = await registry.Transition(target, verifying.Token!, EventSequenceMutationTransition.CommitSourceWithoutRepair);
        _original = await registry.Archive(target, terminal.Token!);
        var nextRequest = request with
        {
            Id = EventSequenceMutationDigestCalculator.CalculateId(target, target, 43UL, request.Kind),
            Origin = origin with { SequenceNumber = 43UL },
            Command = new("different payload", "different-hash")
        };
        _next = await registry.Begin(nextRequest, range);
        var nextApplying = await registry.Transition(target, _next.Token!, EventSequenceMutationTransition.BeginApplying);
        var nextVerifying = await registry.Transition(target, nextApplying.Token!, EventSequenceMutationTransition.BeginVerifying);
        var nextTerminal = await registry.Transition(target, nextVerifying.Token!, EventSequenceMutationTransition.CommitSourceWithoutRepair);
        _next = EventSequenceMutationBeginResult.Resumed(nextTerminal.Active!, nextTerminal.Token!);
        registry = RecreateRegistry();
        _retry = await registry.Archive(target, terminal.Token!);
        _stale = await registry.Archive(target, begin.Token!);
        _wrongBinding = await registry.Archive(target, EventSequenceMutationStateToken.Create(terminal.Token!.Scope, terminal.Active! with { Ordinal = _next.Token!.Ordinal }));
        _future = await registry.Archive(target, EventSequenceMutationStateToken.Create(terminal.Token.Scope, terminal.Active! with { StateVersion = terminal.Token.StateVersion.Next() }));
        _transition = await registry.Transition(target, begin.Token!, EventSequenceMutationTransition.BeginApplying);
        _replayed = await registry.Begin(request, new(100UL, 101UL, 1UL));
        _changed = await registry.Begin(request with { Command = new("changed", "changed") }, range);
        _resumed = await registry.Begin(nextRequest, range);
    }

    [Fact] public void should_archive_original() => _original.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.Archived);
    [Fact] public void should_retry_after_recreation_and_head_reuse() => _retry.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.AlreadyArchived);
    [Fact] public void should_return_the_exact_durable_receipt() => _retry.History.ShouldEqual(_original.History);
    [Fact] public void should_reject_a_preterminal_archive_token() => _stale.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.StateConflict);
    [Fact] public void should_reject_a_wrong_binding() => _wrongBinding.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.StateConflict);
    [Fact] public void should_reject_a_future_archive_token() => _future.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.StateConflict);
    [Fact] public void should_report_archived_lineage_for_a_transition_retry() => _transition.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.AlreadyArchived);
    [Fact] public void should_replay_the_original_begin() => _replayed.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Archived);
    [Fact] public void should_reject_a_changed_archived_request() => _changed.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.DefinitionConflict);
    [Fact] public void should_increase_the_ordinal() => _next.Active!.Ordinal.Value.ShouldEqual(2L);
    [Fact] public void should_not_clear_or_modify_the_reused_head() => _resumed.Active.ShouldEqual(_next.Active);
}
