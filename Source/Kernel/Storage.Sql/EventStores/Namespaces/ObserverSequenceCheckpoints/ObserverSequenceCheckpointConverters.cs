// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Storage.Observation;
using Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.Observers;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.ObserverSequenceCheckpoints;

/// <summary>
/// Converts between kernel checkpoints and SQL entities.
/// </summary>
public static class ObserverSequenceCheckpointConverters
{
    /// <summary>Converts a checkpoint to a SQL entity.</summary>
    /// <param name="checkpoint">The checkpoint.</param>
    /// <returns>The SQL entity.</returns>
    public static ObserverSequenceCheckpointEntry ToEntity(this ObserverSequenceCheckpoint checkpoint) => new()
    {
        ObserverId = checkpoint.ObserverId.Value,
        EventSequenceId = checkpoint.EventSequenceId.Value,
        NextEventSequenceNumber = checkpoint.NextEventSequenceNumber.Value,
        LastHandledEventSequenceNumber = checkpoint.LastHandledEventSequenceNumber.Value,
        TailEventSequenceNumber = checkpoint.TailEventSequenceNumber.Value,
        FailedPartitionCount = checkpoint.FailedPartitionCount.Value,
        IsReplaying = checkpoint.IsReplaying,
        PartitionClaims = checkpoint.PartitionClaims is null ? null : JsonSerializer.Serialize(checkpoint.PartitionClaims, ObserverStateJson.Options),
        ObserverWideClaim = checkpoint.ObserverWideClaim is null ? null : JsonSerializer.Serialize(checkpoint.ObserverWideClaim, ObserverStateJson.Options),
        DeliveryFences = checkpoint.DeliveryFences is null ? null : JsonSerializer.Serialize(checkpoint.DeliveryFences, ObserverStateJson.Options),
        Parked = checkpoint.Parked
    };

    /// <summary>Converts a SQL entity to a checkpoint.</summary>
    /// <param name="checkpoint">The SQL entity.</param>
    /// <returns>The checkpoint.</returns>
    public static ObserverSequenceCheckpoint ToKernel(this ObserverSequenceCheckpointEntry checkpoint) =>
        new(
            new(checkpoint.ObserverId),
            new(checkpoint.EventSequenceId),
            new(checkpoint.NextEventSequenceNumber),
            new(checkpoint.LastHandledEventSequenceNumber),
            new(checkpoint.TailEventSequenceNumber),
            new(checkpoint.FailedPartitionCount),
            checkpoint.IsReplaying,
            checkpoint.PartitionClaims is null ? null : JsonSerializer.Deserialize<PartitionClaim[]>(checkpoint.PartitionClaims, ObserverStateJson.Options),
            checkpoint.ObserverWideClaim is null ? null : JsonSerializer.Deserialize<ObserverWideClaim>(checkpoint.ObserverWideClaim, ObserverStateJson.Options),
            checkpoint.DeliveryFences is null ? null : JsonSerializer.Deserialize<DeliveryFenceState[]>(checkpoint.DeliveryFences, ObserverStateJson.Options),
            checkpoint.Parked);
}
