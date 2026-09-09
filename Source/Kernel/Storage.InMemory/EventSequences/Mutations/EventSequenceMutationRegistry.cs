// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;
using Cratis.Chronicle.Storage.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.InMemory.EventSequences.Mutations;

/// <summary>
/// Represents a namespace-scoped in-memory event sequence mutation registry.
/// </summary>
/// <param name="eventStore">The event store that owns the registry.</param>
/// <param name="namespace">The namespace that owns the registry.</param>
/// <param name="state">The shared namespace mutation state.</param>
sealed class EventSequenceMutationRegistry(
    EventStoreName eventStore,
    EventStoreNamespaceName @namespace,
    EventSequenceMutationRegistryState state) : IEventSequenceMutationRegistry
{
    /// <inheritdoc/>
    public Task<EventSequenceMutationBeginResult> Begin(
        EventSequenceMutationRequest request,
        EventSequenceMutationTarget proposedTarget,
        CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<EventSequenceMutationBeginResult>(cancellationToken)
            : Task.FromResult(state.Begin(eventStore, @namespace, request, proposedTarget));

    /// <inheritdoc/>
    public Task<EventSequenceMutationRegistryTransitionResult> Transition(
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        EventSequenceMutationTransition transition,
        CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<EventSequenceMutationRegistryTransitionResult>(cancellationToken)
            : Task.FromResult(state.Transition(eventStore, @namespace, target, token, transition));

    /// <inheritdoc/>
    public Task<EventSequenceMutationRegistryArchiveResult> Archive(
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<EventSequenceMutationRegistryArchiveResult>(cancellationToken)
            : Task.FromResult(state.Archive(eventStore, @namespace, target, token));

    /// <inheritdoc/>
    public Task<EventSequenceMutationTrackingResult> BeginTracking(
        EventSequenceMutationIdentity target,
        EventSequenceMutationCoverage expected,
        CancellationToken cancellationToken = default) =>
        cancellationToken.IsCancellationRequested
            ? Task.FromCanceled<EventSequenceMutationTrackingResult>(cancellationToken)
            : Task.FromResult(state.BeginTracking(eventStore, @namespace, target, expected));
}
