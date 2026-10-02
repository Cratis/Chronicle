// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Observation;

namespace Cratis.Chronicle.Reactors;

/// <summary>
/// Defines the optional capability to retry a failed partition for a reactor.
/// </summary>
public interface IReactorPartitionRecovery
{
    /// <summary>
    /// Retry a failed partition for a registered reactor.
    /// </summary>
    /// <param name="reactorType">The registered reactor type.</param>
    /// <param name="partition">The failed partition.</param>
    /// <returns>The bounded outcome of the retry request.</returns>
    Task<ReactorPartitionRetryOutcome> RetryFailedPartitionFor(Type reactorType, Partition partition);

    /// <summary>
    /// Clear the quarantine of a failed partition for a registered reactor and reset its retry budget, keeping its attempt history.
    /// </summary>
    /// <param name="reactorType">The registered reactor type.</param>
    /// <param name="partition">The quarantined partition.</param>
    /// <param name="retryImmediately">Whether to start recovering the partition right away.</param>
    /// <returns>The bounded outcome of the request.</returns>
    Task<ReactorPartitionQuarantineClearResult> ClearFailedPartitionQuarantineFor(Type reactorType, Partition partition, bool retryImmediately = true);
}
