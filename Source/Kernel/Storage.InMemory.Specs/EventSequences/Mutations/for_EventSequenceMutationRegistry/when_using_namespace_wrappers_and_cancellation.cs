// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Jobs;
using Cratis.Chronicle.Storage.EventSequences.Mutations;
using Cratis.Chronicle.Storage.Sinks;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.Mutations.for_EventSequenceMutationRegistry;

public class when_using_namespace_wrappers_and_cancellation : given.a_mutation_registry
{
    EventSequenceMutationBeginResult _shared;
    EventSequenceMutationBeginResult _isolated;
    EventSequenceMutationBeginResult _afterCanceledBegin;
    EventSequenceMutationTrackingResult _afterCanceledTracking;
    EventSequenceMutationRegistryTransitionResult _afterCanceledTransition;
    EventSequenceMutationRegistryArchiveResult _afterCanceledArchive;
    Exception _beginCancellation;
    Exception _trackingCancellation;
    Exception _transitionCancellation;
    Exception _archiveCancellation;

    async Task Because()
    {
        var storage = Storage();
        await storage.EventSequenceMutations.Begin(_request, _proposedTarget);
        _shared = await storage.EventSequenceMutations.Begin(_request, new(100UL, 101UL, 1UL));
        _isolated = await Storage().EventSequenceMutations.Begin(_request, _proposedTarget);

        var cancellationRegistry = Registry();
        using var cancellation = new CancellationTokenSource();
        await cancellation.CancelAsync();
        _beginCancellation = await Catch.Exception(async () =>
            await cancellationRegistry.Begin(_request, _proposedTarget, cancellation.Token));
        _afterCanceledBegin = await cancellationRegistry.Begin(_request, _proposedTarget);
        _trackingCancellation = await Catch.Exception(async () =>
            await cancellationRegistry.BeginTracking(_target, EventSequenceMutationCoverage.Untracked, cancellation.Token));
        _afterCanceledTracking = await cancellationRegistry.BeginTracking(_target, EventSequenceMutationCoverage.Untracked);

        _transitionCancellation = await Catch.Exception(async () =>
            await cancellationRegistry.Transition(_target, _afterCanceledBegin.Token!, EventSequenceMutationTransition.BeginApplying, cancellation.Token));
        _afterCanceledTransition = await cancellationRegistry.Transition(
            _target,
            _afterCanceledBegin.Token!,
            EventSequenceMutationTransition.BeginApplying);
        var verifying = await cancellationRegistry.Transition(
            _target,
            _afterCanceledTransition.Token!,
            EventSequenceMutationTransition.BeginVerifying);
        var committed = await cancellationRegistry.Transition(
            _target,
            verifying.Token!,
            EventSequenceMutationTransition.CommitSourceWithoutRepair);
        _archiveCancellation = await Catch.Exception(async () =>
            await cancellationRegistry.Archive(_target, committed.Token!, cancellation.Token));
        _afterCanceledArchive = await cancellationRegistry.Archive(_target, committed.Token!);
    }

    [Fact] void should_share_state_across_registry_wrappers() => _shared.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Resumed);
    [Fact] void should_start_a_new_namespace_storage_empty() => _isolated.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
    [Fact] void should_cancel_begin() => _beginCancellation.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] void should_not_write_during_canceled_begin() => _afterCanceledBegin.Outcome.ShouldEqual(EventSequenceMutationBeginOutcome.Reserved);
    [Fact] void should_cancel_begin_tracking() => _trackingCancellation.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] void should_not_write_during_canceled_tracking() => _afterCanceledTracking.Outcome.ShouldEqual(EventSequenceMutationTrackingOutcome.Began);
    [Fact] void should_cancel_transition() => _transitionCancellation.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] void should_not_write_during_canceled_transition() => _afterCanceledTransition.Outcome.ShouldEqual(EventSequenceMutationRegistryTransitionOutcome.Applied);
    [Fact] void should_cancel_archive() => _archiveCancellation.ShouldBeOfExactType<TaskCanceledException>();
    [Fact] void should_not_write_during_canceled_archive() => _afterCanceledArchive.Outcome.ShouldEqual(EventSequenceMutationRegistryArchiveOutcome.Archived);

    static EventStoreNamespaceStorage Storage() =>
        new("event-store", "namespace", Substitute.For<IJobTypes>(), Substitute.For<ISinks>());
}
