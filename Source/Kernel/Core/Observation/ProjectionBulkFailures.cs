// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using Cratis.Chronicle.Concepts.Observation;
using SinkFailedPartition = Cratis.Chronicle.Storage.Sinks.FailedPartition;

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Records failed writes from the final projection bulk flush just as failures during ApplyChanges are recorded.
/// </summary>
internal static class ProjectionBulkFailures
{
    /// <summary>
    /// Registers each failed partition with its observer before finalization can report a successful outcome.
    /// </summary>
    /// <param name="grainFactory">The grain factory used to find the observer.</param>
    /// <param name="observerDetails">The observer whose bulk write failed.</param>
    /// <param name="failedPartitions">The failed partitions from the sink.</param>
    /// <returns>Awaitable task.</returns>
    internal static async Task Record(IGrainFactory grainFactory, ObserverDetails observerDetails, IEnumerable<SinkFailedPartition> failedPartitions)
    {
        foreach (var failedPartition in failedPartitions)
        {
            var observer = grainFactory.GetGrain<IObserver>(observerDetails.Key);
            await observer.PartitionFailed(
                failedPartition.EventSourceId,
                failedPartition.EventSequenceNumber,
                [MessageFor(failedPartition)],
                string.Empty,
                FailureKind.Handling);
        }
    }

    /// <summary>
    /// Describes a failed partition for its failure record, including the reason the sink gave when it gave one.
    /// </summary>
    /// <param name="failedPartition">The failed partition from the sink.</param>
    /// <returns>The message to record.</returns>
    internal static string MessageFor(SinkFailedPartition failedPartition) =>
        string.IsNullOrEmpty(failedPartition.Reason)
            ? $"Bulk operation failed for partition {failedPartition.EventSourceId}"
            : $"Bulk operation failed for partition {failedPartition.EventSourceId}: {failedPartition.Reason}";
}
