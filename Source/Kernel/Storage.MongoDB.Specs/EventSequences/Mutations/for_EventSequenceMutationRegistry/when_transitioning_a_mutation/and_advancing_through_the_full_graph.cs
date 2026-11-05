// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.MongoDB.Sinks;

namespace Cratis.Chronicle.Storage.MongoDB.EventSequences.Mutations.for_EventSequenceMutationRegistry.when_transitioning_a_mutation;

/// <summary>
/// Drives a mutation through every legal edge of the state graph via the MongoDB-backed registry, proving the
/// compare-and-swap write-back after each <see cref="EventSequenceMutationStateMachine.Apply"/> call persists the
/// successor correctly, that an exact retry is idempotent, and that a token from before a Block/Resume cycle is
/// rejected once replayed against the state the cycle left behind (the ABA case: the phase looks the same again,
/// but the state version has moved on).
/// </summary>
/// <param name="fixture">The <see cref="MongoDBFixture"/> providing the MongoDB server.</param>
[Collection(MongoDBCollection.Name)]
public class and_advancing_through_the_full_graph(MongoDBFixture fixture) : given.a_mutation_registry(fixture)
{
    EventSequenceMutationRegistryTransitionResult[] _applied = default!;
    EventSequenceMutationRegistryTransitionResult _retry = default!;
    EventSequenceMutationRegistryTransitionResult _abaConflict = default!;

    async Task Because()
    {
        var begin = await Registry.Begin(Request, ProposedTarget);
        var applying = await Apply(Registry, begin, EventSequenceMutationTransition.BeginApplying);

        // Exact retry: reapplying BeginApplying with the ORIGINAL pre-transition (Reserved) token, after the
        // transition has already landed, must be recognized as the same transition rather than rejected.
        _retry = await Registry.Transition(Target, begin.Token!, EventSequenceMutationTransition.BeginApplying);

        var blocked = await Registry.Transition(Target, applying.Token!, EventSequenceMutationTransition.Block);
        var resumed = await Registry.Transition(Target, blocked.Token!, EventSequenceMutationTransition.Resume);

        // The ABA case: 'applying.Token' was a valid Applying-phase token before the Block/Resume round-trip.
        // 'resumed' is Applying again too, but at a later state version - the stale token must not be accepted
        // as if it still fenced the current state.
        _abaConflict = await Registry.Transition(Target, applying.Token!, EventSequenceMutationTransition.BeginVerifying);

        var verifying = await Registry.Transition(Target, resumed.Token!, EventSequenceMutationTransition.BeginVerifying);
        var committed = await Registry.Transition(Target, verifying.Token!, EventSequenceMutationTransition.CommitSourceWithRepair);
        var dispatching = await Registry.Transition(Target, committed.Token!, EventSequenceMutationTransition.BeginRepairDispatch);
        var accepted = await Registry.Transition(Target, dispatching.Token!, EventSequenceMutationTransition.AcceptRepair);
        _applied = [applying, blocked, resumed, verifying, committed, dispatching, accepted];
    }

    [Fact] void should_apply_every_legal_transition() => _applied.All(_ => _.Outcome == EventSequenceMutationRegistryTransitionOutcome.Applied).ShouldBeTrue();
    [Fact] void should_report_an_exact_retry_as_already_applied() => _retry.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.AlreadyApplied);
    [Fact] void should_return_the_same_state_for_an_exact_retry() => _retry.Active!.StateVersion.ShouldEqual(_applied[0].Active!.StateVersion);
    [Fact] void should_reject_the_stale_pre_cycle_token() => _abaConflict.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.StateConflict);
    [Fact] void should_preserve_the_ordinal_throughout() => _applied.All(_ => _.Active!.Ordinal == EventSequenceMutationOrdinal.First).ShouldBeTrue();
    [Fact] void should_increment_the_state_version_on_every_applied_transition() =>
        _applied.Select(_ => _.Active!.StateVersion.Value).ShouldContainOnly([2L, 3L, 4L, 5L, 6L, 7L, 8L]);
    [Fact] void should_finish_with_accepted_repair() => _applied[^1].Active!.RepairState.ShouldEqual(EventSequenceMutationRepairState.Accepted);
}
