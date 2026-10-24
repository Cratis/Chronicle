// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Storage.MongoDB.Generations.Fencing;

/// <summary>
/// The exception that is thrown when the outcome of a fenced batch whose commit result was unknown could not be resolved in time.
/// </summary>
/// <param name="token">The <see cref="FenceToken"/> the batch was admitted with.</param>
/// <param name="batchId">The <see cref="FenceBatchId"/> of the batch.</param>
/// <param name="innerException">The last error seen while resolving.</param>
public class FencedBatchOutcomeUnknown(FenceToken token, FenceBatchId batchId, Exception? innerException)
    : Exception($"The outcome of fenced batch '{batchId}' through '{token.GateId}' could not be resolved; it may or may not have committed.", innerException)
{
    /// <summary>
    /// Gets the <see cref="FenceBatchId"/> of the batch.
    /// </summary>
    public FenceBatchId BatchId { get; } = batchId;
}
