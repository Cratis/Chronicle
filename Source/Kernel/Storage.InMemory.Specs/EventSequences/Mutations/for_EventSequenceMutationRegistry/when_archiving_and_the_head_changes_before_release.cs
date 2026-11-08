// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.Mutations.for_EventSequenceMutationRegistry;

/// <summary>
/// The in-memory archive validates the head and writes the receipt under one lock, so a token whose head has
/// moved on can never leave a receipt behind.
/// </summary>
public class when_archiving_and_the_head_changes_before_release : given.a_mutation_registry
{
    EventSequenceMutationRegistryArchiveResult _result;
    EventSequenceMutationBeginResult _next;

    async Task Because()
    {
        var begin = await _registry.Begin(_request, _proposedTarget);
        var applying = await Apply(_registry, begin, EventSequenceMutationTransition.BeginApplying);
        var verifying = await _registry.Transition(_target, applying.Token!, EventSequenceMutationTransition.BeginVerifying);
        var committed = await _registry.Transition(_target, verifying.Token!, EventSequenceMutationTransition.CommitSourceWithoutRepair);
        var movedOn = EventSequenceMutationStateToken.Create(
            committed.Token!.Scope,
            committed.Active! with { RepairState = EventSequenceMutationRepairState.Pending });
        _result = await _registry.Archive(_target, movedOn);
        _next = await _registry.Begin(Request(_target, originSequenceNumber: 43), _proposedTarget);
    }

    [Fact] void should_report_a_state_conflict() => _result.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.StateConflict);
    [Fact] void should_not_return_a_receipt() => _result.History.ShouldBeNull();
    [Fact] void should_keep_the_head_active() => _next.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.MutationAlreadyInProgress);
}
