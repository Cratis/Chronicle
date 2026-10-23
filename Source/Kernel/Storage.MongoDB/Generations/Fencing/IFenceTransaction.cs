// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using MongoDB.Driver;

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Defines one open transaction of a fenced batch.
/// </summary>
public interface IFenceTransaction : IDisposable
{
    /// <summary>
    /// Gets the <see cref="IClientSessionHandle"/> every write of the batch must go through.
    /// </summary>
    IClientSessionHandle Session { get; }

    /// <summary>
    /// Admits the batch by writing the gate record on the condition that it still holds the token's epoch.
    /// </summary>
    /// <param name="token">The <see cref="FenceToken"/> the batch writes with.</param>
    /// <param name="batchId">The <see cref="FenceBatchId"/> of the batch.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop waiting with.</param>
    /// <returns>True if admitted, false if the epoch has moved on.</returns>
    /// <remarks>
    /// This is a write, not a read: it makes the batch conflict with any revocation, so the two serialize on the gate record.
    /// </remarks>
    Task<bool> Admit(FenceToken token, FenceBatchId batchId, CancellationToken cancellationToken);

    /// <summary>
    /// Writes the batch's receipt, which exists after commit exactly when the batch committed.
    /// </summary>
    /// <param name="token">The <see cref="FenceToken"/> the batch writes with.</param>
    /// <param name="batchId">The <see cref="FenceBatchId"/> of the batch.</param>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop waiting with.</param>
    /// <returns>Awaitable task.</returns>
    Task WriteReceipt(FenceToken token, FenceBatchId batchId, CancellationToken cancellationToken);

    /// <summary>
    /// Commits the transaction.
    /// </summary>
    /// <param name="cancellationToken">The <see cref="CancellationToken"/> to stop waiting with.</param>
    /// <returns>Awaitable task.</returns>
    Task Commit(CancellationToken cancellationToken);

    /// <summary>
    /// Aborts the transaction if it is still open; never throws.
    /// </summary>
    /// <returns>Awaitable task.</returns>
    Task Abort();
}
