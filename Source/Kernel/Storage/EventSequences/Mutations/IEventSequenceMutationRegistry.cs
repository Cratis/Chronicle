// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.EventSequences.Mutations;

namespace Cratis.Chronicle.Storage.EventSequences.Mutations;

/// <summary>
/// Defines the namespace-scoped permanent registry for event sequence mutations.
/// </summary>
public interface IEventSequenceMutationRegistry
{
    /// <summary>
    /// Permanently registers a request and begins or resumes its mutation.
    /// </summary>
    /// <param name="request">The immutable mutation request.</param>
    /// <param name="proposedTarget">The target proposed only for a request that wins first registration.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The closed begin result.</returns>
    Task<EventSequenceMutationBeginResult> Begin(
        EventSequenceMutationRequest request,
        EventSequenceMutationTarget proposedTarget,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a pure state-machine transition using compare-and-swap semantics.
    /// </summary>
    /// <param name="target">The target event sequence identity.</param>
    /// <param name="token">The complete state token used as the compare-and-swap fence.</param>
    /// <param name="transition">The pure state transition to apply.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The closed transition result.</returns>
    Task<EventSequenceMutationRegistryTransitionResult> Transition(
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        EventSequenceMutationTransition transition,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Archives a terminal mutation using compare-and-swap semantics.
    /// </summary>
    /// <param name="target">The target event sequence identity.</param>
    /// <param name="token">The complete terminal state token used as the compare-and-swap fence.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The closed archive result containing the verified permanent receipt on success.</returns>
    Task<EventSequenceMutationRegistryArchiveResult> Archive(
        EventSequenceMutationIdentity target,
        EventSequenceMutationStateToken token,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Begins tracking an untracked event sequence by moving its coverage to unsealed.
    /// </summary>
    /// <param name="target">The target event sequence identity.</param>
    /// <param name="expected">The expected current coverage.</param>
    /// <param name="cancellationToken">The caller cancellation token.</param>
    /// <returns>The closed tracking result.</returns>
    Task<EventSequenceMutationTrackingResult> BeginTracking(
        EventSequenceMutationIdentity target,
        EventSequenceMutationCoverage expected,
        CancellationToken cancellationToken = default);
}
