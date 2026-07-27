// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Jobs;

/// <summary>
/// Tracks the progress checkpoints a job step has reported without persisting them and decides when the next one
/// has to be made durable.
/// </summary>
/// <remarks>
/// Initializes a new instance of the <see cref="JobStepCheckpointDebounce"/> class.
/// </remarks>
/// <param name="batchInterval">
/// Number of reported batches that may accumulate before the checkpoint is persisted. Values below one are
/// treated as one.
/// </param>
public class JobStepCheckpointDebounce(int batchInterval)
{
    int _reportsSinceLastWrite;

    /// <summary>
    /// Gets the number of reported batches that may accumulate before a write is forced.
    /// </summary>
    public int BatchInterval { get; } = batchInterval < 1 ? 1 : batchInterval;

    /// <summary>
    /// Gets a value indicating whether a reported checkpoint is still waiting to be persisted.
    /// </summary>
    public bool HasPendingCheckpoint => _reportsSinceLastWrite > 0;

    /// <summary>
    /// Records that a batch has been reported and tells whether the checkpoint must be persisted now.
    /// </summary>
    /// <param name="afterEveryBatch">
    /// True when the step cannot leave a batch unpersisted, which overrides <see cref="BatchInterval"/>.
    /// </param>
    /// <returns>True when the caller must persist the state.</returns>
    public bool Report(bool afterEveryBatch)
    {
        _reportsSinceLastWrite++;
        return _reportsSinceLastWrite >= (afterEveryBatch ? 1 : BatchInterval);
    }

    /// <summary>
    /// Records that the state has been persisted, clearing the pending checkpoint.
    /// </summary>
    public void Persisted() => _reportsSinceLastWrite = 0;
}
