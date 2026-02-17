// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

using System.Text.Json;
using Cratis.Chronicle.Concepts.Keys;
using Cratis.Chronicle.Concepts.Observation;
using Cratis.Chronicle.Properties;

namespace Cratis.Chronicle.Storage.Sql.EventStores.Namespaces.FailedPartitions;

/// <summary>
/// Converters for failed partitions.
/// </summary>
public static class FailedPartitionConverters
{
    /// <summary>
    /// Convert from entity to failed partition.
    /// </summary>
    /// <param name="entity">Entity to convert from.</param>
    /// <returns>Converted failed partition.</returns>
    public static Concepts.Observation.FailedPartition ToFailedPartition(this FailedPartition entity)
    {
        if (string.IsNullOrEmpty(entity.StateJson))
        {
            return new Concepts.Observation.FailedPartition
            {
                Id = (FailedPartitionId)entity.Id,
                Partition = new Key(entity.Partition, ArrayIndexers.NoIndexers),
                ObserverId = new ObserverId(entity.ObserverId),
                IsResolved = entity.IsResolved
            };
        }

        var failedPartition = JsonSerializer.Deserialize<Concepts.Observation.FailedPartition>(entity.StateJson) ?? new Concepts.Observation.FailedPartition();

        // Ensure the key properties are consistent with entity values
        failedPartition.Id = (FailedPartitionId)entity.Id;
        failedPartition.Partition = new Key(entity.Partition, ArrayIndexers.NoIndexers);
        failedPartition.ObserverId = new ObserverId(entity.ObserverId);
        failedPartition.IsResolved = entity.IsResolved;

        return failedPartition;
    }

    /// <summary>
    /// Convert from failed partition to entity.
    /// </summary>
    /// <param name="failedPartition">Failed partition to convert from.</param>
    /// <returns>Converted entity.</returns>
    public static FailedPartition ToEntity(this Concepts.Observation.FailedPartition failedPartition) =>
        new()
        {
            Id = failedPartition.Id.Value,
            Partition = failedPartition.Partition.ToString(),
            ObserverId = failedPartition.ObserverId.Value,
            IsResolved = failedPartition.IsResolved,
            StateJson = JsonSerializer.Serialize(failedPartition)
        };
}
