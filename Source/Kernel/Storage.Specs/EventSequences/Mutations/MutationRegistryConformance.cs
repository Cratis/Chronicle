// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Runs the common registration and state-transition contract against a namespace-scoped provider.
/// </summary>
public static class MutationRegistryConformance
{
    /// <summary>
    /// Verifies registration binding, exact retries, stale tokens, legal edges and rejected transitions.
    /// </summary>
    /// <param name="registry">The provider to exercise.</param>
    /// <returns>The verification task.</returns>
    public static async Task Verify(IEventSequenceMutationRegistry registry)
    {
        var target = Identity("lifecycle-target");
        var request = Request(target);
        var range = new EventSequenceMutationTarget(10UL, 13UL, 3UL);
        var begin = await registry.Begin(request, range);
        begin.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
        (await registry.Begin(request, new(0UL, 0UL, 0UL))).Active.ShouldEqual(begin.Active);
        (await registry.Begin(request with { Command = new("different", "different") }, range)).Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.DefinitionConflict);
        (await registry.Begin(request with { Command = null! }, range)).Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.DefinitionConflict);
        (await registry.Begin(request with { TargetSequence = Identity("wrong-target") }, range)).Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.DefinitionConflict);
        (await registry.Begin(Request(target, 43), range)).Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.MutationAlreadyInProgress);
        (await registry.Transition(Identity("wrong-target"), begin.Token!, EventSequenceMutationTransition.BeginApplying)).Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.StateConflict);
        var wrongBinding = EventSequenceMutationStateToken.Create(begin.Token!.Scope, begin.Active! with { Ordinal = 2L });
        (await registry.Transition(target, wrongBinding, EventSequenceMutationTransition.BeginApplying)).Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.StateConflict);
        (await registry.Transition(target, begin.Token, EventSequenceMutationTransition.AcceptRepair)).Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.StateConflict);
        (await registry.Archive(target, begin.Token)).Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.StateConflict);

        EventSequenceMutationTransition[] path =
        [
            EventSequenceMutationTransition.BeginApplying,
            EventSequenceMutationTransition.Block,
            EventSequenceMutationTransition.Resume,
            EventSequenceMutationTransition.BeginVerifying,
            EventSequenceMutationTransition.Block,
            EventSequenceMutationTransition.Resume,
            EventSequenceMutationTransition.CommitSourceWithRepair,
            EventSequenceMutationTransition.BeginRepairDispatch,
            EventSequenceMutationTransition.AcceptRepair
        ];
        var token = begin.Token;
        foreach (var transition in path)
        {
            var applied = await registry.Transition(target, token, transition);
            applied.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.Applied);
            var retry = await registry.Transition(target, token, transition);
            retry.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.AlreadyApplied);
            retry.Active.ShouldEqual(applied.Active);
            token = applied.Token!;
        }

        (await registry.Transition(target, begin.Token, EventSequenceMutationTransition.BeginApplying)).Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.StateConflict);
        (await registry.Archive(target, token)).Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.Archived);
        (await registry.Begin(request with { Command = null! }, range)).Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.DefinitionConflict);

        var concurrentTarget = Identity("concurrent-target");
        var concurrent = await Task.WhenAll(
            registry.Begin(Request(concurrentTarget), range),
            registry.Begin(Request(concurrentTarget, 43), range));
        concurrent.Count(result => result.Outcome == EventSequenceMutationBeginOutcome.Reserved).ShouldEqual(1);
        concurrent.Count(result => result.Outcome is EventSequenceMutationBeginOutcome.MutationAlreadyInProgress or EventSequenceMutationBeginOutcome.Contended).ShouldEqual(1);

        // SQL Server's default text equality must not fold case or pad away trailing spaces.
        foreach (var name in new[] { "case-target", "CASE-target", "case-target " })
        {
            var distinct = await registry.Begin(Request(Identity(name)), range);
            distinct.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
            distinct.Active!.Ordinal.Value.ShouldEqual(1L);
        }
    }

    static EventSequenceMutationIdentity Identity(string value) => EventSequenceMutationIdentity.TryCreate(value).Identity!;

    static EventSequenceMutationRequest Request(EventSequenceMutationIdentity target, ulong position = 42) => new(
        EventSequenceMutationDigestCalculator.CalculateId(target, target, position, EventSequenceMutationKind.Revision),
        target,
        new(target, position),
        EventSequenceMutationKind.Revision,
        new("command", "hash"));
}
