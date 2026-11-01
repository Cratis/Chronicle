// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using KernelCheckpoint = Cratis.Chronicle.Storage.Observation.ObserverSequenceCheckpoint;

namespace Cratis.Chronicle.Storage.MongoDB.Observation;

/// <summary>
/// Converts between kernel checkpoints and MongoDB documents.
/// </summary>
public static class ObserverSequenceCheckpointConverters
{
    /// <summary>Converts a kernel checkpoint to a MongoDB document.</summary>
    /// <param name="checkpoint">The checkpoint.</param>
    /// <returns>The document.</returns>
    public static ObserverSequenceCheckpoint ToMongoDB(this KernelCheckpoint checkpoint) => new()
    {
        Id = new(checkpoint.ObserverId, checkpoint.EventSequenceId),
        NextEventSequenceNumber = checkpoint.NextEventSequenceNumber,
        LastHandledEventSequenceNumber = checkpoint.LastHandledEventSequenceNumber,
        TailEventSequenceNumber = checkpoint.TailEventSequenceNumber,
        FailedPartitionCount = checkpoint.FailedPartitionCount,
        IsReplaying = checkpoint.IsReplaying,
        PartitionClaims = checkpoint.PartitionClaims,
        ObserverWideClaim = checkpoint.ObserverWideClaim,
        DeliveryFences = checkpoint.DeliveryFences,
        Parked = checkpoint.Parked
    };

    /// <summary>Converts a MongoDB document to a kernel checkpoint.</summary>
    /// <param name="checkpoint">The document.</param>
    /// <returns>The checkpoint.</returns>
    public static KernelCheckpoint ToKernel(this ObserverSequenceCheckpoint checkpoint) =>
        new(
            checkpoint.Id.ObserverId,
            checkpoint.Id.EventSequenceId,
            checkpoint.NextEventSequenceNumber,
            checkpoint.LastHandledEventSequenceNumber,
            checkpoint.TailEventSequenceNumber,
            checkpoint.FailedPartitionCount,
            checkpoint.IsReplaying,
            checkpoint.PartitionClaims,
            checkpoint.ObserverWideClaim,
            checkpoint.DeliveryFences,
            checkpoint.Parked);
}
