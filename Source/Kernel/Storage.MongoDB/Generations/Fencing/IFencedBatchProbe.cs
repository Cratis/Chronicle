// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// Defines points in a fenced batch where a fault harness can pause or fail it; production uses <see cref="NullFencedBatchProbe"/>.
/// </summary>
public interface IFencedBatchProbe
{
    /// <summary>
    /// Called in the open transaction before the gate record is written.
    /// </summary>
    /// <param name="batchId">The <see cref="FenceBatchId"/> of the batch.</param>
    /// <returns>Awaitable task.</returns>
    Task BeforeAdmission(FenceBatchId batchId);

    /// <summary>
    /// Called after the gate record write and before the batch's data writes.
    /// </summary>
    /// <param name="batchId">The <see cref="FenceBatchId"/> of the batch.</param>
    /// <returns>Awaitable task.</returns>
    Task AfterAdmission(FenceBatchId batchId);

    /// <summary>
    /// Called after the data writes and the receipt, just before commit.
    /// </summary>
    /// <param name="batchId">The <see cref="FenceBatchId"/> of the batch.</param>
    /// <returns>Awaitable task.</returns>
    Task BeforeCommit(FenceBatchId batchId);
}
