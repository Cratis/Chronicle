// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_transitioning_through_the_state_graph : given.a_mutation_registry
{
    EventSequenceMutationRegistryTransitionResult[] _applied;
    EventSequenceMutationRegistryTransitionResult _retry;
    EventSequenceMutationRegistryTransitionResult _staleConflict;

    async Task Because()
    {
        var begin = await _registry.Begin(_request, _proposedTarget);
        var applying = await Apply(_registry, begin, EventSequenceMutationTransition.BeginApplying);

        // Exact retry of an already-applied transition, fenced by the very same predecessor token.
        _retry = await _registry.Transition(_target, begin.Token!, EventSequenceMutationTransition.BeginApplying);

        // A different transition fenced by the SAME now-stale token - a token reuse after the
        // state has already advanced once. This is the ABA-fencing check the new
        // ActiveStateVersion column exists for: without it, a SQL implementation keyed only on
        // the phase composite could not tell this apart from a legitimate retry.
        _staleConflict = await _registry.Transition(_target, begin.Token!, EventSequenceMutationTransition.BeginVerifying);

        var blocked = await _registry.Transition(_target, applying.Token!, EventSequenceMutationTransition.Block);
        var resumed = await _registry.Transition(_target, blocked.Token!, EventSequenceMutationTransition.Resume);
        var verifying = await _registry.Transition(_target, resumed.Token!, EventSequenceMutationTransition.BeginVerifying);
        var committed = await _registry.Transition(_target, verifying.Token!, EventSequenceMutationTransition.CommitSourceWithRepair);
        var dispatching = await _registry.Transition(_target, committed.Token!, EventSequenceMutationTransition.BeginRepairDispatch);
        var accepted = await _registry.Transition(_target, dispatching.Token!, EventSequenceMutationTransition.AcceptRepair);
        _applied = [applying, blocked, resumed, verifying, committed, dispatching, accepted];
    }

    [Fact] void should_apply_every_legal_transition() => _applied.All(_ => _.Outcome == EventSequenceMutationRegistryTransitionOutcome.Applied).ShouldBeTrue();
    [Fact] void should_report_an_exact_retry() => _retry.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.AlreadyApplied);
    [Fact] void should_reject_a_stale_token_reused_for_another_edge() => _staleConflict.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.StateConflict);
    [Fact] void should_preserve_the_ordinal() => _applied.All(_ => _.Active!.Ordinal.Value == 1).ShouldBeTrue();
    [Fact] void should_increment_every_applied_state_version() => _applied.Select(_ => _.Active!.StateVersion.Value).ShouldContainOnly([2L, 3L, 4L, 5L, 6L, 7L, 8L]);
    [Fact] void should_finish_with_accepted_repair() => _applied[^1].Active!.RepairState.ShouldEqual(EventSequenceMutationRepairState.Accepted);
}
