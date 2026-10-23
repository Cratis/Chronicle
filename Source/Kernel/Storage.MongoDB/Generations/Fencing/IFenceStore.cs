// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Defines the storage the fenced batch executor opens transactions in and resolves unknown commits against.
/// </summary>
public interface IFenceStore
{
    /// <summary>
    /// Begins a transaction for one fenced batch attempt.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop waiting with.</param>
    /// <returns>The open <see cref="IFenceTransaction"/>.</returns>
    Task<IFenceTransaction> Begin(CancellationToken cancellationToken);

    /// <summary>
    /// Learns whether a batch whose commit result was unknown committed, closing the gate on it if it did not.
    /// </summary>
    /// <param name="token">The <see cref="FenceToken"/> the batch was admitted with.</param>
    /// <param name="batchId">The <see cref="FenceBatchId"/> of the batch.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop waiting with.</param>
    /// <returns>The <see cref="UnknownCommitResolution"/>.</returns>
    /// <exception cref="FencedBatchOutcomeUnknown">The outcome could not be learned within the retry policy.</exception>
    Task<UnknownCommitResolution> Resolve(FenceToken token, FenceBatchId batchId, CancellationToken cancellationToken);
}
