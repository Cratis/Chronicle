// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Defines a runner of fenced batches: data writes that commit only if their owner epoch is still current.
/// </summary>
public interface IFencedBatchExecutor
{
    /// <summary>
    /// Runs a batch of writes in one transaction with the conditional gate write and the batch receipt.
    /// </summary>
    /// <typeparam name="T">The type of result.</typeparam>
    /// <param name="token">The <see cref="FenceToken"/> to write with.</param>
    /// <param name="writes">The batch's writes; every write must use the given session. Called again from the start after a transient error.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop waiting with.</param>
    /// <returns>The result of the writes of the attempt that committed.</returns>
    /// <exception cref="FenceRevoked">The epoch has moved on; nothing of the batch committed.</exception>
    /// <exception cref="FencedBatchOutcomeUnknown">The commit result was unknown and could not be resolved.</exception>
    /// <exception cref="FencedBatchRetriesExhausted">Every attempt failed with a transient transaction error.</exception>
    Task<T> Execute<T>(FenceToken token, Func<IClientSessionHandle, Task<T>> writes, CancellationToken cancellationToken = default);
}
