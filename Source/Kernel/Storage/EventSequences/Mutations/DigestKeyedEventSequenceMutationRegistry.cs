// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts;
using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents an <see cref="IEventSequenceMutationRegistry"/> that puts the event store's digest key in scope around
/// every operation of a provider registry.
/// </summary>
/// <remarks>
/// The key is resolved before the provider registry reads or writes anything. When it cannot be resolved the
/// operation returns a corrupt result without reaching the provider: nothing is written, nothing is re-applied, and no
/// digest is computed with an unkeyed version instead.
/// </remarks>
/// <param name="eventStore">The event store the registry belongs to.</param>
/// <param name="keys">The source of digest keys.</param>
/// <param name="inner">The provider registry.</param>
public sealed class DigestKeyedEventSequenceMutationRegistry(
    EventStoreName eventStore,
    IEventSequenceMutationDigestKeys keys,
    IEventSequenceMutationRegistry inner) : IEventSequenceMutationRegistry
{
    /// <inheritdoc/>
    public Task<EventSequenceMutationBeginResult> Begin(
        EventSequenceMutationRequest request,
        EventSequenceMutationTarget proposedTarget,
        CancellationToken cancellationToken = default) =>
        WithKey(() => inner.Begin(request, proposedTarget, cancellationToken), EventSequenceMutationBeginResult.Corrupt);

    /// <inheritdoc/>
    public Task<EventSequenceMutationRegistryTransitionResult> Transition(
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        EventSequenceMutationTransition transition,
        CancellationToken cancellationToken = default) =>
        WithKey(() => inner.Transition(target, token, transition, cancellationToken), EventSequenceMutationRegistryTransitionResult.Corrupt);

    /// <inheritdoc/>
    public Task<EventSequenceMutationRegistryArchiveResult> Archive(
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        CancellationToken cancellationToken = default) =>
        WithKey(() => inner.Archive(target, token, cancellationToken), EventSequenceMutationRegistryArchiveResult.Corrupt);

    /// <inheritdoc/>
    public Task<EventSequenceMutationTrackingResult> BeginTracking(
        EventSequenceMutationIdentity target,
        EventSequenceMutationCoverage expected,
        CancellationToken cancellationToken = default) =>
        WithKey(() => inner.BeginTracking(target, expected, cancellationToken), EventSequenceMutationTrackingResult.Corrupt);

    async Task<TResult> WithKey<TResult>(Func<Task<TResult>> operation, Func<TResult> unavailable)
    {
        if (await keys.TryGetFor(eventStore) is not { } key)
        {
            return unavailable();
        }

        using var scope = EventSequenceMutationDigestSeam.UseKey(key);
        return await operation();
    }
}
