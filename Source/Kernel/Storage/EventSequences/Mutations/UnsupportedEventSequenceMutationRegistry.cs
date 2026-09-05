// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Represents a fail-loud event sequence mutation registry for providers without registry support.
/// </summary>
public sealed class UnsupportedEventSequenceMutationRegistry : IEventSequenceMutationRegistry
{
    /// <summary>
    /// Gets the shared unsupported registry instance.
    /// </summary>
    public static readonly UnsupportedEventSequenceMutationRegistry Instance = new();

    /// <inheritdoc />
    public Task<EventSequenceMutationBeginResult> Begin(
        EventSequenceMutationRequest request,
        EventSequenceMutationTarget proposedTarget,
        CancellationToken cancellationToken = default) =>
        throw new EventSequenceMutationRegistryNotSupported(nameof(Begin));

    /// <inheritdoc />
    public Task<EventSequenceMutationRegistryTransitionResult> Transition(
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        EventSequenceMutationTransition transition,
        CancellationToken cancellationToken = default) =>
        throw new EventSequenceMutationRegistryNotSupported(nameof(Transition));

    /// <inheritdoc />
    public Task<EventSequenceMutationRegistryArchiveResult> Archive(
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        CancellationToken cancellationToken = default) =>
        throw new EventSequenceMutationRegistryNotSupported(nameof(Archive));

    /// <inheritdoc />
    public Task<EventSequenceMutationTrackingResult> BeginTracking(
        EventSequenceMutationIdentity target,
        EventSequenceMutationCoverage expected,
        CancellationToken cancellationToken = default) =>
        throw new EventSequenceMutationRegistryNotSupported(nameof(BeginTracking));
}
