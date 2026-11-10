// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Defines the silo-wide admission for observer subscription work: building and subscribing projections, and
/// recovering failed partitions.
/// </summary>
/// <remarks>
/// <para>
/// There is one per silo, shared by every event store on it, so how much of that work runs at once on a cold start
/// does not grow with the number of event stores.
/// </para>
/// <para>
/// Recovery takes only a share of it, <see cref="RecoveryLimit"/>. A recovery batch can hold its admission for minutes
/// on a cold silo - every job it starts is a grain call that may run to its timeout - and if recoveries could take every
/// slot, projection subscriptions would wait for them until their own registration deadline expired.
/// </para>
/// </remarks>
public interface ISubscriptionAdmission
{
    /// <summary>
    /// Gets the most operations admitted at once.
    /// </summary>
    int Limit { get; }

    /// <summary>
    /// Gets the most recovery batches admitted at once: half of <see cref="Limit"/>, and at least one.
    /// </summary>
    int RecoveryLimit { get; }

    /// <summary>
    /// Waits for admission within the caller's remaining budget.
    /// </summary>
    /// <param name="timeout">The remaining queue budget, or <see cref="Timeout.InfiniteTimeSpan"/> for background repair.</param>
    /// <param name="cancellationToken">The activation's cancellation token.</param>
    /// <returns>Whether admission was granted.</returns>
    /// <exception cref="OperationCanceledException">The activation cancelled its wait.</exception>
    Task<bool> WaitAsync(TimeSpan timeout, CancellationToken cancellationToken);

    /// <summary>
    /// Takes admission for a recovery batch only if it is free right now and recoveries have not used up their share,
    /// without waiting.
    /// </summary>
    /// <returns>Whether admission was granted.</returns>
    /// <remarks>
    /// For work running inside a grain turn, which must not hold the turn while queuing for the silo.
    /// </remarks>
    bool TryEnterForRecovery();

    /// <summary>
    /// Releases exactly one admission granted by <see cref="WaitAsync"/>.
    /// </summary>
    void Release();

    /// <summary>
    /// Releases exactly one admission granted by <see cref="TryEnterForRecovery"/>.
    /// </summary>
    void ReleaseRecovery();
}
