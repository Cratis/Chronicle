// Copyright (c) Cratis. All rights reserved.
// Licensed under the MIT license. See LICENSE file in the project root for full license information.

namespace Cratis.Chronicle.Observation;

/// <summary>
/// Represents an implementation of <see cref="IFailedPartitions"/>.
/// </summary>
/// <param name="eventStore"><see cref="IEventStore"/> to use for getting failed partitions.</param>
public class FailedPartitions(IEventStore eventStore) : IFailedPartitions
{
    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> GetAllFailedPartitions()
    {
        var failedPartitions = await eventStore.Connection.Services.FailedPartitions.GetFailedPartitions(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
        });

        return failedPartitions.ToClient();
    }

    /// <inheritdoc/>
    public async Task<IEnumerable<FailedPartition>> GetFailedPartitionsFor(ObserverId observerId)
    {
        var failedPartitions = await eventStore.Connection.Services.FailedPartitions.GetFailedPartitions(new()
        {
            EventStore = eventStore.Name,
            Namespace = eventStore.Namespace,
            ObserverId = observerId
        });

        return failedPartitions.ToClient();
    }
}
